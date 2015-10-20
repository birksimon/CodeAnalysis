using System;
using System.IO;
using System.Text;

namespace CodeAnalysis
{
    class CSVWriter
    {
        private const string Header = @"Solution;NOC;NOM;CYCLO;NOP;LOC;NOC/NOP;NOM/NOC;LOC/NOM;CYCLO/LOC";
        private const char Semicolon = ';';
        public void WriteResultToFile(string path, MetricCollection metrics)
        {
            if (metrics.TotalNumberOfNamespaces == 0 || metrics.TotalNumberOfClasses == 0 
                || metrics.TotalLinesOfCode == 0)
                return;

            if (!File.Exists(path))
            {
                CreateFile(path);
                WriteLineToFile(path, Header);
            }
            var resultString = CreateResultCSVString(metrics);
            WriteLineToFile(path, resultString);
        }

        public void CreateFile(string path)
        {
            File.Create(path).Dispose();
        }

        private void WriteLineToFile(string path, string line)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }

        private string CreateResultCSVString(MetricCollection metrics)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(metrics.Solution).Append(Semicolon);
            builder.Append(metrics.TotalNumberOfClasses).Append(Semicolon);
            builder.Append(metrics.TotalNumberOfMethods).Append(Semicolon);
            builder.Append(metrics.CyclomaticComplexity).Append(Semicolon);
            builder.Append(metrics.TotalNumberOfNamespaces).Append(Semicolon);
            builder.Append(metrics.TotalLinesOfCode).Append(Semicolon);
            builder.Append(Double.Parse(metrics.TotalNumberOfClasses.ToString()) / metrics.TotalNumberOfNamespaces).Append(Semicolon);
            builder.Append(Double.Parse(metrics.TotalNumberOfMethods.ToString()) / metrics.TotalNumberOfClasses).Append(Semicolon);
            builder.Append(Double.Parse(metrics.TotalLinesOfCode.ToString()) / metrics.TotalNumberOfMethods).Append(Semicolon);
            builder.Append(Double.Parse(metrics.CyclomaticComplexity.ToString()) / metrics.TotalLinesOfCode);

            return builder.ToString();
        }
    }
}
