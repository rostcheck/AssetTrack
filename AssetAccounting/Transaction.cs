namespace AssetAccounting
{
	public class Transaction
	{
		public string Service { get; set; }
		public string Account { get; set; }
		public DateTime DateAndTime { get; set; }
		public string TransactionID { get; set; }
		public TransactionTypeEnum TransactionType { get; set; }
		public string Vault { get; set; }
		public decimal AmountPaid { get; set; }
		public decimal AmountReceived { get; set; }
		public AssetMeasurementUnitEnum MeasurementUnit { get; set; }
		public string Memo { get; set; }
		public CurrencyUnitEnum CurrencyUnit { get; set; }
		public AssetTypeEnum AssetType { get; set; }
		public string? TransferFromVault { get; set; }
		public string? TransferFromAccount { get; set; }
        public string? TransferFromService { get; set; }
        public string ItemType { get; set; }
		public decimal? SpotPrice { get; set; }

		public Transaction(string service, string account, DateTime dateAndTime, string transactionID, 
			TransactionTypeEnum transactionType, string vault, decimal amountPaid, CurrencyUnitEnum currencyUnit, 
			decimal amountReceived, AssetMeasurementUnitEnum measurementUnit, AssetTypeEnum assetType, string memo, 
			string itemType, decimal? spotPrice)
		{
			this.Service = service;
			this.Account = account;
			this.DateAndTime = dateAndTime;
			this.TransactionID = transactionID;
			this.TransactionType = transactionType;
			this.Vault = vault;
			this.AmountPaid = amountPaid;
			this.CurrencyUnit = currencyUnit;
			this.AmountReceived = amountReceived;
			this.MeasurementUnit = measurementUnit;
			this.AssetType = assetType;
			this.Memo = memo;
			this.ItemType = itemType;
			this.SpotPrice = spotPrice;
		}

		public decimal Measure
		{
			get
			{
				switch (this.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
					case TransactionTypeEnum.TransferIn:
					case TransactionTypeEnum.IncomeInAsset:
						return AmountReceived;
					case TransactionTypeEnum.TransferOut:
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
					case TransactionTypeEnum.FeeInAsset:
						return AmountPaid;
					default:
						return 0.0m;
				}
			}

			set
			{
				switch (this.TransactionType)
				{
					case TransactionTypeEnum.Purchase:
					case TransactionTypeEnum.PurchaseViaExchange:
						AmountReceived = value;
						break;
					case TransactionTypeEnum.Sale:
					case TransactionTypeEnum.SaleViaExchange:
					case TransactionTypeEnum.FeeInAsset:
						AmountPaid = value;
						break;
				}
			}
		}

		public decimal GetMeasureInUnits(AssetMeasurementUnitEnum toMeasurementUnit)
		{
			return Utils.ConvertMeasurementUnit(Measure, MeasurementUnit, toMeasurementUnit);
		}

		public Transaction Duplicate()
		{
			return new Transaction(Service, Account, DateAndTime, TransactionID, TransactionType,
				Vault, AmountPaid, CurrencyUnit, AmountReceived, MeasurementUnit, AssetType, Memo, ItemType, SpotPrice);
		}

		public void MakeTransfer(string service, string account, string vault)
		{
			this.TransactionType = TransactionTypeEnum.TransferIn;
			this.TransferFromService = service;
			this.TransferFromAccount = account;
			this.TransferFromVault = vault;
		}

		// Returns TransactionTypeEnum.Indeterminate if the type has no opposite
		public TransactionTypeEnum GetOppositeTransactionType()
		{
			switch (TransactionType)
			{
				case TransactionTypeEnum.Purchase:
					return TransactionTypeEnum.Sale;
				case TransactionTypeEnum.Sale:
					return TransactionTypeEnum.Purchase;
				case TransactionTypeEnum.PurchaseViaExchange:
					return TransactionTypeEnum.SaleViaExchange;
				case TransactionTypeEnum.SaleViaExchange:
					return TransactionTypeEnum.PurchaseViaExchange;
				default:
					return TransactionTypeEnum.Indeterminate;
			}
		}
	}
}

