using AssetAccounting;

namespace AssetAccountingTests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    public void TestBullionVaultParser()
    {
        var bv = new BullionVaultParser();
        var transactions = bv.Parse("../../../BullionVault-test-buytest.txt");
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
    //    var bv = new BlockFiParser();
    //    var transactions = bv.Parse("../../../BlockFi-test-buytest.txt");
    //    Assert.AreEqual(1, transactions.Count);
    //}

    [TestMethod]
    public void TestCoinbaseParser()
    {
        var bv = new CoinbaseParser();
        var transactions = bv.Parse("../../../Coinbase-test-buytest.txt");
        Assert.AreEqual(1, transactions.Count);
        Assert.AreEqual(transactions[0].Account, "test");
        Assert.AreEqual(transactions[0].AmountPaid, 100m);
        Assert.AreEqual(transactions[0].AmountReceived, 0.00509137m);
        Assert.AreEqual(transactions[0].Service, "Coinbase");
        Assert.AreEqual(transactions[0].Memo, "Bought 0.00509137 BTC for $100.00 USD Spot: 19053.81");

    }
}
