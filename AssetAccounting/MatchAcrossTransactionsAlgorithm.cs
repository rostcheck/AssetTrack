﻿namespace AssetAccounting
{
	// Match sales by looking for any buy transaction winin +- 30 days with the same asset type
	public class MatchAcrossTransactionsAlgorithm : ITransactionListProcessor
	{
		public MatchAcrossTransactionsAlgorithm()
		{
		}

		// Like transfers, like kind exchanges become one transfer transaction (the receiving side) and a 
		// storage fee (on the sending side account) that accounts for any effect on basis.
		public List<Transaction> FormLikeKindExchanges(List<Transaction> transactionList, ILogWriter writer)
		{
			writer.WriteEntry("\nIdentifying like kind exchanges using match-across-transactions algorithm:");
			List<Transaction> transactionsToRemove = new List<Transaction>();
			string formatString = "Matched {0} {1} from {2} on {3} of {4} {5} (transaction ID {6}) with {7} to {8} on {9} of {10} {11} (transaction ID {12})";				
			foreach (Transaction sourceTransaction in transactionList
				.Where(s => s.TransactionType == TransactionTypeEnum.Sale)
				.OrderBy(s => s.DateAndTime))
			{
				List<Transaction>? possibleExchanges = GetPossibleLikeKindTransactions(sourceTransaction, transactionList);
				if (possibleExchanges is null)
					continue;

				AmountInAsset amountToMatch = new AmountInAsset(sourceTransaction.DateAndTime, sourceTransaction.TransactionID, 
					sourceTransaction.Vault, sourceTransaction.AmountPaid, sourceTransaction.MeasurementUnit, 
					sourceTransaction.AssetType);
				int exchangeIndex = 0;
				while (amountToMatch.Amount > 0.0m && exchangeIndex < possibleExchanges.Count)
				{
					Transaction receiveTransaction = possibleExchanges[exchangeIndex];
					string message = string.Format(formatString, sourceTransaction.AssetType, sourceTransaction.TransactionType, 
						sourceTransaction.Service, sourceTransaction.DateAndTime.ToShortDateString(),
						sourceTransaction.Measure, sourceTransaction.MeasurementUnit, sourceTransaction.TransactionID,
						receiveTransaction.TransactionType, receiveTransaction.Service, 
						receiveTransaction.DateAndTime.ToShortDateString(), receiveTransaction.Measure, 
						receiveTransaction.MeasurementUnit, receiveTransaction.TransactionID);
					writer.WriteEntry(message);
					decimal leftover = receiveTransaction.GetMeasureInUnits(sourceTransaction.MeasurementUnit) - amountToMatch.Amount;
					if (leftover >= 0.0m)
					{
						// Split transaction into a like-kind transfer and a remaining unmatched part
						if (leftover > 0.0m)
						{
							Transaction remainingTransaction = receiveTransaction.Duplicate();
							remainingTransaction.AmountReceived = leftover;
							transactionList.Add(remainingTransaction);
						}
						receiveTransaction.MakeTransfer(sourceTransaction.Service, sourceTransaction.Account, sourceTransaction.Vault);
						receiveTransaction.AmountReceived = sourceTransaction.AmountPaid;
						break;
					}
					else
					{
						amountToMatch.Decrease(amountToMatch.Amount, amountToMatch.MeasurementUnit);
						receiveTransaction.MakeTransfer(sourceTransaction.Service, sourceTransaction.Account, sourceTransaction.Vault);
					}
					exchangeIndex++;
				}

				if (amountToMatch.Amount > 0.0m)
				{
					// This could potentially be treated as a transfer fee, depending on size
					sourceTransaction.Measure = amountToMatch.Amount;
				}
				else
				{
					// Transaction completely eliminated in the transfer
					transactionsToRemove.Add(sourceTransaction);
				}
			}
			foreach (Transaction sourceTransaction in transactionsToRemove)
				transactionList.Remove(sourceTransaction);
			writer.WriteEntry("Finished identifying like kind exchanges.");
			return transactionList;
		}

		// Returns null if there are no possible like kind transactions
		private List<Transaction>? GetPossibleLikeKindTransactions(Transaction transaction, List<Transaction> transactionList)
		{
			TransactionTypeEnum oppositeTransactionType = transaction.GetOppositeTransactionType();
			if (oppositeTransactionType != TransactionTypeEnum.Purchase && oppositeTransactionType != TransactionTypeEnum.Sale)
				return null;
			else
				return transactionList.Where(
					s => s.DateAndTime > transaction.DateAndTime - new TimeSpan(30, 0, 0, 0)
					&& s.DateAndTime < transaction.DateAndTime + new TimeSpan(30, 0, 0, 0)
					&& s.AssetType == transaction.AssetType
					&& s.ItemType == transaction.ItemType
					&& s.Vault != transaction.Vault
					&& s.TransactionType == oppositeTransactionType)
						.OrderBy(s => s.DateAndTime).ToList();
		}
	}
}