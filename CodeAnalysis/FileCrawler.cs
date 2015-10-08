using System.Collections.Generic;
using System.IO;

namespace CodeAnalysis
{
    class FileCrawler
    {
        private const string SolutionFileExtension = "*.sln";

        public IEnumerable<string> GetSolutionsFromDirectory(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, SolutionFileExtension, SearchOption.AllDirectories);
        }
    }
}
