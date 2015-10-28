using System.Collections.Generic;
using System.IO;
using CodeAnalysis.DataClasses;

namespace CodeAnalysis.Output
{
    internal class CSVWriter
    {
        private const string MetricHeader = @"Solution;NOC;NOM;CYCLO;NOP;LOC;NOC/NOP;NOM/NOC;LOC/NOM;CYCLO/LOC";
        private const string AnalysisHeader = @"Recommendation;Codefragment;Line;File";

        public void WriteAnalysisResultToFile(string path, IEnumerable<OptimizationRecomendation> recommendations)
        {
            InitializeFile(path);
            WriteRecommendations(path, recommendations);
        }

        private void InitializeFile(string path)
        {
            if (File.Exists(path)) return;
            CreateFile(path);
            WriteLineToFile(path, AnalysisHeader);
        }

        private void WriteRecommendations(string path, IEnumerable<OptimizationRecomendation> recommendations)
        {
            foreach (var recommendation in recommendations)
            {
                if (recommendation.HasOccurences())
                    WriteLineToFile(path, recommendation.ToCSVString());
            }
        }

        public void WriteResultMetricsToFile(string path, MetricCollection metrics)
        {
            if (metrics.TotalNumberOfNamespaces == 0 || metrics.TotalNumberOfClasses == 0
                || metrics.TotalLinesOfCode == 0)
                return;

            if (!File.Exists(path))
            {
                CreateFile(path);
                WriteLineToFile(path, MetricHeader);
            }
            WriteLineToFile(path, metrics.ToCSVString());
        }

        private void CreateFile(string path)
        {
            File.Create(path).Dispose();
        }

        private void WriteLineToFile(string path, string line)
        {
            File.AppendAllText(path, line);
        }
    }
}