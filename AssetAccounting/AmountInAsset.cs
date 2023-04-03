namespace AssetAccounting
{
	public class AmountInAsset
	{
		public string Vault { get; set; }
		public decimal Amount { get; set; }
		public AssetTypeEnum AssetType { get; set; }
		public AssetMeasurementUnitEnum MeasurementUnit { get; set; }
		public string TransactionID { get; set; }
		public DateTime Date { get; set; }

		public AmountInAsset(DateTime transationDate, string transactionID, string vault, decimal amount, 
			AssetMeasurementUnitEnum measurementUnit, AssetTypeEnum assetType)
		{
			this.Date = transationDate;
			this.TransactionID = transactionID;
			this.Vault = vault;
			this.Amount = amount;
			this.MeasurementUnit = measurementUnit;
			this.AssetType = assetType;
		}

		public void Decrease(decimal amount, AssetMeasurementUnitEnum fromMeasurementUnit)
		{
			this.Amount -= Utils.ConvertMeasurementUnit(amount, fromMeasurementUnit, this.MeasurementUnit);
			if (this.Amount < 0.0m)
				throw new Exception("Cannot decrease storage fee less than 0");
		}
	}
}

