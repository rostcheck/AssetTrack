using System.Text.RegularExpressions;
using Csv;

namespace AssetAccounting
{
	public abstract class ParserBase : IFileParser
	{
		private string serviceName;
		private int headerLines;
		private bool isLedgerFormat;

		public ParserBase(string serviceName, int headerLines = 1, bool ledgerFormat = false)
		{
			if (serviceName == null || serviceName == string.Empty)
				throw new Exception("Cannot initialize ParserBase without a service name");

			this.serviceName = serviceName;
			this.headerLines = headerLines;
			this.isLedgerFormat = ledgerFormat;
		}

		public virtual List<Transaction> Parse(string fileName)
		{
			if (this.isLedgerFormat)
			{
                string serviceName = ParseServiceNameFromFilename(fileName);
                string accountName = ParseAccountNameFromFilename(fileName, serviceName);
                var lines = ReadLines(fileName);
				return this.ParseLines(lines, serviceName, accountName);
			}
			else
			{
				if (fileName.ToLower().EndsWith(".txt"))
					return ParseTxt(fileName);
				else if (fileName.ToLower().EndsWith(".csv"))
					return ParseCsv(fileName);
				else
					throw new FileLoadException("Unrecognized filename extension");
			}
		}

		public abstract Transaction ParseFields(IList<string> fields, string serviceName, string accountName);

        public virtual List<Transaction> ParseLines(IList<string> lines, string serviceName, string accountName)
		{
            // Only implemented for ledger-style parsers that may need to look at multiple lines to form a transaction
            throw new NotImplementedException(); 
        }

        // Returns all lines for further parsing. Needed for ledger-style ("transaction-format" files, where a
        // transaction can span two lines, the debug and credit sides separately
        public List<string> ReadLines(string fileName)
        {
            string serviceName = ParseServiceNameFromFilename(fileName);
            string accountName = ParseAccountNameFromFilename(fileName, serviceName);

            var lines = new List<string>();
            StreamReader reader = new StreamReader(fileName);
            string? line = reader.ReadLine();
            int lineCount = 0;

            while (lineCount++ < this.headerLines)
            {
                line = reader.ReadLine();
                continue;
            }
            while (line != null && line != string.Empty)
            {
                lines.Add(line);
                line = reader.ReadLine();
            }

            return lines;
        }

        private List<Transaction> ParseTxt(string fileName)
		{
			string serviceName = ParseServiceNameFromFilename(fileName);
			string accountName = ParseAccountNameFromFilename(fileName, serviceName);

			List<Transaction> transactionList = new List<Transaction>();
			StreamReader reader = new StreamReader(fileName);
			string? line = reader.ReadLine();
			int lineCount = 0;
            
            while (lineCount++ < this.headerLines)
            {
                line = reader.ReadLine();
                continue;
            }
            while (line != null && line != string.Empty)
			{
				string[] fields = line.Split('\t');
				if (fields.Length < 2)
				{
					fields = line.Split(','); // Could be CSV
				}
				for (var i = 0; i < fields.Length; i++)
				{
					if (fields[i] == "\"\"")
						fields[i] = "";
				}
				if (string.Join("", fields) == string.Empty || line.Contains("Number of transactions ="))
				{
					line = reader.ReadLine();
					continue;
				}

				transactionList.Add(this.ParseFields(fields, serviceName, accountName));
				line = reader.ReadLine();
			}

			return transactionList;
		}

		private List<Transaction> ParseCsv(string fileName)
		{
			string serviceName = ParseServiceNameFromFilename(fileName);
			string accountName = ParseAccountNameFromFilename(fileName, serviceName);
			List<Transaction> transactionList = new List<Transaction>();
			var csv = File.ReadAllText(fileName);
			var options = new CsvOptions();
			options.AllowSingleQuoteToEncloseFieldValues = true;
			options.RowsToSkip = this.headerLines;
			options.HeaderMode = HeaderMode.HeaderAbsent; // handle via RowsToSkip
			foreach (var readFields in CsvReader.ReadFromText(csv, options))
			{
				List<string> fields = new List<string>(readFields.ColumnCount);
				for (int i = 0; i < readFields.ColumnCount; i++)
					fields.Add(readFields[i]);
				transactionList.Add(ParseFields(fields, serviceName, accountName));
			}
			return transactionList;
		}

		protected string ParseAccountNameFromFilename(string fileName, string? thisServiceName = null)
		{
			var trimmedFileName = Path.GetFileName(fileName);
			if (thisServiceName == null)
				thisServiceName = serviceName;
			Regex r = new Regex(string.Format(@"^{0}-(?<account>\w+)-", thisServiceName));
			Match m = r.Match(trimmedFileName);
			if (m.Success)
				return m.Groups["account"].Value;
			else
				throw new Exception("Cannot parse account name from filename " + fileName);
		}

		protected string ParseServiceNameFromFilename(string fileName)
		{
			var parts = Path.GetFileName(fileName).Split('-');
			if (serviceName.ToLower().Contains("generic"))
				serviceName = parts[0];
			return parts[0];
		}

		protected void VerifyFilename(string fileName)
		{
			if (serviceName.ToLower().Contains("generic"))
				return; // Generic parser will accept anything
			
			if (!fileName.Contains(serviceName))
				throw new Exception(string.Format("Filename {0} should contain '{1}' to be parsed by {2}Parser",
					fileName, serviceName, serviceName));
		}
    }
}

