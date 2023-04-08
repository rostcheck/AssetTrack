using System;

namespace AssetAccounting
{
    // Coinbase and Coinbase Pro provide separate report formats. This is Coinbase.
    //
    // The transaction report begins with 7 lines that should be ignored, then has this format:
    // Timestamp	Transaction Type	Asset	Quantity Transacted	Spot Price Currency	Spot Price at Transaction	Subtotal	Total (inclusive of fees and/or spread)	Fees and/or Spread	Notes
    // 2017-12-19T05:40:53Z Buy BTC	0.00509137	USD	19053.81	97.01	100	2.99	Bought 0.00509137 BTC for $100.00 USD

    public class CoinbaseParser : ParserBase, IFileParser
    {
        int recordCount = 0;
        public CoinbaseParser() : base("Coinbase", 8)
        {        
        }

        public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
            // 0: Timestamp	
            // 1: Transaction Type	
            // 2: Asset	
            // 3: Quantity Transacted	
            // 4: Spot Price Currency	
            // 5: Spot Price at Transaction	
            // 6: Subtotal	
            // 7: Total (inclusive of fees and/or spread)	
            // 8: Fees and/or Spread	
            // 9: Notes

            DateTime dateAndTime = DateTime.Parse(fields[0]);
            TransactionTypeEnum transactionType = GetTransactionType(fields[1]);
            string itemType = fields[2];
            decimal amount = Decimal.Parse(fields[3],System.Globalization.NumberStyles.Any);
            CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[4]);

            decimal currencyAmount = 0.0m;
            if (fields[7] == "")
            {
                decimal spotPriceAtTransaction = Decimal.Parse(fields[5]);
                currencyAmount = spotPriceAtTransaction * amount;
            }
            else {
                currencyAmount = Decimal.Parse(fields[7]);
            }
            string memo = fields[9].Replace("\"", "");

            string vault = "Coinbase-" + accountName;
            string transactionID = string.Format("{0}-{1}", vault, recordCount++);
            
            AssetMeasurementUnitEnum measurementUnit = AssetMeasurementUnitEnum.CryptoCoin;// Only support crypto
            AssetTypeEnum assetType = AssetTypeEnum.Crypto; // only support crypto

            decimal amountPaid = 0.0m, amountReceived = 0.0m;

            if (transactionType == TransactionTypeEnum.Purchase || transactionType == TransactionTypeEnum.IncomeInAsset)
            {
                amountPaid = currencyAmount;
                amountReceived = amount;
            }
            else if (transactionType == TransactionTypeEnum.Sale)
            {
                amountPaid = amount;
                amountReceived = currencyAmount;
            }
            else if (transactionType == TransactionTypeEnum.TransferIn)
                amountReceived = amount;
            else if (transactionType == TransactionTypeEnum.TransferOut)
                amountPaid = amount;
            else if (transactionType == TransactionTypeEnum.FeeInCurrency)
                amountPaid = Math.Abs(currencyAmount);           
            else if (transactionType == TransactionTypeEnum.IncomeInCurrency)
                amountReceived = Math.Abs(currencyAmount);
            else
                throw new Exception("Unknown transaction type " + transactionType);
            decimal? spotPrice = Utils.GetSpotPrice(currencyAmount, amount);
            if (spotPrice == null && itemType.Contains("USDC"))
                spotPrice = 1.0m; // Set USDC stablecoins to 1.0

            return new Transaction(serviceName, accountName, dateAndTime,
                transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived,
                measurementUnit, assetType, memo, itemType, spotPrice);
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

        private static TransactionTypeEnum GetTransactionType(string transactionType)
        {
            switch (transactionType.ToLower())
            {
                case "buy":
                case "advanced trade buy":
                    return TransactionTypeEnum.Purchase;
                case "sell":
                case "advanced trade sell":
                    return TransactionTypeEnum.Sale;
                case "rewards income":
                    return TransactionTypeEnum.IncomeInAsset;
                case "storage_fee":
                    return TransactionTypeEnum.FeeInCurrency;
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

