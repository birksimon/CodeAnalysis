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
            var solutions = fileCrawler.GetSolutionsFromDirectory(directory);
            var filesToIgnore = (fileCrawler.GetIgnoredFiles(directory)).ToList();
            var csvWriter = new CSVWriter();
            var resultFile = directory + "/metrics.csv";

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var solution in solutions)
            {
                try
                {
                    var analyzer = new SolutionAnalyzer(solution);
                    var result = analyzer.Analyze(filesToIgnore);
                    if (result.TotalNumberOfNamespaces != 0)
                    {
                        csvWriter.WriteResultToFile(resultFile, result);
                        PrintResult(result);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error analysing " + solution + ":");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }

            stopwatch.Stop();
            Console.WriteLine("Time taken: " + stopwatch.Elapsed);
        }


        /*
        asdasd
        */

        /// <summary>
        /// lksdflaksdlf
        /// </summary>
        /// <param name="metrics"></param>
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