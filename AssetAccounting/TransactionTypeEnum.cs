namespace AssetAccounting
{
	public enum TransactionTypeEnum
	{
		StorageFeeInAsset,
		StorageFeeInCurrency,
		Purchase,
		PurchaseViaExchange, // Heterogenous asset-to-asset exchange (ex. gold-to-silver) forces a sale and purchase
        Sale,
		SaleViaExchange,
		TransferOut,
		TransferIn,
		Indeterminate // Cannot determine with info provided - will determine in later phase. Use in parsers only.
	}
}

