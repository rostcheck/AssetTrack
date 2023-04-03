namespace AssetAccounting
{
	public class AssetAmount
	{
		public decimal Measure { get; set; }
		public AssetTypeEnum AssetType { get; set; }
		public string ItemType { get; set; }
		public AssetMeasurementUnitEnum MeasurementUnit { get; set; }

		public AssetAmount(decimal measure, AssetTypeEnum assetType, AssetMeasurementUnitEnum measurementUnit, string itemType)
		{
			this.Measure = measure;
			this.AssetType = assetType;
			this.MeasurementUnit = measurementUnit;
			this.ItemType = itemType;
		}

		public static AssetAmount operator -(AssetAmount amount1, AssetAmount amount2)
		{
			if (amount1.AssetType != amount2.AssetType)
				throw new Exception(string.Format("Cannot subtract different asset types: {0} and {1}", amount1.AssetType, amount2.AssetType));

			if (amount1.ItemType != amount2.ItemType)
				throw new Exception(string.Format("Cannot subtract different item types: {0} and {1}", amount1.ItemType, amount2.ItemType));

			decimal measureToSubtract = Utils.ConvertMeasurementUnit(amount2.Measure, amount2.MeasurementUnit, amount1.MeasurementUnit);
			return new AssetAmount(amount1.Measure - measureToSubtract, amount1.AssetType, amount1.MeasurementUnit, amount1.ItemType);
		}
	}
}

