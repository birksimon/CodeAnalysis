using System.Collections.Generic;
using CodeAnalysis.Enums;

namespace CodeAnalysis.DataClasses
{
    struct OptimizationRecomendation
    {
        public static readonly Dictionary<RecommendationType, string> RecommendationTypeToMessageMapping
            = new Dictionary<RecommendationType, string>() {
                { RecommendationType.FunctionWithTooManyArguments, "Function has too many arguments. Suggested max. value is 3"},
                { RecommendationType.VariableNameIsNumberSeries, "Variable name consists of a number series. Give it a meaningful name."},
                { RecommendationType.FunctionIsTooBig, "A function should not be larger than 20 LOC. Try to extract functionality."}
        };

        public IEnumerable<Occurence> Occurrences { get; set; }
        public RecommendationType RecommendationType { get; set; }

        public OptimizationRecomendation(RecommendationType recommendationType, IEnumerable<Occurence> occurences)
        {
            RecommendationType = recommendationType;
            Occurrences = occurences;
        }
    }
}
