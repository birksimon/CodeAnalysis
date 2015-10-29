using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeAnalysis.Enums;

namespace CodeAnalysis.DataClasses
{
    internal struct OptimizationRecomendation
    {
        public static readonly Dictionary<RecommendationType, string> RecommendationTypeToMessageMapping
            = new Dictionary<RecommendationType, string>()
            {
                { RecommendationType.FunctionWithTooManyArguments, "Function has too many arguments. Suggested max. value is 3." },
                { RecommendationType.VariableNameIsNumberSeries, "Variable name consists of a number series. Give it a meaningful name." },
                { RecommendationType.FunctionIsTooBig, "A function should not be larger than 20 LOC. Try to extract functionality." },
                { RecommendationType.FlagArgument, "Do not use Flag-Arguments as it violates the SRP. Perhapse use two functions instead." },
                { RecommendationType.CommentHeadline, "Do not use comments to describe the functionality of a codeblock. Extract block into separate function instead." },
                { RecommendationType.CodeInComment, "Do not comment out code. Delete it!." },
                { RecommendationType.DocumentationOnPrivateSoftwareUnits, "Only use documentation on public API elements." }
            };

        private const char Semicolon = ';';
        public IEnumerable<Occurence> Occurrences { get; set; }
        public RecommendationType RecommendationType { get; set; }

        public OptimizationRecomendation(RecommendationType recommendationType, IEnumerable<Occurence> occurences)
        {
            RecommendationType = recommendationType;
            Occurrences = occurences;
        }

        public string ToCSVString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var occurence in Occurrences)
            {
                builder.Append(RecommendationTypeToMessageMapping[RecommendationType]).Append(Semicolon);
                builder.Append(@"" + occurence.CodeFragment 
                    .Replace(Environment.NewLine, "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("\r\n", "")
                    .Replace("\n\r", "")
                    .Replace(";", " ")
                               + "")
                    .Append(Semicolon);
                builder.Append(occurence.Line).Append(Semicolon);
                builder.Append(occurence.File).Append(Environment.NewLine);
            }
            return builder.ToString();
        }

        public bool HasOccurences()
        {
            return Occurrences.Any();
        }
    }
}