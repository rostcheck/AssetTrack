using System;
using System.Diagnostics;

namespace AssetAccounting
{
    // Coinbase and Coinbase Pro provide separate report formats. This is Coinbase Pro. It has this format:
    // portfolio,type,time,amount,balance,amount/balance unit, transfer id,trade id, order id
    // default,deposit,2022-01-02T20:34:30.503Z,2000.0000000000000000,3051.2761222022958500,USD,aad1b211-2560-472d-8ee2-e9b55ea7984a,,
    // default,match,2022-01-03T20:04:08.967Z,-245.3900000000000000,2805.8861222022958500,USD,,258594329,3609df77-468d-44a1-a047-5dbad155be45
    // default,match,2022-01-03T20:04:08.967Z,0.0053000000000000,0.3801516100000000,BTC,,258594329,3609df77-468d-44a1-a047-5dbad155be45
    // default,fee,2022-01-03T20:04:08.967Z,-1.2269500000000000,2804.6591722022958500,USD,,258594329,3609df77-468d-44a1-a047-5dbad155be45
    public class CoinbaseProParser : ParserBase, IFileParser
    {
        int recordCount = 0;

        public CoinbaseProParser() : base("Coinbase", 1, true)
        {
        }

        public override List<Transaction> ParseLines(IList<string> lines, string serviceName, string accountName)
        {
            // 0: portfolio,
            // 1: type,
            // 2: time,
            // 3: amount,
            // 4: balance,
            // 5: amount/balance unit,
            // 6: transfer id,
            // 7: trade id,
            // 8: order id

            var transactions = new List<Transaction>();
            int lineNumber = 0;
            while (lineNumber < lines.Count)
            {
                var line = lines[lineNumber];
                string[] fields = line.Split(',');
                string currentTradeId = fields[7];
                string thisAssetType = fields[5];
                DateTime dateAndTime = DateTime.Parse(fields[2]);
                string transactionId = fields[7];
                string vault = "CoinbasePro-" + accountName;
                if (transactionId == "")
                    transactionId = string.Format("{0}-{1}", vault, recordCount++);

                decimal currencyAmount = 0.0m;
                decimal assetAmount = 0.0m;
                decimal thisLineAmount = Decimal.Parse(fields[3]);
                string thisLineItemType = fields[5];
                var currencyUnit = CurrencyUnitEnum.USD; // only supports USD                
                string itemType = thisLineItemType;
                AssetMeasurementUnitEnum measurementUnit = AssetMeasurementUnitEnum.CryptoCoin;// Only support crypto
                AssetTypeEnum assetType = AssetTypeEnum.Crypto; // only support crypto

                // Possible input transaction types are: deposit, fee, match, withdrawal
                var inputTransactionType = fields[1].ToLower();
                var transactionType = TransactionTypeEnum.Indeterminate;

                if (inputTransactionType == "deposit")
                {
                    // ignore currency deposits
                    if (thisAssetType == "USD")
                    {
                        lineNumber++;
                        continue;
                    }
                    else
                        transactionType = TransactionTypeEnum.TransferIn;
                }
                else if (inputTransactionType == "withdrawal")
                {
                    thisLineAmount = Math.Abs(thisLineAmount); // CoinbasePro records withdrawals as negatives
                    // ignore currency withdrawal
                    if (thisAssetType == "USD")
                    {
                        lineNumber++;
                        continue;
                    }
                    else
                        transactionType = TransactionTypeEnum.TransferOut;
                }
                else if (inputTransactionType == "match")
                {
                    // Assemble a transaction from multiple lines
                    lineNumber++;
                    var nextLineFields = lines[lineNumber].Split(',');
                    if (nextLineFields[7] != transactionId || nextLineFields[1] != "match")
                        throw new Exception("Could not find matching line for transaction: " + transactionId);
                    var nextLineAmount = Decimal.Parse(nextLineFields[3]);
                    var nextLineAssetType = nextLineFields[5];
                    if (thisAssetType == "USD")
                    {
                        currencyAmount = thisLineAmount;
                        assetAmount = nextLineAmount;
                        itemType = nextLineAssetType;
                        if (thisLineAmount < 0.0m)
                            transactionType = TransactionTypeEnum.Purchase;
                        else
                            transactionType = TransactionTypeEnum.Sale;
                    }
                    else
                    {
                        assetAmount = thisLineAmount;
                        currencyAmount = nextLineAmount;                       
                    }
                    // Fee follows both match lines
                    lineNumber++;
                    var plus2LineFields = lines[lineNumber].Split(',');
                    if (plus2LineFields[7] != transactionId || plus2LineFields[1] != "fee")
                        throw new Exception("Could not find matching fee for transaction: " + transactionId);
                    if (plus2LineFields[5] != "USD")
                        throw new Exception("Fee expressed in non-USD currency " + plus2LineFields[5] + " for transaction: " + transactionId);
                    // Add the fee to the currency amount (increase the basis)
                    currencyAmount += Decimal.Parse(plus2LineFields[3]);

                }
                else throw new Exception("Unrecognized transaction type: " + inputTransactionType);

                decimal amountPaid = 0.0m, amountReceived = 0.0m;
                if (transactionType == TransactionTypeEnum.Purchase )
                {
                    amountPaid = Math.Abs(currencyAmount);
                    amountReceived = Math.Abs(assetAmount);
                }
                else if (transactionType == TransactionTypeEnum.Sale)
                {
                    amountPaid = Math.Abs(assetAmount);
                    amountReceived = Math.Abs(currencyAmount);
                }
                else if (transactionType == TransactionTypeEnum.TransferIn)
                    amountReceived = thisLineAmount;
                else if (transactionType == TransactionTypeEnum.TransferOut)
                    amountPaid = thisLineAmount;
                else 
                    throw new Exception("Unknown transaction type " + transactionType);
                decimal? spotPrice = Utils.GetSpotPrice(currencyAmount, assetAmount);

                string memo = FormMemo(transactionType, amountPaid, amountReceived, itemType);
                transactions.Add(new Transaction(serviceName, accountName, dateAndTime,
                    transactionId, transactionType, vault, amountPaid, currencyUnit, amountReceived,
                    measurementUnit, assetType, memo, itemType, spotPrice));
                lineNumber++;
            }
            return transactions;
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
            else
                throw new Exception("Unsupported transaction type: " + transactionType.ToString());

        }

        public override Transaction ParseFields(IList<string> fields, string serviceName, string accountName)
        {
            throw new NotImplementedException();
        }
    }
}

