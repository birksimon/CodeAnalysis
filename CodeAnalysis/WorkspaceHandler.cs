using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeAnalysis
{
    internal class WorkspaceHandler
    {
        private const string TestString = "TEST";

        public IEnumerable<Solution> CreateSolutionsFromFilePath(IEnumerable<string> paths)
        {
            var solutions = new List<Solution>();
            foreach (var solution in paths)
            {
                ConsolePrinter.PrintStatus(Operation.BuildingSolution, solution);
                try
                {
                    var msWorkspace = MSBuildWorkspace.Create();
                    solutions.Add(msWorkspace.OpenSolutionAsync(solution).Result.GetIsolatedSolution());
                }
                catch (Exception e)
                {
                    ConsolePrinter.PrintException(e, $"Cannot create solution for {solution}");
                }
            }
            return solutions;
        }

        public IEnumerable<Solution> RemoveTestFiles(IEnumerable<Solution> solutions)
        {
            var solutionList = solutions.ToList();
            var filteredSolutionsStep1 = RemoveTestSolutions(solutionList);
            var filteredSolutionsStep2 = RemoveTestProjects(filteredSolutionsStep1);
            var filteredSolutionsStep3 = RemoveTestDocuments(filteredSolutionsStep2);
            return filteredSolutionsStep3;
        }

        private IEnumerable<Solution> RemoveTestSolutions(IEnumerable<Solution> solutions)
        {
            return solutions.Where(solution => !solution.FilePath.ToUpper().Contains(TestString)).ToList();
        }

        private IEnumerable<Solution> RemoveTestProjects(IEnumerable<Solution> solutions)
        {
            var filteredSolutions = new List<Solution>();

            foreach (var solution in solutions)
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
                filteredSolutions.Add(filteredSolution ?? solution);
            }
            return filteredSolutions;
        }

        private IEnumerable<Solution> RemoveTestDocuments(IEnumerable<Solution> solutions)
        {
            var filteredSolutions = new List<Solution>();

            foreach (var solution in solutions)
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
                filteredSolutions.Add(filteredSolution ?? solution);
            }
            return filteredSolutions;
        }

        public IEnumerable<Solution> RemoveBlackListedDocuments(IEnumerable<Solution> solutions,
            IEnumerable<string> blackList)
        {
            var filteredSolutions = new List<Solution>();
            var originalBlackList = blackList.ToList();
            foreach (var solution in solutions)
            {
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
                filteredSolutions.Add(filteredSolution ?? solution);
            }
            
            return filteredSolutions;
        }
    }
}