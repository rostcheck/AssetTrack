namespace AssetAccounting
{
	public class BullionVaultParser : ParserBase, IFileParser
	{
		public BullionVaultParser() : base("BullionVault")
		{
		}

		public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
		{
			DateTime dateAndTime = DateTime.Parse(fields[0]);
			string transactionID = fields[1];
			string transactionTypeString = fields[2];
			TransactionTypeEnum transactionType = GetTransactionType(transactionTypeString);
			string vault = fields[3];
			decimal value = Decimal.Parse(fields[4]);
			decimal weight = Decimal.Parse(fields[14]);
			decimal commission = Decimal.Parse(fields[15]);
			decimal consideration = Decimal.Parse(fields[16]);
			decimal totalCompensation = commission + consideration;
			decimal amountPaid = 0.0m, amountReceived = 0.0m;

			AssetTypeEnum metalType = GetMetalType (fields [7]);
			weight *= 1000; // Bullionvault counts metal in kg, convert to grams
				
			if (transactionType == TransactionTypeEnum.Purchase)
			{
				amountPaid = totalCompensation;
				amountReceived = weight;
			}
			else if (transactionType == TransactionTypeEnum.Sale)
			{
				amountPaid = weight;
				amountReceived = totalCompensation;
			}
			else if (transactionType == TransactionTypeEnum.FeeInCurrency)
				amountPaid = Math.Abs(value);
			else
				throw new Exception("Unknown transaction type " + transactionType);	
			CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[6]);
			decimal? spotPrice = Utils.GetSpotPrice(totalCompensation, weight); 

			return new Transaction("BullionVault", accountName, dateAndTime, 
				transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived, 
				AssetMeasurementUnitEnum.Gram, metalType, "", metalType.ToString(), spotPrice);
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

		private static AssetTypeEnum GetMetalType(string metalType)
		{
			switch (metalType.ToLower())
			{
				case "gold":
					return AssetTypeEnum.Gold;
				case "silver":
					return AssetTypeEnum.Silver;
				case "platinum":
					return AssetTypeEnum.Platinum;
				case "palladium":
					return AssetTypeEnum.Palladium;
				default:
					throw new Exception("Unrecognized metal type " + metalType);
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
					return TransactionTypeEnum.FeeInCurrency;
				default:
					throw new Exception("Transaction type " + transactionType + " not recognized");
			}
		}
	}
}

