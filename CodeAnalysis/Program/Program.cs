using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CodeAnalysis.Domain;
using CodeAnalysis.Filesystem;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;

namespace CodeAnalysis.Program
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var directory = args[0];
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var analysisResults = RunAnalysis(directory);
            PrintResults(analysisResults, directory);
            stopwatch.Stop();          
        }

        private static IEnumerable<ICSVPrintable> RunAnalysis(string directory)
        {
            var solutionPaths = GetSolutionsFilePaths(directory);
            var unfiltereSolutions = GetVisualStudioSolutions(solutionPaths);
            var filteredSolutions = FilterSolutions(unfiltereSolutions);
            var analyzers = GetAllAnalyzers().ToList();
            var results = new List<ICSVPrintable>();

            foreach (var solution in filteredSolutions)
            {
                foreach (var analyzer in analyzers)
                {
                    //if (analyzer.GetType() == typeof(ObjectAndDatastructureValidator)) 
                        results.AddRange(analyzer.Analyze(solution));
                }
            }
            return results;
        }

        private static IEnumerable<string> GetSolutionsFilePaths(string directory)
        {
            var fileCrawler = new FileCrawler();
            return fileCrawler.GetSolutionsFromDirectory(directory);
        }

        private static IEnumerable<Solution> GetVisualStudioSolutions(IEnumerable<string> solutionFilePaths)
        {
            var workspaceHandler = new WorkspaceHandler();
            var unfilteredSolutions = solutionFilePaths.Select(solution => workspaceHandler.CreateSolutionsFromFilePath(solution));
            return FilterSolutions(unfilteredSolutions);
        }

        private static IEnumerable<Solution> FilterSolutions(IEnumerable<Solution> solutions, string pathBlackList = "")
        {
            var fileCrawler = new FileCrawler();
            var workspaceHandler = new WorkspaceHandler();
            var ienumSolutions = solutions.ToList();
            ienumSolutions.RemoveAll(item => item == null);
            var solutionsWithoutTests = ienumSolutions.Select(solution => workspaceHandler.RemoveTestFiles(solution));
            var filesToIgnore = fileCrawler.GetIgnoredFiles(pathBlackList);
            var solutionsWithoutBlackListFiles =solutionsWithoutTests.Select(solution => workspaceHandler.RemoveBlackListedDocuments(solution, filesToIgnore));
            return solutionsWithoutBlackListFiles;
        }

        private static IEnumerable<ICodeAnalyzer> GetAllAnalyzers()
        {
            var type = typeof(ICodeAnalyzer);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
            return types.Select(t => (ICodeAnalyzer) Activator.CreateInstance(t));
        }

        private static void PrintResults(IEnumerable<ICSVPrintable> results, string directory)
        {
            var csvWriter = new CSVWriter();
            foreach (var result in results)
            {
                csvWriter.WriteAnalysisResultToFile(directory, result);
            }
        }
    }
}