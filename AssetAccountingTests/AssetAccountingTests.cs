using AssetAccounting;

namespace AssetAccountingTests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    public void TestBullionVaultParser()
    {
        var parser = new BullionVaultParser();
        var transactions = parser.Parse("../../../BullionVault-test-buytest.txt");
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual("test", transactions[0].Account);
        Assert.AreEqual(4, transactions[0].AmountReceived);
        Assert.AreEqual("BullionVault", transactions[0].Service);
        Assert.AreEqual("37055944", transactions[0].TransactionID);
        Assert.AreEqual("Zurich", transactions[0].Vault);
    }

    [TestMethod]
    public void TestBlockFiParser()
    {
        var parser = new BlockFiParser();
        var transactions = parser.Parse("../../../BlockFi-test-buytest.txt");
        Assert.AreEqual(10, transactions.Count);
        Assert.AreEqual("USDC", transactions[0].ItemType);
        Assert.AreEqual(10000.0m, transactions[0].AmountReceived);
        Assert.AreEqual(TransactionTypeEnum.TransferIn, transactions[0].TransactionType);
        Assert.AreEqual("BlockFi", transactions[0].Service);
        Assert.AreEqual("Transferred in 10000.000000 USDC", transactions[0].Memo);
    }

    [TestMethod]
    public void TestCoinbaseParser()
    {
        var parser = new CoinbaseParser();
        var transactions = parser.Parse("../../../Coinbase-test-buytest.txt");
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual("test", transactions[0].Account);
        Assert.AreEqual(100m, transactions[0].AmountPaid);
        Assert.AreEqual(0.00509137m, transactions[0].AmountReceived);
        Assert.AreEqual("Coinbase", transactions[0].Service);
        Assert.AreEqual("Bought 0.00509137 BTC for $100.00 USD", transactions[0].Memo);
    }


    [TestMethod]
    public void TestCoinbaseProParser()
    {
        var parser = new CoinbaseProParser();
        var transactions = parser.Parse("../../../CoinbasePro-test-buytest.txt");
        Assert.AreEqual(2, transactions.Count);
        Assert.AreEqual("test", transactions[0].Account);
        Assert.AreEqual(246.61695m, transactions[0].AmountPaid);
        Assert.AreEqual(0.0053m, transactions[0].AmountReceived);
        Assert.AreEqual("CoinbasePro", transactions[0].Service);
        Assert.AreEqual("Bought 0.005300 BTC for 246.62 USD", transactions[0].Memo);
    }

    [TestMethod]
    public void TestCelsiusParser()
    {
        var parser = new CelsiusParser();
        var transactions = parser.Parse("../../../Celsius-test-buytest.csv");
        Assert.AreEqual(2, transactions.Count);
        Assert.AreEqual("test", transactions[0].Account);
        Assert.AreEqual(9.545224512m, transactions[0].AmountPaid);
        Assert.AreEqual(0.000471004m, transactions[0].AmountReceived);
        Assert.AreEqual("Celsius", transactions[0].Service);
        Assert.AreEqual("BTC", transactions[0].ItemType);
        Assert.AreEqual(TransactionTypeEnum.IncomeInAsset, transactions[0].TransactionType);
        Assert.AreEqual("Reward, receive 0.000471 BTC valued at 9.55 USD", transactions[0].Memo);
        Assert.AreEqual("test", transactions[1].Account);
        Assert.AreEqual(0.0m, transactions[1].AmountPaid);
        Assert.AreEqual(0.27m, transactions[1].AmountReceived);
        Assert.AreEqual("Celsius", transactions[1].Service);
        Assert.AreEqual("ETH", transactions[1].ItemType);
        Assert.AreEqual(TransactionTypeEnum.TransferIn, transactions[1].TransactionType);
        Assert.AreEqual("Transferred in 0.270000 ETH", transactions[1].Memo);
    }
}