using AssetAccounting;

namespace AssetTrack
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: AssetTrack <filename> [filenames...]");
				return;
			}
			Console.WriteLine("\nStarting run at {0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());

			ConsoleLogWriter writer = new ConsoleLogWriter();
			AssetStorageService storageService = new AssetStorageService(writer);
			GoldMoneyParser goldMoneyParser = new GoldMoneyParser();
			BullionVaultParser bullionVaultParser = new BullionVaultParser();
			GenericCsvParser genericCsvParser = new GenericCsvParser();
			List<Transaction> transactionList = new List<Transaction>();
			foreach (string filename in args)
			{
				if (filename.Contains("tm-"))
					continue;
				else
				{
					Console.WriteLine("Read transactions from {0}", filename);
					if (filename.Contains("GoldMoney"))
						transactionList.AddRange(goldMoneyParser.Parse(filename));
					else if (filename.Contains("BullionVault"))
						transactionList.AddRange(bullionVaultParser.Parse(filename));
					else
						transactionList.AddRange(genericCsvParser.Parse(filename));
				}
			}
			transactionList = transactionList.OrderBy(s => s.DateAndTime).ToList();
			storageService.ApplyTransactions(transactionList);
			PrintResults(storageService);
			DumpTransactions("tm-transactions.txt", transactionList);
			ExportLots(storageService.Lots, "tm-lots.txt");
			ExportHoldings(storageService.Lots, "tm-holdings.txt");
			//string command = "";
			//do {
			//	ProcessCommand(command, storageService);
			//	command = GetString("command: ");
			//} while (command != "quit");
		}

		private static void ProcessCommand(string command, AssetStorageService storageService)
		{
			string[] args = command.Split(' ');
			if (args.Length < 1)
				return;

			switch (args[0])
			{
				case "lot":
					ShowLot(args[1], storageService);
					break;
				case "help":
				default:
					Console.WriteLine("Unknown command");
					Console.WriteLine("Commands: help, lot");
					break;
			}
		}

		private static void ShowLot(string lotID, AssetStorageService storageService)
		{
			Lot? thisLot = storageService.Lots.Where(s => s.LotID == lotID).FirstOrDefault();
			if (thisLot is not null)
			{
				foreach (string entry in thisLot.History)
					Console.WriteLine(entry);
			}
		}

		private static string GetString(string userPrompt)
		{
			string result = string.Empty;
			Console.Write(userPrompt);
			ConsoleKeyInfo key = Console.ReadKey();
			while (key.Key != ConsoleKey.Enter)
			{
				result += key.KeyChar;
				key = Console.ReadKey();
			}
			return result;
		}

		public static void PrintResults(AssetStorageService storageService)
		{
			Console.WriteLine();
			Console.WriteLine("Wrote list of all transactions to tm-transactions.txt.");
			Console.WriteLine("Wrote capital gains to tm-gains.txt files (by year).");
			ExportCapitalGains(storageService.Sales);

			Console.WriteLine();
			Console.WriteLine("Remaining lots:");
			string formatString = "Lot ID {0} @ {1} in {2}: bought {3}, remaining {4} {5} {6} {7}";
			foreach (Lot lot in storageService.Lots.Where(s => !s.IsDepleted())
				.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, lot.LotID, lot.Service, lot.Vault, lot.PurchaseDate.ToShortDateString(),
					lot.CurrentAmount(lot.measurementUnit), lot.measurementUnit, lot.AssetType, lot.ItemType);
				Console.WriteLine(formatted);
				ShowLot(lot.LotID, storageService);
			}
		}

		public static void PrintCapitalGains(List<TaxableSale> sales)
		{
			string formatString = "{0} Lot ID {1}: Bought {2} {3} ({4}), sold {5} {6} for ${7:0.00}, adjusted basis ${8:0.00}, net gain ${9:0.00}";

			foreach (TaxableSale sale in sales.OrderBy(s => s.PurchaseDate).ToList())
			{
				string formatted = string.Format(formatString, sale.Service, sale.LotID, sale.AssetType.ToString().ToLower(),
					sale.PurchaseDate.ToShortDateString(), sale.SaleMeasure,	sale.SaleDate.ToShortDateString(), 
					sale.SalePrice.Value, sale.AdjustedBasis.Value, sale.SalePrice.Value - sale.AdjustedBasis.Value);
				Console.WriteLine(formatted);
			}
		}

		public static void ExportCapitalGains(List<TaxableSale> sales)
		{
			var years = sales.Select(s => s.SaleDate.Year).Distinct();
			foreach (var year in years)
			{
				StreamWriter sw = new StreamWriter(string.Format("tm-gains-{0}.txt", year));
				sw.WriteLine("Service\tLot ID\tAsset\tItemType\tBought Date\tSold Date\tAdjusted Basis\tSale Price\tNet Gain");
				string formatString = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6:0.00}\t{7:0.00}\t{8:0.00}";

				foreach (TaxableSale sale in sales.Where(s => s.SaleDate.Year == year)
					.OrderBy(s => s.Service).ThenBy(s => s.ItemType).ThenBy(s => s.PurchaseDate).ToList())
				{
					string formatted = string.Format(formatString, sale.Service,sale.LotID, 
						sale.AssetType.ToString().ToLower(), sale.ItemType, 
						sale.PurchaseDate.ToShortDateString(), sale.SaleDate.ToShortDateString(), 
						sale.AdjustedBasis.Value, sale.SalePrice.Value, sale.SalePrice.Value - sale.AdjustedBasis.Value);
					sw.WriteLine(formatted);
				}
				sw.Close();				
			}
		}

		private static void DumpTransactions(string filename, List<Transaction> transactions)
		{
			StreamWriter sw = new StreamWriter(filename);
			sw.WriteLine("Date\tService\tType\tAsset\tMeasure\tUnit\t\tItemType\tAccount\tAmountPaid\tAmountReceived\tCurrency\tVault\tTransactionId\tMemo");
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

		private static void ExportLots(List<Lot> lots, string filename)
        {
			StreamWriter sw = new StreamWriter(filename);
			sw.WriteLine("Date\tLotID\tAsset\tOriginalMeasure\tCurrentMeasure\tUnit\tItemType\tAccount\tService\tVault\tOriginalBasis\tCurrentBasis\tCurrency");
			string formatString = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}";

			foreach (var lot in lots.Where(s => s.IsDepleted() == false).OrderBy(s => s.PurchaseDate))
            {
				string formatted = string.Format(formatString, lot.PurchaseDate, lot.LotID, lot.AssetType, lot.OriginalAmount, lot.CurrentAmount(lot.measurementUnit), lot.measurementUnit,
					lot.ItemType, lot.Account, lot.Service, lot.Vault, lot.OriginalPrice.Value, lot.AdjustedPrice.Value, lot.AdjustedPrice.Currency);
				sw.WriteLine(formatted);
			}
			sw.Close();
		}

		// Holdings are all summed lots, regardless of where stored (ex. all gold, silver, etc) by ItemType
		private static void ExportHoldings(List<Lot> lots, string filename)
		{
			StreamWriter sw = new StreamWriter(filename);
			sw.WriteLine("Metal\tItemType\tCurrentMeasure\tUnit\tCurrentBasis\tCurrency");
			string formatString = "{0}\t{1}\t{2}\t{3}\t{4:0.######}\t{5}";

			var currentBasis = 0.0m;
			var currentWeight = 0.0m;
			Lot? lastLot = null;
			string currentAssetType = "", currentItemType = "";
			AssetMeasurementUnitEnum currentWeightUnit = AssetMeasurementUnitEnum.CryptoCoin;
			CurrencyUnitEnum currentCurrencyUnit = CurrencyUnitEnum.USD;
			foreach (var lot in lots.Where(s => s.IsDepleted() == false).OrderBy(s => s.AssetType).ThenBy(s => s.ItemType))
			{
				if (lot.AssetType.ToString() != currentAssetType || lot.ItemType != currentItemType)
				{
					if (lastLot is not null)
						sw.WriteLine(string.Format(formatString, currentAssetType, currentItemType, currentWeight, currentWeightUnit.ToString(), currentBasis, currentCurrencyUnit));
					currentBasis = lot.AdjustedPrice.Value;
					currentWeight = lot.CurrentAmount(lot.measurementUnit);
					currentAssetType = lot.AssetType.ToString();
					currentItemType = lot.ItemType;
					currentWeightUnit = lot.measurementUnit;
					currentCurrencyUnit = lot.AdjustedPrice.Currency;
				}
				else
				{
					currentBasis += lot.AdjustedPrice.Value;
					currentWeight += lot.CurrentAmount(currentWeightUnit);
				}
				lastLot = lot;
			}
			sw.WriteLine(string.Format(formatString, currentAssetType, currentItemType, currentWeight, currentWeightUnit.ToString(), currentBasis, currentCurrencyUnit));
			sw.Close();
		}
	}
}
