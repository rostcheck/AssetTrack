namespace AssetAccounting
{
	public class AssetStorageService
	{
		private List<Lot> lots;
		private List<TaxableSale> sales;
		private ILogWriter logWriter;

		public List<Lot> Lots
		{
			get
			{
				return new List<Lot>(lots);
			}
		}

		public List<TaxableSale> Sales
		{
			get
			{
				return new List<TaxableSale>(sales);
			}
		}

		public AssetStorageService(ILogWriter writer)
		{
			lots = new List<Lot>();
			sales = new List<TaxableSale>();
			logWriter = writer;
		}

		public void ApplyTransactions(List<Transaction> transactionList)
		{
			transactionList = ScrubDuplicateTransactions(transactionList);
			(transactionList, var problemTransactions) = FormTransfers(transactionList);
			DumpTransactions("tm-no-match-transfers.txt", problemTransactions);
            //transactionList = MatchAlgorithmFactory.Create(MatchAlgorithmEnum.MatchAcrossTransactions)
            //	.FormLikeKindExchanges(transactionList, logWriter);
            foreach (Transaction transaction in transactionList.OrderBy(s => s.DateAndTime))
			{
				switch (transaction.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
						logWriter.WriteEntry(string.Format("{0} {1} purchased {2:0.000000} {3}s {4} ({5}) to account {6} vault {7}", 
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountReceived, 
							transaction.MeasurementUnit.ToString().ToLower(), transaction.AssetType.ToString().ToLower(),
							transaction.ItemType, transaction.Account, transaction.Vault));
						PurchaseNewLot(transaction);
						break;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
						logWriter.WriteEntry(string.Format("{0} {1} sold {2:0.000000} {3}s {4} ({5}) from account {6} vault {7}", 
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountPaid, 
							transaction.MeasurementUnit.ToString().ToLower(), transaction.AssetType.ToString().ToLower(), 
							transaction.ItemType, transaction.Account, transaction.Vault));
						ProcessSale(transaction);
						break;
					case TransactionTypeEnum.TransferIn:
						logWriter.WriteEntry(string.Format("{0} {1} transferred {2:0.000000} {3}s {4} ({5}) from account {6}, vault {7} to account {8}, vault {9}",
							transaction.DateAndTime.ToShortDateString(), transaction.Service, transaction.AmountReceived, 
							transaction.MeasurementUnit.ToString().ToLower(), transaction.AssetType.ToString().ToLower(), 
							transaction.ItemType, 
							transaction.TransferFromAccount, transaction.TransferFromVault, transaction.Account, transaction.Vault));
						ProcessTransfer(transaction);
							break;
					case TransactionTypeEnum.FeeInCurrency:
						ApplyStorageFeeInCurrency(transaction);
						break;
					case TransactionTypeEnum.FeeInAsset:
						ApplyFeeInAsset(transaction);
						break;
				}
			}
		}

		public void DumpTransactions(string filename, List<Transaction> transactions)
		{
			StreamWriter sw = new StreamWriter(filename);
			sw.WriteLine("Date\tService\tType\tAsset\tMeasure\tUnit\t\tItemTypeAccount\tAmountPaid\tAmountReceived\tCurrency\tVault\tTransactionId\tMemo");
			string formatString = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}";

			foreach (var transaction in transactions.OrderBy(s => s.DateAndTime).ToList())
			{
				
				string formatted = string.Format(formatString, transaction.DateAndTime, transaction.Service, 
					transaction.TransactionType.ToString(), 
					transaction.AssetType.ToString().ToLower(),
					transaction.Measure, transaction.MeasurementUnit, transaction.ItemType,
					transaction.Account, transaction.AmountPaid, transaction.AmountReceived, 
					transaction.CurrencyUnit, transaction.Vault, transaction.TransactionID, transaction.Memo);
				sw.WriteLine(formatted);
			}
			sw.Close();
		}

        // For transfers, combine both sides into one transaction
        private (List<Transaction> processedTransactions, List<Transaction> problemTransactions) FormTransfers(List<Transaction> transactionList)
        {
            List<Transaction> sourceTransactions = new List<Transaction>();
			List<Transaction> unmatchedTransfers = new List<Transaction>();
            var transferTransactionList = transactionList.Where(
                s => s.TransactionType == TransactionTypeEnum.TransferIn);
            foreach (Transaction transaction in transferTransactionList.OrderBy(s => s.DateAndTime))
            {
                if (sourceTransactions.FirstOrDefault(s => s.TransactionID == transaction.TransactionID) != null)
                    continue; // Already processed it

                // Find the source and receipt sides
                Transaction? sourceTransaction = GetSourceTransactionCrypto(transaction, transactionList);
                if (sourceTransaction is null)
				{
					unmatchedTransfers.Add(transaction);
					transactionList.Remove(transaction);
					continue;
				}
                    //throw new Exception("Could not match source transaction for transfer " + transaction.TransactionID);
                if (sourceTransaction.AmountPaid == 0.0m && sourceTransaction.TransactionType != TransactionTypeEnum.TransferOut)
                    throw new Exception("Found incorrect source transaction for transfer " + transaction.TransactionID);
                Transaction? receiveTransaction = GetReceiveTransaction(transaction, transactionList);
                if (receiveTransaction is null)
                    throw new Exception("Could not identify receive transaction for transfer " + transaction.TransactionID);
                if (receiveTransaction.AmountReceived == 0.0m && sourceTransaction.TransactionType != TransactionTypeEnum.TransferIn)
                    throw new Exception("Found incorrect receive transaction for transfer " + transaction.TransactionID);

                if (receiveTransaction.AmountReceived != sourceTransaction.AmountPaid)
                {
                    decimal amountDifference = sourceTransaction.AmountPaid - Utils.ConvertMeasurementUnit(receiveTransaction.AmountReceived,
                                                   receiveTransaction.MeasurementUnit, sourceTransaction.MeasurementUnit);
					// Create a storage fee to account for the difference
					Transaction storageFee = new Transaction(sourceTransaction.Service, sourceTransaction.Account,
						sourceTransaction.DateAndTime, NewTransactionId(sourceTransaction.TransactionID, transactionList),
						TransactionTypeEnum.FeeInAsset,sourceTransaction.Vault, amountDifference,
						sourceTransaction.CurrencyUnit, 0.0m, 
						sourceTransaction.MeasurementUnit, sourceTransaction.AssetType, 
						"Transfer fee (in asset) from " + sourceTransaction.Memo, transaction.ItemType, sourceTransaction.SpotPrice);
					transactionList.Add(storageFee);
				}

				// Set the source vault property in the receipt side
				receiveTransaction.MakeTransfer(sourceTransaction.Service, sourceTransaction.Account, sourceTransaction.Vault);
				sourceTransactions.Add(sourceTransaction);
			}

			foreach (Transaction sourceTransaction in sourceTransactions)
			{
				// Throw away the source side
				transactionList.Remove(sourceTransaction);
			}
			return (transactionList, unmatchedTransfers);

        }

		// Generate a new transaction id for a transaction synthesized out of another transaction) and insure it
		// is unique
		private string NewTransactionId(string oldTransactionId, IList<Transaction> transactions)
		{
			bool unique = false;
			int counter = 1;
			string newTransactionId = "";
			do
			{
				newTransactionId = string.Format("{0}-{1}", oldTransactionId, counter++);
				unique = (transactions.Where(s => s.TransactionID == newTransactionId).FirstOrDefault() == null);
			}
			while (!unique);
			return newTransactionId;
        }

		// Coinbase Pro files include transfer transactions that also appear on Coinbase as outbound transactions
		private List<Transaction> ScrubDuplicateTransactions(IList<Transaction> transactionList)
		{
            List<Transaction> outputTransactions = new List<Transaction>();
			foreach (Transaction sourceTransaction in transactionList)
			{
				if (sourceTransaction.TransactionType == TransactionTypeEnum.TransferOut &&
					sourceTransaction.Service == "CoinbasePro")
				{
					// If we find another transfer in Coinbase with the exact same amount on the same day, filter the
					// one from Coinbase Pro
					var duplicate = transactionList.Where(s => s.TransactionType == TransactionTypeEnum.TransferOut
						&& s.DateAndTime.Date == sourceTransaction.DateAndTime.Date
						&& s.AmountPaid == sourceTransaction.AmountPaid
						&& s.Service == "Coinbase"
						).FirstOrDefault();
					if (duplicate != null)
						continue;

				}
				outputTransactions.Add(sourceTransaction);
			}
			return outputTransactions;
        }

		// Modified crypto rules for finding transaction pairs
        private Transaction? GetSourceTransactionCrypto(Transaction transaction, List<Transaction> transactionList)
        {
            Transaction? sourceTransaction = null;
            if (transaction.TransactionType == TransactionTypeEnum.TransferOut)
                return transaction; // This is the source
            else
            {
				DateTime startTime = transaction.DateAndTime.AddHours(-5.0);
                DateTime endTime = transaction.DateAndTime.AddHours(5.0);
				decimal amountMin = transaction.Measure * 0.9m;
                decimal amountMax = transaction.Measure * 1.1m;
				// Inferred source (transaction ID not available/trustworthy)
				var possibles = transactionList.OrderBy(s => s.DateAndTime).Where(
					s =>
					s.DateAndTime >= startTime &&
					s.DateAndTime <= endTime &&
					s.ItemType == transaction.ItemType &&
					s.TransactionType == TransactionTypeEnum.TransferOut //&&
																		 //s.Measure >= amountMin &&
																		 //s.Measure <= amountMax
																		 //s.AmountPaid == transaction.AmountReceived
																		 //s.AmountPaid > 0.0m
																		 //&& !s.Memo.Contains("Fee")
					);
                sourceTransaction = possibles.FirstOrDefault();
                // Fix generic "Transfer" to indicate direction
                if (sourceTransaction is not null)
                {
                    if (sourceTransaction.TransactionType != TransactionTypeEnum.TransferOut)
                        sourceTransaction.TransactionType = TransactionTypeEnum.TransferOut;
                }
                

            }
            return sourceTransaction;
        }

        private Transaction? GetSourceTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			Transaction? sourceTransaction = null;
			if (transaction.TransactionType == TransactionTypeEnum.TransferOut)
				return transaction; // This is the source
			else
            {
				// Explicit source
				sourceTransaction = transactionList.OrderBy(s => s.DateAndTime).Where(
					s => s.TransactionType == TransactionTypeEnum.TransferOut
                    //&& s.TransactionID == transaction.TransactionID // temporarily disable
                    //&& s.Service == transaction.Service // temporarily disable
                    && s.ItemType == transaction.ItemType).FirstOrDefault();
				if (sourceTransaction is not null)
                {
					// Inferred source
					sourceTransaction = transactionList.OrderBy(s => s.DateAndTime).Where(
						s => s.TransactionID == transaction.TransactionID
						&& s.Service == transaction.Service
						&& s.ItemType == transaction.ItemType
						&& s.AmountPaid > 0.0m
						&& !s.Memo.Contains("Fee")).FirstOrDefault();
					// Fix generic "Transfer" to indicate direction
					if (sourceTransaction is not null)
					{
						if (sourceTransaction.TransactionType != TransactionTypeEnum.TransferOut)
							sourceTransaction.TransactionType = TransactionTypeEnum.TransferOut;
					}
				}

			}
			return sourceTransaction;
		}

		private Transaction? GetReceiveTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			Transaction? receiveTransaction = null;
			if (transaction.TransactionType == TransactionTypeEnum.TransferIn)
				return transaction; // This is the source
			else
			{
				// Explicit destination
				receiveTransaction = transactionList.Where(
					s => s.TransactionType == TransactionTypeEnum.TransferIn
					&& s.TransactionID == transaction.TransactionID
					&& s.Service == transaction.Service
					&& s.ItemType == transaction.ItemType).FirstOrDefault();
				if (receiveTransaction is null)
				{
					// Inferred destination
					receiveTransaction = transactionList.Where(
						s => s.TransactionID == transaction.TransactionID
						&& s.Service == transaction.Service
						&& s.ItemType == transaction.ItemType
						&& s.AmountPaid > 0.0m
						&& !s.Memo.Contains("Fee")).FirstOrDefault();
					// Fix generic "Transfer" to indicate direction
					if (receiveTransaction is not null)
					{
						if (receiveTransaction.TransactionType != TransactionTypeEnum.TransferIn)
							receiveTransaction.TransactionType = TransactionTypeEnum.TransferIn;
					}
				}
			}
			return receiveTransaction;
		}
						
		// All that happens in a transfer is the vault changes
		private void ProcessTransfer(Transaction transaction)
		{
			// Find the open lots in the current vault with the correct asset type
			List<Lot> availableLots = lots.Where(
				s =>
				// s.Service == transaction.Service &&  // temporarily disable service matching
				s.AssetType == transaction.AssetType &&
				s.ItemType == transaction.ItemType &&
				s.Vault == transaction.TransferFromVault &&
				s.Account == transaction.TransferFromAccount &&
				s.IsDepleted() == false)
				.OrderBy(s => s.PurchaseDate).ToList();
			AmountInAsset amount = new AmountInAsset(transaction.DateAndTime, transaction.TransactionID, 
				                       transaction.Vault, transaction.AmountReceived, transaction.MeasurementUnit, 
				                       transaction.AssetType);
			foreach(Lot lot in availableLots)
			{
				if (lot.CurrentAmount(amount.MeasurementUnit) >= amount.Amount)
				{
					if (transaction.TransferFromAccount is null)
						throw new Exception("Poorly formatted transaction in ProcessTransfer - missing TransferFromAccount");
                    if (transaction.TransferFromVault is null)
                        throw new Exception("Poorly formatted transaction in ProcessTransfer - missing TransferFromVault");

                    // Split lot and transfer part of it
                    Lot newLot = new Lot(transaction.Service, lot.LotID + "-split", lot.PurchaseDate, amount.Amount, amount.MeasurementUnit, 
						lot.OriginalPrice, lot.AssetType, transaction.Vault, transaction.Account, transaction.ItemType);
					lots.Add(newLot);
					// Remove from original lot
					lot.DecreaseMeasureViaTransfer(transaction.DateAndTime, amount.Amount, amount.MeasurementUnit,
						transaction.TransferFromAccount, transaction.TransferFromVault);
					amount.Decrease(amount.Amount, amount.MeasurementUnit);
					break;
				}
				else
				{
					// Just reassign entire lot to other vault
					lot.Vault = transaction.Vault;
					lot.Service = transaction.Service;
					amount.Decrease(lot.CurrentAmount(lot.measurementUnit), lot.measurementUnit); // some transfer remaining
				}
			}
			if (amount.Amount > 0.0m)
				throw new Exception("Requested transfer exceeds available lots");
		}

		private void ProcessSale(Transaction transaction)
		{
			var targetMeasurementUnit = transaction.MeasurementUnit;
			if (targetMeasurementUnit != AssetMeasurementUnitEnum.CryptoCoin)
				targetMeasurementUnit = AssetMeasurementUnitEnum.Gram; // Convert metals to grams
			decimal originalMeasureToSell = Utils.ConvertMeasurementUnit(transaction.AmountPaid, transaction.MeasurementUnit, targetMeasurementUnit);
			AssetAmount remainingAmountToSell = new AssetAmount(transaction.AmountPaid, transaction.AssetType, transaction.MeasurementUnit, transaction.ItemType);
			List<Lot> availableLots = lots.Where(
				s =>
				s.Service == transaction.Service &&
				s.AssetType == transaction.AssetType &&
				s.ItemType == transaction.ItemType &&
				// s.Account == transaction.Account &&
				//s.Vault == transaction.Vault  && // where it is doesn't affect lot accounting
				s.IsDepleted() == false)
				.OrderBy(s => s.PurchaseDate).ToList();
			
			foreach(Lot lot in availableLots)
			{
				if (remainingAmountToSell.Measure == 0.0m)
				{
					break;
				}
				AssetAmount amountToSell = lot.GetAmountToSell(remainingAmountToSell);
				decimal percentageOfSale = Utils.ConvertMeasurementUnit(amountToSell.Measure, amountToSell.MeasurementUnit, targetMeasurementUnit) /
					originalMeasureToSell;
				ValueInCurrency valuePaidForThisAmount = new ValueInCurrency(percentageOfSale * transaction.AmountReceived,
					                                         transaction.CurrencyUnit, transaction.DateAndTime);
				remainingAmountToSell = remainingAmountToSell - amountToSell;
				sales.Add(lot.Sell(amountToSell, valuePaidForThisAmount));
			}
			if (remainingAmountToSell.Measure > 0.0m)
				throw new Exception("Cannot sell more asset than is available");
		}

		private void PurchaseNewLot(Transaction transaction)
		{
			Lot newLot = new Lot(transaction.Service, transaction.TransactionID, transaction.DateAndTime, transaction.AmountReceived, 
				transaction.MeasurementUnit, 
				new ValueInCurrency(transaction.AmountPaid, transaction.CurrencyUnit, transaction.DateAndTime),
				transaction.AssetType, transaction.Vault, transaction.Account, transaction.ItemType);
			lots.Add(newLot);
		}

		private void ApplyFeeInAsset(Transaction transaction)
		{
			if (transaction.TransactionType != TransactionTypeEnum.FeeInAsset)
				throw new Exception("Wrong transaction type " + transaction.TransactionType + " passed to ApplyFeeInAsset");

			AmountInAsset fee = new AmountInAsset(transaction.DateAndTime, transaction.TransactionID, 
				                            transaction.Vault, transaction.AmountPaid, transaction.MeasurementUnit, 
											transaction.AssetType);
			List<Lot> availableLots = lots.Where(
				s => s.Service == transaction.Service
				&& s.AssetType == fee.AssetType 
				&& s.Account == transaction.Account
				&& s.ItemType == transaction.ItemType
				&& s.IsDepleted() == false)
				.OrderBy(s => s.PurchaseDate).ToList();
			if (transaction.Vault.ToLower() != "any")
				availableLots = availableLots.Where(s => s.Vault == transaction.Vault).ToList();

			foreach(Lot lot in availableLots)
			{
				if (lot.CurrentAmount(fee.MeasurementUnit) >= fee.Amount)
				{
					lot.DecreaseAmountViaFee(transaction.DateAndTime, fee.Amount, fee.MeasurementUnit);
					fee.Decrease(fee.Amount, fee.MeasurementUnit); // fee paid
					break;
				}
				else
				{
					fee.Decrease(lot.CurrentAmount(lot.measurementUnit), lot.measurementUnit); // some fee remaining
					lot.DecreaseAmountViaFee(transaction.DateAndTime, lot.CurrentAmount(lot.measurementUnit), lot.measurementUnit); // lot expended
				}
			}
			if (fee.Amount > 0.0m)
				throw new Exception("Storage fee exceeds available funds");
		}

		private void ApplyStorageFeeInCurrency(Transaction transaction)
		{
			if (transaction.TransactionType != TransactionTypeEnum.FeeInCurrency)
				throw new Exception("Wrong transaction type " + transaction.TransactionType + " passed to ApplyStorageFeeInCurrency");

			// Storage fee can apply to any lot closed within the month
			DateTime limitDate = transaction.DateAndTime.AddDays(-1 * (transaction.DateAndTime.Day - 1));
			
			List<Lot> availableLots = lots.Where(
				                          s => s.Service == transaction.Service
				                          && s.AssetType == transaction.AssetType
				                          && s.Account == transaction.Account
										  && s.ItemType == transaction.ItemType
				                          && s.CloseDate >= limitDate || s.CloseDate == null)
				.OrderBy(s => s.PurchaseDate).ToList();
			if (transaction.Vault.ToLower() != "any")
				availableLots = availableLots.Where(s => s.Vault == transaction.Vault).ToList();

			// Apply fees to the first available lot
			if (availableLots.Count == 0)
				throw new Exception("No available lots to allocate storage fee to");

			availableLots[0].ApplyFeeInCurrency(new ValueInCurrency(transaction.AmountPaid, transaction.CurrencyUnit, transaction.DateAndTime));
		}
	}
}