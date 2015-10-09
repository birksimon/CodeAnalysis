using System;
using System.Linq;

namespace CodeAnalysis
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            var directory = args[0];
            var fileCrawler = new FileCrawler();
            var solutions = fileCrawler.GetSolutionsFromDirectory(directory);
            var filesToIgnore = (fileCrawler.GetIgnoredFiles(directory)).ToList();

            foreach (var solution in solutions)
            {
                var analyzer = new SolutionAnalyzer(solution);
                var result = analyzer.Analyze(filesToIgnore);
                PrintResult(result);
            }
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
            Console.WriteLine($"NOC/NOP: {metrics.TotalNumberOfClasses/metrics.TotalNumberOfNamespaces}");
            Console.WriteLine($"NOM/NOC: {metrics.TotalNumberOfMethods / metrics.TotalNumberOfClasses}");
            Console.WriteLine($"LOC/NOM: {metrics.TotalLinesOfCode / metrics.TotalNumberOfMethods}");
            Console.WriteLine($"CYCLO/LOC: {metrics.CyclomaticComplexity / metrics.TotalLinesOfCode}");
            Console.WriteLine();
        }
    }
}