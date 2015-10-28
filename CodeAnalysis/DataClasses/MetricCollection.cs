using System;
using System.Text;

namespace CodeAnalysis.DataClasses
{
    public struct MetricCollection
    {
        public readonly string Solution;
        public int TotalNumberOfClasses { get; set; }
        public int TotalNumberOfMethods { get; set; }
        public int TotalNumberOfNamespaces { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int TotalLinesOfCode { get; set; }
        private const char Semicolon = ';';
        public MetricCollection(string solution)
        {
            Solution = solution;
            TotalNumberOfClasses = 0;
            TotalNumberOfMethods = 0;
            TotalNumberOfNamespaces = 0;
            CyclomaticComplexity = 0;
            TotalLinesOfCode = 0;
        }

        public string ToCSVString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Solution).Append(Semicolon);
            builder.Append(TotalNumberOfClasses).Append(Semicolon);
            builder.Append(TotalNumberOfMethods).Append(Semicolon);
            builder.Append(CyclomaticComplexity).Append(Semicolon);
            builder.Append(TotalNumberOfNamespaces).Append(Semicolon);
            builder.Append(TotalLinesOfCode).Append(Semicolon);
            builder.Append(Double.Parse(TotalNumberOfClasses.ToString()) / TotalNumberOfNamespaces).Append(Semicolon);
            builder.Append(Double.Parse(TotalNumberOfMethods.ToString()) / TotalNumberOfClasses).Append(Semicolon);
            builder.Append(Double.Parse(TotalLinesOfCode.ToString()) / TotalNumberOfMethods).Append(Semicolon);
            builder.Append(Double.Parse(CyclomaticComplexity.ToString()) / TotalLinesOfCode);
            return builder.ToString();
        }

        public bool IsEmpty()
        {
            return (TotalNumberOfNamespaces == 0 || TotalNumberOfClasses == 0
                || TotalLinesOfCode == 0);
        }
    }
}
