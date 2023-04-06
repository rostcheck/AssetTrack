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
        Assert.AreEqual(transactions[0].Account, "test");
        Assert.AreEqual(transactions[0].AmountReceived, 4);
        Assert.AreEqual(transactions[0].Service, "BullionVault");
        Assert.AreEqual(transactions[0].TransactionID, "37055944");
        Assert.AreEqual(transactions[0].Vault, "Zurich");
    }

    //[TestMethod]
    //public void TestBlockFiParser()
    //{
    //    var parser = new BlockFiParser();
    //    var transactions = parser.Parse("../../../BlockFi-test-buytest.txt");
    //    Assert.AreEqual(1, transactions.Count);
    //}

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
}