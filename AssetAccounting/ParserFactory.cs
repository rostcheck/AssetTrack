using System;
namespace AssetAccounting
{
	public static class ParserFactory
	{
		public static IFileParser GetParser(string filename)
		{
            if (filename.Contains("GoldMoney"))
                return new GoldMoneyParser();
            else if (filename.Contains("BullionVault"))
                return new BullionVaultParser();
            else if (filename.Contains("CoinbasePro"))
                return new CoinbaseProParser();
            else if (filename.Contains("Coinbase"))
                return new CoinbaseParser();
            else if (filename.Contains("Celsius"))
                return new CelsiusParser();
            else if (filename.Contains("BlockFi"))
                return new BlockFiParser();
            else
                return new GenericCsvParser();
        }
	}
}

