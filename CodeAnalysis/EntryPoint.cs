
using System.Diagnostics;
using System.Linq;

namespace CodeAnalysis
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            var directory = args[0];
            var fileCrawler = new FileCrawler();
            var solutionFilePaths = fileCrawler.GetSolutionsFromDirectory(directory);
            var filesToIgnore = (fileCrawler.GetIgnoredFiles(directory)).ToList();
            var csvWriter = new CSVWriter();
            var resultFile = directory + "/metrics.csv";
            var workspaceHandler = new WorkspaceHandler();
            var stopwatch = new Stopwatch();
            var analyzer = new SolutionAnalyzer();

            stopwatch.Start();
            var solutions = workspaceHandler.CreateSolutionsFromFilePath(solutionFilePaths).ToList();
            var solutionsWithoutTestFiles = workspaceHandler.RemoveTestFiles(solutions);
            var filteredSolutions = workspaceHandler.RemoveBlackListedDocuments(solutionsWithoutTestFiles, filesToIgnore);
            var result = analyzer.Analyze(filteredSolutions);
            stopwatch.Stop();

            foreach (var metric in result) 
            {
                csvWriter.WriteResultToFile(resultFile, metric);
                ConsolePrinter.PrintResult(metric);
            }
            ConsolePrinter.PrintStuff("Time taken: ", stopwatch.Elapsed);
        }
    }
}