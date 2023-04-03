namespace AssetAccounting
{
	public class TaxableSale
	{
		public string LotID { get; set; }
		public DateTime PurchaseDate { get; set; }
		public DateTime SaleDate { get; set; }
		public ValueInCurrency AdjustedBasis { get; set; }
		public AssetMeasurementUnitEnum MeasurementUnit { get; set; }
		public AssetTypeEnum AssetType { get; set; }
		public decimal SaleMeasure { get; set; }
		public ValueInCurrency SalePrice { get; set; }
		public string Service { get; set; }
		public string ItemType { get; set; }

		public TaxableSale(Lot fromLot, AssetAmount amount, ValueInCurrency salePrice)
		{
			this.LotID = fromLot.LotID;
			this.Service = fromLot.Service;
			this.PurchaseDate = fromLot.PurchaseDate;
			this.MeasurementUnit = fromLot.measurementUnit;
			decimal saleMeasure = Utils.ConvertMeasurementUnit(amount.Measure, amount.MeasurementUnit, this.MeasurementUnit);
			decimal percentageOfLot = saleMeasure / fromLot.CurrentAmount(fromLot.measurementUnit);
			this.AdjustedBasis = new ValueInCurrency(percentageOfLot * fromLot.AdjustedPrice.Value,
				fromLot.AdjustedPrice.Currency, fromLot.AdjustedPrice.Date);
			this.AssetType = fromLot.AssetType;
			this.SaleMeasure = Utils.ConvertMeasurementUnit(amount.Measure, amount.MeasurementUnit, this.MeasurementUnit);
			this.SalePrice = new ValueInCurrency(salePrice);
			this.SaleDate = salePrice.Date;
			this.ItemType = amount.ItemType;
		}
	}
}

