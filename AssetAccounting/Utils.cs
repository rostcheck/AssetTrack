using System;
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

		public static (decimal amountPaid, decimal amountReceived) GetAmounts(TransactionTypeEnum transactionType, decimal amount, decimal currencyAmount)
		{
			decimal amountPaid = 0.0m;
            decimal amountReceived = 0.0m;
            if (transactionType == TransactionTypeEnum.Purchase || transactionType == TransactionTypeEnum.IncomeInAsset)
			{
				amountPaid = Math.Abs(currencyAmount);
				amountReceived = Math.Abs(amount);
			}
			else if (transactionType == TransactionTypeEnum.Sale)
			{
				amountPaid = Math.Abs(amount);
				amountReceived = Math.Abs(currencyAmount);
			}
			else if (transactionType == TransactionTypeEnum.TransferIn)
				amountReceived = Math.Abs(amount);
			else if (transactionType == TransactionTypeEnum.TransferOut)
				amountPaid = Math.Abs(amount);
			else if (transactionType == TransactionTypeEnum.FeeInCurrency)
				amountPaid = Math.Abs(currencyAmount);
			else if (transactionType == TransactionTypeEnum.IncomeInCurrency)
				amountReceived = Math.Abs(currencyAmount);
			else if (transactionType == TransactionTypeEnum.FeeInAsset)
				amountPaid = Math.Abs(amount);
			else if (transactionType == TransactionTypeEnum.IncomeInAsset)
			{
				amountPaid = Math.Abs(currencyAmount);
				amountReceived = Math.Abs(currencyAmount); // Basis
			}
			else
				throw new Exception("Unknown transaction type " + transactionType);
			return (amountPaid, amountReceived);
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