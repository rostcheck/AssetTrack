using static AssetAccounting.Utils;

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

        public static decimal? GetSpotPrice(decimal currencyAmount, decimal assetAmount)
        {
            if (assetAmount == 0.0m || currencyAmount == 0.0m)
                return null;
            else
                return Math.Abs(currencyAmount / assetAmount);
        }

		public struct Amounts
		{
			public Amounts()
			{
                amountPaid = 0.0m;
                amountReceived = 0.0m;
            }
			public decimal amountPaid;
			public decimal amountReceived ;
		}

		public static Amounts GetAmounts(TransactionTypeEnum transactionType, decimal amount, decimal currencyAmount)
		{
			var m = new Amounts();
			if (transactionType == TransactionTypeEnum.Purchase || transactionType == TransactionTypeEnum.IncomeInAsset)
			{
				m.amountPaid = Math.Abs(currencyAmount);
				m.amountReceived = Math.Abs(amount);
			}
			else if (transactionType == TransactionTypeEnum.Sale)
			{
				m.amountPaid = Math.Abs(amount);
				m.amountReceived = Math.Abs(currencyAmount);
			}
			else if (transactionType == TransactionTypeEnum.TransferIn)
				m.amountReceived = Math.Abs(amount);
			else if (transactionType == TransactionTypeEnum.TransferOut)
				m.amountPaid = Math.Abs(amount);
			else if (transactionType == TransactionTypeEnum.FeeInCurrency)
				m.amountPaid = Math.Abs(currencyAmount);
			else if (transactionType == TransactionTypeEnum.IncomeInCurrency)
				m.amountReceived = Math.Abs(currencyAmount);
			else if (transactionType == TransactionTypeEnum.IncomeInAsset)
			{
				m.amountPaid = Math.Abs(currencyAmount);
				m.amountReceived = Math.Abs(currencyAmount); // Basis
			}
			else
				throw new Exception("Unknown transaction type " + transactionType);
			return m;
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