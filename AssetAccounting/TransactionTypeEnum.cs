namespace AssetAccounting
{
	public enum TransactionTypeEnum
	{
		FeeInAsset,
		FeeInCurrency,
		Purchase,
		PurchaseViaExchange, // Heterogenous asset-to-asset exchange (ex. gold-to-silver) forces a sale and purchase
        Sale,
		SaleViaExchange,
		TransferOut,
		TransferIn,
        IncomeInCurrency, // interest payment, bonus, etc
        IncomeInAsset, // interest or bonus paid in asset (metal, crypto, etc)
        Indeterminate // Cannot determine with info provided - will determine in later phase. Use in parsers only.
	}
}

