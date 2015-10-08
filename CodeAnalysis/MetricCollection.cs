namespace CodeAnalysis
{
    public struct MetricCollection
    {
        public readonly string Solution;
        public int TotalNumberOfClasses { get; set; }
        public int TotalNumberOfMethods { get; set; }
        public int TotalNumberOfNamespaces { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int TotalLinesOfCode { get; set; }

        public MetricCollection(string solution)
        {
            Solution = solution;
            TotalNumberOfClasses = 0;
            TotalNumberOfMethods = 0;
            TotalNumberOfNamespaces = 0;
            CyclomaticComplexity = 0;
            TotalLinesOfCode = 0;
        }
    }
}
