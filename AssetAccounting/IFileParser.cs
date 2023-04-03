namespace AssetAccounting
{
	public interface IFileParser
	{
		List<Transaction> Parse(string fileName);
	}
}

