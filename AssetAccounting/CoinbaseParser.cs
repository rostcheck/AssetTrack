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
            decimal amount = Convert.ToDecimal(fields[3]);
            CurrencyUnitEnum currencyUnit = GetCurrencyUnit(fields[4]);
            decimal currencyAmount = Convert.ToDecimal(fields[7]);
            string memo = fields[9] + " Spot: " + fields[5];

            const string vault = "";
            const string transactionID = "";
            
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
            else if (transactionType == TransactionTypeEnum.StorageFeeInCurrency)
                amountPaid = Math.Abs(currencyAmount);
            else if (transactionType != TransactionTypeEnum.IncomeInCurrency) // ignore interest
                throw new Exception("Unknown transaction type " + transactionType);

            return new Transaction(serviceName, accountName, dateAndTime,
                transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived,
                measurementUnit, assetType, memo, itemType);
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
                case "advanced trade buy":
                    return TransactionTypeEnum.Purchase;
                case "sell":
                case "advanced trade sell":
                    return TransactionTypeEnum.Sale;
                case "rewards income":
                    return TransactionTypeEnum.IncomeInCurrency;
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

