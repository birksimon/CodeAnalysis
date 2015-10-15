using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis
{
    class ConsolePrinter
    {
        public static void PrintStatus(Operation operation, string project)
        {
            Console.WriteLine($"{operation} {project}");
        }

        public static void PrintException(Exception e, string text)
        {
            Console.WriteLine($"{text}: ");
            Console.WriteLine($"{e.StackTrace}:");
            Console.WriteLine($"{e}:");
        }

        public static void PrintResult(MetricCollection metrics)
        {
            Console.WriteLine($"Result for Solution: {metrics.Solution}");
            Console.WriteLine($"Total Number Of Classes: {metrics.TotalNumberOfClasses}");
            Console.WriteLine($"Total Number Of Methods: {metrics.TotalNumberOfMethods}");
            Console.WriteLine($"Cyclomatic Complexity Of Whole Code: {metrics.CyclomaticComplexity}");
            Console.WriteLine($"Total Number Of Namespaces: {metrics.TotalNumberOfNamespaces}");
            Console.WriteLine($"Total Number Of Lines Of Code: {metrics.TotalLinesOfCode}");
            Console.WriteLine();
            if (!(metrics.TotalLinesOfCode == 0 || metrics.TotalNumberOfClasses == 0 
                || metrics.TotalNumberOfMethods == 0 || metrics.TotalNumberOfNamespaces == 0))
            { 
                Console.WriteLine("Ratios:");
                Console.WriteLine($"NOC/NOP: {metrics.TotalNumberOfClasses / metrics.TotalNumberOfNamespaces}");
                Console.WriteLine($"NOM/NOC: {metrics.TotalNumberOfMethods / metrics.TotalNumberOfClasses}");
                Console.WriteLine($"LOC/NOM: {metrics.TotalLinesOfCode / metrics.TotalNumberOfMethods}");
                Console.WriteLine($"CYCLO/LOC: {metrics.CyclomaticComplexity / metrics.TotalLinesOfCode}");
            }
            Console.WriteLine();
        }

        public static void PrintStuff(string text, object value)
        {
            Console.WriteLine($"{text} {value}");
        }
    }
}
