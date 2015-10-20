using System;
using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeAnalysis.Domain
{
    internal class WorkspaceHandler
    {
        private const string TestString = "TEST";

        public Solution CreateSolutionsFromFilePath(string path)
        {
            Solution solution = null;
            ConsolePrinter.PrintStatus(Operation.BuildingSolution, path);
            try
            {
                var msWorkspace = MSBuildWorkspace.Create();
                solution = msWorkspace.OpenSolutionAsync(path).Result.GetIsolatedSolution();
            }
            catch (Exception e)
            {
                ConsolePrinter.PrintException(e, $"Cannot create solution for {path}");
            }
            return solution;
        }

        public Solution RemoveTestFiles(Solution solution)
        {
            var filteredSolutionStep1 = RemoveTestProjects(solution);
            var filteredSolutionStep2 = RemoveTestDocuments(filteredSolutionStep1);
            return filteredSolutionStep2;
        }

        public bool IsTestSolutions(Solution solution)
        {
            return solution.FilePath.ToUpper().Contains(TestString);
        }

        private Solution RemoveTestProjects(Solution solution)
        {
            var projectsToRemove = new List<ProjectId>();
            projectsToRemove.AddRange(from p in solution.Projects
                where p.Name.ToUpper().Contains(TestString)
                select p.Id);

            Solution filteredSolution = null;
            foreach (var project in projectsToRemove)
            {
                filteredSolution = solution.RemoveProject(project);
            }
            return filteredSolution ?? solution;
        }

        private Solution RemoveTestDocuments(Solution solution)
        {
            var documentsToRemove = new List<DocumentId>();
            foreach (var project in solution.Projects)
            {
                ConsolePrinter.PrintStatus(Operation.RemovingTestFiles, $"{solution.Projects}");
                documentsToRemove.AddRange(from d in project.Documents
                    where d.Name.ToUpper().Contains(TestString)
                    select d.Id);
            }
            Solution filteredSolution = null;
            foreach (var document in documentsToRemove)
            {
                filteredSolution = solution.RemoveDocument(document);
            }
            return filteredSolution ?? solution;
        }

        public Solution RemoveBlackListedDocuments(Solution solution, IEnumerable<string> blackList)
        {
            var originalBlackList = blackList.ToList();
            Solution filteredSolution = null;
            var blacklistedDocuments = new HashSet<DocumentId>();
            foreach (var project in solution.Projects)
            {
                foreach (var item in originalBlackList)
                {
                    blacklistedDocuments.UnionWith(from doc in project.Documents where doc.Name.Contains(item) select doc.Id);
                }
            }
            foreach (var document in blacklistedDocuments)
            {
                filteredSolution = solution.RemoveDocument(document);
            }
            return filteredSolution ?? solution;
        }
    }
}