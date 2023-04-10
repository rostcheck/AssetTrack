using System;
namespace AssetAccounting
{
    // Celsius files have this format:
    // Internal id, Date and time, Transaction type, Coin type, Coin amount, USD Value, Original Reward Coin, Reward Amount In Original Coin, Confirmed
    // da097cf7-44b3-4c4a-8465-dcf8b75267d1,7/14/22 0:00,Reward,BTC,0.000471004,9.545224512,BTC,,Yes
    // a5305ea7-293a-464d-a3dc-68d7927d7427,4/14/22 20:18,Transfer,ETH,0.27,807.5457289,,,Yes
    public class CelsiusParser : ParserBase, IFileParser
    {

		public CelsiusParser() : base("Celsius", 1)
		{
		}

        public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
            // 0: Internal id
            // 1: Date and time
            // 2: Transaction type
            // 3: Coin type
            // 4: Coin amount
            // 5: USD Value
            // 6: Original Reward Coin
            // 7: Reward Amount In Original Coin
            // 8: Confirmed

            string transactionID = fields[0];
            //var offset = new DateTimeOffset(
            DateTime dt = DateTime.Parse(fields[1]);
            DateTime dateAndTime = DateTime.SpecifyKind(dt, DateTimeKind.Utc); //TimeZoneInfo.ConvertTimeToUtc(
                
            //, DateTimeOffset)
            //        TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            string itemType = fields[3];
            decimal assetAmount = Decimal.Parse(fields[4], System.Globalization.NumberStyles.Any);
            TransactionTypeEnum transactionType = GetTransactionType(fields[2], assetAmount >= 0.0m);
            CurrencyUnitEnum currencyUnit = CurrencyUnitEnum.USD;
            decimal currencyAmount = Decimal.Parse(fields[5], System.Globalization.NumberStyles.Any);
            decimal spotPriceAtTransaction = currencyAmount / assetAmount;

            string vault = "Celsius-" + accountName;           

            AssetMeasurementUnitEnum measurementUnit = AssetMeasurementUnitEnum.CryptoCoin;// Only support crypto
            AssetTypeEnum assetType = AssetTypeEnum.Crypto; // only support crypto

            var a = Utils.GetAmounts(transactionType, assetAmount, currencyAmount);

            decimal? spotPrice = Utils.GetSpotPrice(currencyAmount, assetAmount);
            if (spotPrice == null && itemType.Contains("USDC"))
                spotPrice = 1.0m; // Set USDC stablecoins to 1.0
            string memo = FormMemo(transactionType, a.amountPaid, a.amountReceived, itemType);

            return new Transaction(serviceName, accountName, dateAndTime,
                transactionID, transactionType, vault, a.amountPaid, currencyUnit, a.amountReceived,
                measurementUnit, assetType, memo, itemType, spotPrice);
        }

        private static string FormMemo(TransactionTypeEnum transactionType, decimal amountPaid, decimal amountReceived,
            string itemType)
        {
            if (transactionType == TransactionTypeEnum.Sale)
                return string.Format("Sold {0:0.000000} {1} for {2:0.00} USD", amountPaid, itemType, amountReceived);
            else if (transactionType == TransactionTypeEnum.Purchase)
                return string.Format("Bought {0:0.000000} {1} for {2:0.00} USD", amountReceived, itemType, amountPaid);
            else if (transactionType == TransactionTypeEnum.TransferIn)
                return string.Format("Transferred in {0:0.000000} {1}", amountReceived, itemType);
            else if (transactionType == TransactionTypeEnum.TransferOut)
                return string.Format("Transferred out {0:0.000000} {1}", amountPaid, itemType);
            else if (transactionType == TransactionTypeEnum.IncomeInAsset)
                return string.Format("Reward, receive {0:0.000000} {1} valued at {2:0.00} USD", amountReceived, itemType, amountPaid);
            else
                throw new Exception("Unsupported transaction type: " + transactionType.ToString());

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

        private static TransactionTypeEnum GetTransactionType(string transactionType, bool directionIsIn)
        {
            switch (transactionType.ToLower())
            {
                case "buy":
                    return TransactionTypeEnum.Purchase;
                case "sell":
                    return TransactionTypeEnum.Sale;
                case "reward":
                    return TransactionTypeEnum.IncomeInAsset;
                case "transfer":
                    if (directionIsIn)
                        return TransactionTypeEnum.TransferIn;
                    else
                        return TransactionTypeEnum.TransferOut;
                default:
                    throw new Exception("Transaction type " + transactionType + " not recognized");
            }
        }
    }
}

