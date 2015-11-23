using System.IO;

namespace CodeAnalysis.Output
{
    internal class CSVWriter
    {
        public void WriteAnalysisResultToFile(string path, ICSVPrintable analysisResult)
        {
            if (analysisResult.IsEmpty()) return;
            var filePath = path + analysisResult.GetFileName();
            InitializeFile(filePath, analysisResult.GetCSVHeader());
            WriteLineToFile(filePath, analysisResult.GetCSVString());
        }
        private void InitializeFile(string path, string header)
        {
            if (File.Exists(path)) return;
            CreateFile(path);
            WriteLineToFile(path, header);
        }
        private void CreateFile(string path)
        {
            File.Create(path).Dispose();
        }
        private void WriteLineToFile(string path, string line)
        {
            File.AppendAllText(path, line);
        }
    }
}