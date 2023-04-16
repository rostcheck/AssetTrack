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

		public static void DumpTransactions(string filename, List<Transaction> transactions)
        {
            StreamWriter sw = new StreamWriter(filename);
            sw.WriteLine("Date\tService\tType\tAsset\tMeasure\tUnit\tItemType\tAccount\tAmountPaid\tAmountReceived\tCurrency\tVault\tTransactionId\tSpotPrice\tMemo");
            string formatString = "\"{0}\"\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}";

            foreach (var transaction in transactions.OrderBy(s => s.DateAndTime).ToList())
            {
                string formatted = string.Format(formatString, transaction.DateAndTime, transaction.Service,
                    transaction.TransactionType.ToString(),
                    transaction.AssetType.ToString().ToLower(),
                    transaction.Measure, transaction.MeasurementUnit, transaction.ItemType,
                    transaction.Account, transaction.AmountPaid, transaction.AmountReceived,
                    transaction.CurrencyUnit, transaction.Vault, transaction.TransactionID, transaction.SpotPrice,
                    string.Format("\"{0}\"", transaction.Memo));
                sw.WriteLine(formatted);
            }
            sw.Close();
        }

        public static void ExportLots(List<Lot> lots, string filename)
        {
            StreamWriter sw = new StreamWriter(filename);
            sw.WriteLine("Date\tLotID\tAsset\tOriginalMeasure\tCurrentMeasure\tUnit\tItemType\tAccount\tService\tVault\tOriginalBasis\tCurrentBasis\tCurrency");
            string formatString = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}";

            foreach (var lot in lots.Where(s => s.IsDepleted() == false).OrderBy(s => s.PurchaseDate))
            {
                string formatted = string.Format(formatString, lot.PurchaseDate, lot.LotID, lot.AssetType, lot.OriginalAmount, lot.CurrentAmount(lot.measurementUnit), lot.measurementUnit,
                    lot.ItemType, lot.Account, lot.Service, lot.Vault, lot.OriginalPrice.Value, lot.AdjustedPrice.Value, lot.AdjustedPrice.Currency);
                sw.WriteLine(formatted);
            }
            sw.Close();
        }

        // Holdings are all summed lots, regardless of where stored (ex. all gold, silver, etc) by ItemType
        public static void ExportHoldings(List<Lot> lots, string filename)
        {
            StreamWriter sw = new StreamWriter(filename);
            sw.WriteLine("Metal\tItemType\tCurrentMeasure\tUnit\tCurrentBasis\tCurrency");
            string formatString = "{0}\t{1}\t{2}\t{3}\t{4:0.######}\t{5}";

            var currentBasis = 0.0m;
            var currentWeight = 0.0m;
            Lot? lastLot = null;
            string currentAssetType = "", currentItemType = "";
            AssetMeasurementUnitEnum currentWeightUnit = AssetMeasurementUnitEnum.CryptoCoin;
            CurrencyUnitEnum currentCurrencyUnit = CurrencyUnitEnum.USD;
            foreach (var lot in lots.Where(s => s.IsDepleted() == false).OrderBy(s => s.AssetType).ThenBy(s => s.ItemType))
            {
                if (lot.AssetType.ToString() != currentAssetType || lot.ItemType != currentItemType)
                {
                    if (lastLot is not null)
                        sw.WriteLine(string.Format(formatString, currentAssetType, currentItemType, currentWeight, currentWeightUnit.ToString(), currentBasis, currentCurrencyUnit));
                    currentBasis = lot.AdjustedPrice.Value;
                    currentWeight = lot.CurrentAmount(lot.measurementUnit);
                    currentAssetType = lot.AssetType.ToString();
                    currentItemType = lot.ItemType;
                    currentWeightUnit = lot.measurementUnit;
                    currentCurrencyUnit = lot.AdjustedPrice.Currency;
                }
                else
                {
                    currentBasis += lot.AdjustedPrice.Value;
                    currentWeight += lot.CurrentAmount(currentWeightUnit);
                }
                lastLot = lot;
            }
            sw.WriteLine(string.Format(formatString, currentAssetType, currentItemType, currentWeight, currentWeightUnit.ToString(), currentBasis, currentCurrencyUnit));
            sw.Close();
        }
    }
}