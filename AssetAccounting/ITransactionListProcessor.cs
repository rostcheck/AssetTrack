namespace AssetAccounting
{
	public interface ITransactionListProcessor
	{
		List<Transaction> FormLikeKindExchanges(List<Transaction> transactionList, ILogWriter writer);
	}
}

