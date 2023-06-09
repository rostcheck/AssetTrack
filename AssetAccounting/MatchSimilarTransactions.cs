﻿namespace AssetAccounting
{
	public class MatchSimilarTransactions : ITransactionListProcessor
	{
		public MatchSimilarTransactions()
		{
		}

		// Like transfers, like kind exchanges become one transfer transaction (the receiving side) and a 
		// storage fee (on the sending side account) that accounts for any effect on basis.
		public List<Transaction> FormLikeKindExchanges(List<Transaction> transactionList, ILogWriter writer)
		{
			writer.WriteEntry("\nIdentifying like kind exchanges using similar transactions algorithm:");
			List<Transaction> transactionsToRemove = new List<Transaction>();
			string formatString = "Matched {0} {1} from {2} on {3} of {4} {5} (transaction ID {6}) with {7} to {8} on {9} of {10} {11} (transaction ID {12})";				
			List<Transaction> sourceTransactions = new List<Transaction>();

			foreach (Transaction sourceTransaction in transactionList
				.Where(s => s.TransactionType == TransactionTypeEnum.Sale)
				.OrderBy(s => s.DateAndTime))
			{
				Transaction? receiveTransaction = GetPossibleLikeKindTransaction(sourceTransaction, transactionList);
				if (receiveTransaction is null)
					continue; // no match

				string message = string.Format(formatString, sourceTransaction.AssetType, sourceTransaction.TransactionType, 
					sourceTransaction.Service, sourceTransaction.DateAndTime.ToShortDateString(),
					sourceTransaction.Measure, sourceTransaction.MeasurementUnit, sourceTransaction.TransactionID,
					receiveTransaction.TransactionType, receiveTransaction.Service, 
					receiveTransaction.DateAndTime.ToShortDateString(), receiveTransaction.Measure, 
					receiveTransaction.MeasurementUnit, receiveTransaction.TransactionID);
				writer.WriteEntry(message);
				if (receiveTransaction.AmountReceived != sourceTransaction.AmountPaid)
				{
					decimal amountDifference = sourceTransaction.AmountPaid - Utils.ConvertMeasurementUnit(receiveTransaction.AmountReceived,
						receiveTransaction.MeasurementUnit, sourceTransaction.MeasurementUnit);
					// Create an asset storage fee to account for the difference
					Transaction storageFee = new Transaction(sourceTransaction.Service, sourceTransaction.Account,
						sourceTransaction.DateAndTime, sourceTransaction.TransactionID, TransactionTypeEnum.FeeInAsset,
						sourceTransaction.Vault, amountDifference, sourceTransaction.CurrencyUnit, 0.0m, 
						sourceTransaction.MeasurementUnit, sourceTransaction.AssetType, 
						"Transfer fee (in asset) from like-kind exchange " + sourceTransaction.TransactionID, "Generic",
						sourceTransaction.SpotPrice);
					transactionList.Add(storageFee);
				}

				// Set the source vault property in the receipt side
				receiveTransaction.MakeTransfer(sourceTransaction.Service, sourceTransaction.Account, sourceTransaction.Vault);
				sourceTransactions.Add(sourceTransaction);
			}
			foreach (Transaction sourceTransaction in transactionsToRemove)
				transactionList.Remove(sourceTransaction);
			writer.WriteEntry("Finished identifying like kind exchanges.");
			return transactionList;
		}

		// A similar transaction is a later purchase within 30 days of the sale transaction with the same
		// asset and within 10% of the price. May returns null.
		private Transaction? GetPossibleLikeKindTransaction(Transaction transaction, List<Transaction> transactionList)
		{
			TransactionTypeEnum oppositeTransactionType = transaction.GetOppositeTransactionType();
			if (oppositeTransactionType != TransactionTypeEnum.Purchase && oppositeTransactionType != TransactionTypeEnum.Sale)
				return null;
			else
			{
				Transaction? returnTransaction = transactionList.Where(
					s => s.TransactionType == TransactionTypeEnum.Purchase
					&& s.DateAndTime <= (transaction.DateAndTime + new TimeSpan(30, 0, 0, 0))
					&& s.DateAndTime >= transaction.DateAndTime
					&& s.AssetType == transaction.AssetType
					&& s.TransactionType == oppositeTransactionType
					&& s.ItemType == transaction.ItemType
					&& s.AmountReceived >= (.9m * transaction.AmountPaid))
						.OrderBy(s => s.DateAndTime).FirstOrDefault();

				// Prefer later transactions, but an earlier one may qualify
				if (returnTransaction == null)
					returnTransaction = transactionList.Where(
						s => s.TransactionType == TransactionTypeEnum.Purchase
						&& s.DateAndTime >= (transaction.DateAndTime - new TimeSpan(30, 0, 0, 0))
						&& s.DateAndTime <= transaction.DateAndTime
						&& s.AssetType == transaction.AssetType
						&& s.TransactionType == oppositeTransactionType
						&& s.ItemType == transaction.ItemType
						&& s.AmountReceived >= (.9m * transaction.AmountPaid))
						.OrderBy(s => s.DateAndTime).FirstOrDefault();

				return returnTransaction;
			}
		}
	}
}


