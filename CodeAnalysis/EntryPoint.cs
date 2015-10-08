using System;
using System.Collections.Generic;

namespace CodeAnalysis
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            var fileCrawler = new FileCrawler();
            IEnumerable<string> solutions = fileCrawler.GetSolutionsFromDirectory(args[0]);

            foreach (var solution in solutions)
            {
                var analyzer = new SolutionAnalyzer(solution);
                var result = analyzer.Analyze();
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
