using System.Collections.Generic;
using System.IO;
using CodeAnalysis.DataClasses;

namespace CodeAnalysis.Output
{
    internal class CSVWriter
    {
        private const string MetricHeader = "Solution;NOC;NOM;CYCLO;NOP;LOC;NOC/NOP;NOM/NOC;LOC/NOM;CYCLO/LOC";
        private const string AnalysisHeader = "Recommendation;Codefragment;Line;File\n";

        public void WriteAnalysisResultToFile(string path, IEnumerable<OptimizationRecomendation> recommendations)
        {
            InitializeFile(path + "/analysis.csv", AnalysisHeader);
            WriteRecommendations(path + "/analysis.csv", recommendations);
        }

        private void InitializeFile(string path, string header)
        {
            if (File.Exists(path)) return;
            CreateFile(path);
            WriteLineToFile(path, header);
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
            if (metrics.IsEmpty())
                return;

            InitializeFile(path, MetricHeader);
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