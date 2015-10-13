using System;
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
                PrintResult(metric);
            }

            Console.WriteLine("Time taken: " + stopwatch.Elapsed);
        }

        private static void PrintResult(MetricCollection metrics)
        {
            Console.WriteLine($"Result for Solution: {metrics.Solution}");
            Console.WriteLine($"Total Number Of Classes: {metrics.TotalNumberOfClasses}");
            Console.WriteLine($"Total Number Of Methods: {metrics.TotalNumberOfMethods}");
            Console.WriteLine($"Cyclomatic Complexity Of Whole Code: {metrics.CyclomaticComplexity}");
            Console.WriteLine($"Total Number Of Namespaces: {metrics.TotalNumberOfNamespaces}");
            Console.WriteLine($"Total Number Of Lines Of Code: {metrics.TotalLinesOfCode}");
            Console.WriteLine();
            Console.WriteLine("Ratios:");
            Console.WriteLine($"NOC/NOP: {metrics.TotalNumberOfClasses / metrics.TotalNumberOfNamespaces}");
            Console.WriteLine($"NOM/NOC: {metrics.TotalNumberOfMethods / metrics.TotalNumberOfClasses}");
            Console.WriteLine($"LOC/NOM: {metrics.TotalLinesOfCode / metrics.TotalNumberOfMethods}");
            Console.WriteLine($"CYCLO/LOC: {metrics.CyclomaticComplexity / metrics.TotalLinesOfCode}");
            Console.WriteLine();
        }
    }
}