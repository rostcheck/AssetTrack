using System;

namespace AssetAccounting
{
    // BlockFi has two file types available: the transaction report and the trading report.
    //
    // The transaction report contains transfers, bonuses, interest payments, etc and has a format like:
    //
    // Cryptocurrency	Amount	Transaction Type	Confirmed At
    // USDC 0.22443969	Interest Payment    2022-10-31 23:59:59
    //
    // Many of its transactions are bonus or interest payments, which are sepacredit card rewards payments that 
    // transactions are in USD or an equivalent (USDC,
    //
    // The trading report has a format like:
    // Trade ID	Date	Buy Quantity	Buy Currency	Sold Quantity	Sold Currency	Rate Amount	Rate Currency	Type	Frequency	Destination
    // 95327825-6976-480f-ab46-00b3daa679a7	2022-07-06 03:46:50	51.61135821	usdc	51.61135821	gusd	1	usdc Trade   One Time    Wallet
    // and contains both sides of a trade, including conversion between cryptocurrencies that have identical values
    // (ex. USDC and GUSD are both stablecoins tied to the US dollar)
    //
    // NOTE: incomplete


    public class BlockFiParser : ParserBase, IFileParser
    {
        public BlockFiParser() : base("BlockFi")
        {
        }

        public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
            string itemType = fields[0];
            decimal currencyAmount = Decimal.Parse(fields[1]);
            TransactionTypeEnum transactionType = GetTransactionType(fields[2]);
            DateTime dateAndTime = DateTime.Parse(fields[3]);

            const string vault = "";
            const string transactionID = "";

            CurrencyUnitEnum currencyUnit = CurrencyUnitEnum.USD; //GetCurrencyUnit(fields[5]);
            decimal weight = Decimal.Parse(fields[6]);
            AssetMeasurementUnitEnum weightUnit = GetWeightUnit(fields[7]);

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
            else if (transactionType == TransactionTypeEnum.FeeInCurrency)
                amountPaid = Math.Abs(currencyAmount);
            else
                throw new Exception("Unknown transaction type " + transactionType);

            decimal? spotPrice = Utils.GetSpotPrice(currencyAmount, weight);
            AssetTypeEnum assetType = GetAssetType(fields[8]);

            return new Transaction(serviceName, accountName, dateAndTime,
                transactionID, transactionType, vault, amountPaid, currencyUnit, amountReceived,
                weightUnit, assetType, "", itemType, spotPrice);
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

