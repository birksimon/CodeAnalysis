using System;
using System.Text;
using CodeAnalysis.Output;

namespace CodeAnalysis.DataClasses
{
    public class MetricCollection : ICSVPrintable
    {
        public readonly string Solution;
        public int TotalNumberOfClasses { get; set; }
        public int TotalNumberOfMethods { get; set; }
        public int TotalNumberOfNamespaces { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int TotalLinesOfCode { get; set; }
        public MetricCollection(string solution)
        {
            Solution = solution;
            TotalNumberOfClasses = 0;
            TotalNumberOfMethods = 0;
            TotalNumberOfNamespaces = 0;
            CyclomaticComplexity = 0;
            TotalLinesOfCode = 0;
        }
        public string GetCSVString()
        {
            var builder = new StringBuilder();
            builder.Append(Solution).Append(PrintConstants.Semicolon);
            builder.Append(TotalNumberOfClasses).Append(PrintConstants.Semicolon);
            builder.Append(TotalNumberOfMethods).Append(PrintConstants.Semicolon);
            builder.Append(CyclomaticComplexity).Append(PrintConstants.Semicolon);
            builder.Append(TotalNumberOfNamespaces).Append(PrintConstants.Semicolon);
            builder.Append(TotalLinesOfCode).Append(PrintConstants.Semicolon);
            builder.Append(Double.Parse(TotalNumberOfClasses.ToString()) / TotalNumberOfNamespaces).Append(PrintConstants.Semicolon);
            builder.Append(Double.Parse(TotalNumberOfMethods.ToString()) / TotalNumberOfClasses).Append(PrintConstants.Semicolon);
            builder.Append(Double.Parse(TotalLinesOfCode.ToString()) / TotalNumberOfMethods).Append(PrintConstants.Semicolon);
            builder.Append(Double.Parse(CyclomaticComplexity.ToString()) / TotalLinesOfCode);
            return builder.ToString();
        }
        public string GetCSVHeader()
        {
            return "Solution;NOC;NOM;CYCLO;NOP;LOC;NOC/NOP;NOM/NOC;LOC/NOM;CYCLO/LOC\n";
        }
        public bool IsEmpty()
        {
            return (TotalNumberOfNamespaces == 0 || TotalNumberOfClasses == 0
                || TotalLinesOfCode == 0);
        }
        public string GetFileName()
        {
            return "/MetricCollection.csv";
        }
    }
}
