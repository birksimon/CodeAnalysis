using System.Collections.Generic;
using System.IO;

namespace CodeAnalysis
{
    class FileCrawler
    {
        private const string SolutionFileExtension = "*.sln";
        private const string BlackListFileName = "\\blacklist.csv";

        public IEnumerable<string> GetSolutionsFromDirectory(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, SolutionFileExtension, SearchOption.AllDirectories);
        }

        public IEnumerable<string> GetIgnoredFiles(string directoryPath)
        {
            if (File.Exists(directoryPath + BlackListFileName))
            {
                return File.ReadAllText(directoryPath + BlackListFileName).Split(';');
            }
            else
            {
                return new List<string>();
            }
        }
    }
}
