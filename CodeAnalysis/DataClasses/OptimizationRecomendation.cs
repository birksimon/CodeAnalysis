using System.Collections.Generic;

namespace CodeAnalysis.DataClasses
{
    struct OptimizationRecomendation
    {
        public List<Occurence> Occurrences { get; set; }
        public KeyValuePair<string, string> WarningAndRecommendation { get; set; }

        public OptimizationRecomendation(string warning, string suggestion)
        {
            WarningAndRecommendation = new KeyValuePair<string, string>(warning, suggestion);
            Occurrences = new List<Occurence>();
        }
    }
}
