using System;
using AssetAccounting;

namespace AssetTrack
{
    public class ConsoleLogWriter : ILogWriter
    {
        public ConsoleLogWriter()
        {
        }

        public void WriteEntry(string s)
        {
            Console.WriteLine(s);
        }
    }
}
