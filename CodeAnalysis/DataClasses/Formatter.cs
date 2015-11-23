using System;

namespace CodeAnalysis.DataClasses
{
    class Formatter
    {
        public static string RemoveNewLines(string str)
        {
            return @"" + str.Replace(Environment.NewLine, "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\r\n", "")
                .Replace("\n\r", "")
                .Replace(";", " ")
                   + "";
        }
    }
}
