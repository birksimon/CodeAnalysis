using System;
using CodeAnalysis.Enums;

namespace CodeAnalysis.Output
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
    }
}
