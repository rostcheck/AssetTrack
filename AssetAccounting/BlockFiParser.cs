using System;
using System.Globalization;

namespace AssetAccounting
{
    // BlockFi has two file types available: the transaction report and the trading report.
    //
    // The transaction report contains transfers, bonuses, interest payments, etc and has a format like:
    //
    // Cryptocurrency	Amount	Transaction Type	Confirmed At    Value
    // USDC 0.22443969	Interest Payment    2022-10-31 23:59:59     
    // BTC,0.00111328,Cc Rewards Redemption,2021-10-08 18:03:03     28.71
    //
    // It is written backwards (for some reason) with latest transactions first.
    // It is also incomplete and must be manually augmented with the "Value" column from the statements for columns
    // that contain non-USD-stablecoin transactions.
    // We read only that file. Many of its transactions are bonus or interest payments or credit card rewards payments.
    //
    public class BlockFiParser : ParserBase, IFileParser
    {
        int recordCount = 0;

        public BlockFiParser() : base("BlockFi", 1, true)
        {
        }

        public override List<Transaction> ParseLines(IList<string> lines, string serviceName, string accountName)
        {
            // 0: Cryptocurrency	
            // 1: Amount	
            // 2: Transaction Type	
            // 3: Confirmed At
            // 4: Value (may be null)

            var transactions = new List<Transaction>();
            int lineNumber = lines.Count - 1;
            while (lineNumber >= 0)
            {
                var line = lines[lineNumber];
                string[] fields = line.Split(',');
                if (fields[2].Contains("BIA"))
                {
                    lineNumber--; // Filter these, they are internal movements between wallet and interest account
                    continue;
                }
                string itemType = fields[0];
                decimal assetAmount = Decimal.Parse(fields[1]);
                var transactionType = GetTransactionType(fields[2], assetAmount <= 0.0m);
                DateTime dateAndTime = DateTime.Parse(fields[3], CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                string vault = "BlockFi-" + accountName;
                string transactionId = string.Format("{0}-{1}", vault, recordCount++);
                var currencyUnit = CurrencyUnitEnum.USD; // only supports USD                
                AssetMeasurementUnitEnum measurementUnit = AssetMeasurementUnitEnum.CryptoCoin;// Only support crypto
                AssetTypeEnum assetType = AssetTypeEnum.Crypto; // only support crypto
                var inputTransactionType = fields[1].ToLower();
                decimal currencyAmount = 0.0m; // BlockFi doesn't process USD, only USDC


                if (transactionType == TransactionTypeEnum.Purchase)
                {
                    if (itemType.Contains("USD"))
                        currencyAmount = Math.Abs(assetAmount); // Value is 1-to-1 with USD
                    else
                    {
                        // Look back one line to find the USD stablecoin
                        if (lineNumber == 0)
                            throw new Exception("Could not find prior matching line for transaction: " + transactionId);
                        var nextLineFields = lines[lineNumber - 1].Split(',');
                        string nextItemType = nextLineFields[0];
                        decimal nextLineCurrencyAmount = Math.Abs(Decimal.Parse(nextLineFields[1]));
                        var nextLineTransactionType = GetTransactionType(nextLineFields[2], assetAmount >= 0.0m);
                        DateTime nextLineDateAndTime = DateTime.Parse(nextLineFields[3], CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                        if (nextLineTransactionType != TransactionTypeEnum.Sale || nextLineDateAndTime != dateAndTime)
                            throw new Exception("Could not find matching line for transaction: " + transactionId);
                        currencyAmount = nextLineCurrencyAmount;
                    }
                }
                else if (transactionType == TransactionTypeEnum.Sale)
                {
                    if (itemType.Contains("USD"))
                        currencyAmount = Math.Abs(assetAmount); // Value is 1-to-1 with USD
                    else
                    {
                        // Look ahead one line to find the USD stablecoin
                        if (lineNumber == lines.Count - 1)
                            throw new Exception("Could not find prior matching line for transaction: " + transactionId);
                        var priorLineFields = lines[lineNumber + 1].Split(',');
                        string priorItemType = priorLineFields[0];
                        decimal priorLineCurrencyAmount = Math.Abs(Decimal.Parse(priorLineFields[1]));
                        var priorLineTransactionType = GetTransactionType(priorLineFields[2], assetAmount >= 0.0m);
                        DateTime priorLineDateAndTime = DateTime.Parse(priorLineFields[3], CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                        if (priorLineTransactionType != TransactionTypeEnum.Purchase || priorLineDateAndTime != dateAndTime)
                            throw new Exception("Could not find matching line for transaction: " + transactionId);
                        currencyAmount = priorLineCurrencyAmount;
                    }
                }
                else if (transactionType == TransactionTypeEnum.IncomeInAsset)
                {
                    if (itemType.Contains("USD"))
                        currencyAmount = Math.Abs(assetAmount); // Value is 1-to-1 with USD
                    else
                        currencyAmount = Math.Abs(decimal.Parse(fields[4])); // Use asset value
                }

                assetAmount = Math.Abs(assetAmount);
                var a = Utils.GetAmounts(transactionType, assetAmount, currencyAmount);
                decimal? spotPrice = Utils.GetSpotPrice(currencyAmount, assetAmount);

                string memo = FormMemo(transactionType, a.amountPaid, a.amountReceived, itemType);
                transactions.Add(new Transaction(serviceName, accountName, dateAndTime,
                    transactionId, transactionType, vault, a.amountPaid, currencyUnit, a.amountReceived,
                    measurementUnit, assetType, memo, itemType, spotPrice));
                lineNumber--;
            }
            return transactions;
        }

        private static TransactionTypeEnum GetTransactionType(string transactionType, bool isSale)
        {
            switch (transactionType)
            {
                case "Trade":
                    if (isSale)
                        return TransactionTypeEnum.Sale;
                    else
                        return TransactionTypeEnum.Purchase;                    
                case "Interest Payment":
                case "Cc Rewards Redemption":
                case "Cc Trading Rebate":
                case "Bonus Payment":               
                    return TransactionTypeEnum.IncomeInAsset;
                case "Withdrawal Fee":
                    return TransactionTypeEnum.FeeInAsset;
                case "Withdrawal":
                case "BIA Withdraw":
                    return TransactionTypeEnum.TransferOut;
                case "Crypto Transfer":
                case "BIA Deposit":
                    return TransactionTypeEnum.TransferIn;
                default:
                    throw new Exception("Transaction type " + transactionType + " not recognized");
            }
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
                return string.Format("Income in asset: {0:0.000000} {1}", amountReceived, itemType);
            else if (transactionType == TransactionTypeEnum.FeeInAsset)
                return string.Format("Fee in asset: {0:0.000000} {1}", amountPaid, itemType);
            else
                throw new Exception("Unsupported transaction type: " + transactionType.ToString());

        }

        public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
            throw new NotImplementedException();
        }
    }

}