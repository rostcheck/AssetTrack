namespace AssetAccounting
{
	public class GenericCsvParser : ParserBase, IFileParser
	{
		public GenericCsvParser() : base("GenericCsv")
		{
		}

		public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
			DateTime dateAndTime = DateTime.Parse(fields[0]);
			string vault = fields[1];
			string transactionID = fields[2];
			string transactionTypeString = fields[3];
			TransactionTypeEnum transactionType = GetTransactionType(transactionTypeString);
			decimal currencyAmount = Convert.ToDecimal(fields[4].Replace("$", ""));
			CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[5]);
			decimal weight = Convert.ToDecimal(fields[6]);
			AssetMeasurementUnitEnum weightUnit = GetWeightUnit(fields[7]);
			string itemType = "Generic";
			if (fields.Count > 12)
				itemType = fields[12];

			decimal amountPaid = 0.0m, amountReceived = 0.0m;
			if (transactionType == TransactionTypeEnum.Purchase)
			{
				amountPaid = currencyAmount;
				amountReceived = weight;
			}
			else if (transactionType == TransactionTypeEnum.Sale)
			{
				amountPaid = weight;
				amountReceived = currencyAmount;
			}
			else if (transactionType == TransactionTypeEnum.TransferIn)
				amountReceived = weight;
			else if (transactionType == TransactionTypeEnum.TransferOut)
				amountPaid = weight;
			else if (transactionType == TransactionTypeEnum.StorageFeeInCurrency)
				amountPaid = Math.Abs(currencyAmount);
			else 
				throw new Exception("Unknown transaction type " + transactionType);

			AssetTypeEnum assetType = GetAssetType(fields[8]);

			return new Transaction(serviceName, accountName, dateAndTime,
				transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived,
				weightUnit, assetType, "", itemType);
		}

		private static CurrencyUnitEnum GetCurrencyUnit(string currencyUnit)
		{
			switch (currencyUnit.ToUpper())
			{
				case "USD":
					return CurrencyUnitEnum.USD;
				default:
					throw new Exception("Unrecognized currency unit " + currencyUnit);
			}
		}

		private static AssetMeasurementUnitEnum GetWeightUnit(string weightUnit)
		{
			switch (weightUnit.ToUpper())
			{
				case "OZ":
				case "TROYOZ":
					return AssetMeasurementUnitEnum.TroyOz;
				case "G":
					return AssetMeasurementUnitEnum.Gram;
				case "CRYPTOCOIN":
					return AssetMeasurementUnitEnum.CryptoCoin;
				default:
					throw new Exception("Unrecognized weight unit " + weightUnit);
			}
		}

		private static AssetTypeEnum GetAssetType(string assetType)
		{
			switch (assetType.ToLower())
			{
				case "gold":
					return AssetTypeEnum.Gold;
				case "silver":
					return AssetTypeEnum.Silver;
				case "platinum":
					return AssetTypeEnum.Platinum;
				case "palladium":
					return AssetTypeEnum.Palladium;
				case "crypto":
					return AssetTypeEnum.Crypto;
				default:
					throw new Exception("Unrecognized asset type " + assetType);
			}
		}

		private static TransactionTypeEnum GetTransactionType(string transactionType)
		{
			switch (transactionType.ToLower())
			{
				case "buy":
					return TransactionTypeEnum.Purchase;
				case "sell":
					return TransactionTypeEnum.Sale;
				case "storage_fee":
				case "storage fee":
					return TransactionTypeEnum.StorageFeeInCurrency;
				case "send":
					return TransactionTypeEnum.TransferOut;
				case "receive":
					return TransactionTypeEnum.TransferIn;
				default:
					throw new Exception("Transaction type " + transactionType + " not recognized");
			}
		}
	}
}

