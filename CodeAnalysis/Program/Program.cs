using System.Diagnostics;
using System.Linq;
using CodeAnalysis.Domain;
using CodeAnalysis.Output;

namespace CodeAnalysis.Program
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var directory = args[0];
            var fileCrawler = new FileCrawler();
            var solutionFilePaths = fileCrawler.GetSolutionsFromDirectory(directory).ToList();
            var filesToIgnore = (fileCrawler.GetIgnoredFiles(directory)).ToList();
            var csvWriter = new CSVWriter();
            var metricResultFile = directory + "/metrics.csv";
            var analysisResultFile = directory + "/analysis.csv";
            var workspaceHandler = new WorkspaceHandler();
            var stopwatch = new Stopwatch();
            var metricCalculator = new MetricCalculator();
            var nameInspector = new NameInspector();
            var functionInspector = new FunctionInspector();
            var commentInspector = new CommentInspector();
            var lawOfDemeterValidator = new LawOfDemeterValidator();

            stopwatch.Start();

            foreach (var solution in solutionFilePaths)
            {
                var vsSolution = workspaceHandler.CreateSolutionsFromFilePath(solution);
                if (vsSolution == null) continue;
                if (workspaceHandler.IsTestSolutions(vsSolution)) continue;
                
                var solutionWithoutTestFiles = workspaceHandler.RemoveTestFiles(vsSolution);
                var filteredSolution = workspaceHandler.RemoveBlackListedDocuments(solutionWithoutTestFiles, filesToIgnore);

                //var resultMetric = metricCalculator.AnalyzeSolution(filteredSolution);
                var nameRecommendations = nameInspector.Analyze(filteredSolution).ToList();
                var functionRecommendations = functionInspector.Analyze(filteredSolution).ToList();
                var commentRecommendations = commentInspector.Analyze(filteredSolution).ToList();
                var demeterViolations = lawOfDemeterValidator.Analyze(filteredSolution).ToList();

               // csvWriter.WriteResultMetricsToFile(metricResultFile, resultMetric);
               csvWriter.WriteAnalysisResultToFile(analysisResultFile, nameRecommendations);
               csvWriter.WriteAnalysisResultToFile(analysisResultFile, functionRecommendations);
               csvWriter.WriteAnalysisResultToFile(analysisResultFile, commentRecommendations);
               csvWriter.WriteAnalysisResultToFile(analysisResultFile, demeterViolations);

                // ConsolePrinter.PrintMetrics(resultMetric);
                // ConsolePrinter.PrintRecomendations(nameRecommendations);
                // ConsolePrinter.PrintRecomendations(functionRecommendations);
            } 

            stopwatch.Stop();
            ConsolePrinter.PrintStuff("Time taken: ", stopwatch.Elapsed);
        }        
    }
}