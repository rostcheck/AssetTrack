namespace AssetAccounting
{
	public static class Utils
	{
		const decimal GramsPerTroyOz = 31.1034768m;

		public static decimal ConvertCurrency(decimal amount, CurrencyUnitEnum fromCurrency, CurrencyUnitEnum toCurrency)
		{
			if (fromCurrency == toCurrency)
				return amount;
			else
				throw new NotImplementedException("Currency conversion not implemented");
		}

		public static decimal ConvertMeasurementUnit(decimal measurement, AssetMeasurementUnitEnum fromUnits, AssetMeasurementUnitEnum toUnits)
		{
			if (fromUnits == toUnits)			
				return measurement;
			else			
				return ConvertFromGrams(ConvertToGrams(measurement, fromUnits), toUnits); 
		}
			
		private static decimal ConvertToGrams(decimal weight, AssetMeasurementUnitEnum fromUnits)
		{
			switch (fromUnits)
			{
				case AssetMeasurementUnitEnum.Gram:
					return weight;
				case AssetMeasurementUnitEnum.TroyOz:
					return weight * GramsPerTroyOz;
				default:
					throw new Exception("Unknown weight unit " + fromUnits);
			}
		}

		private static decimal ConvertFromGrams(decimal weightInGrams, AssetMeasurementUnitEnum toUnits)
		{
			switch (toUnits)
			{
				case AssetMeasurementUnitEnum.Gram:
					return weightInGrams;
				case AssetMeasurementUnitEnum.TroyOz:
					return weightInGrams / GramsPerTroyOz;
				default:
					throw new Exception("Unknown weight unit " + toUnits);
			}
		}
	}
}

