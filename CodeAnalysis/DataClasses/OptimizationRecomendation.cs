using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;

namespace CodeAnalysis.DataClasses
{
    internal class OptimizationRecomendation : ICSVPrintable
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
                { RecommendationType.DocumentationOnPrivateSoftwareUnits, "Only use documentation on public API elements." },
                { RecommendationType.LODViolation, "Only talk to your closest frieds. Try to encapsulate nested method call." },
                { RecommendationType.NullReturn, "Do not return null. In case of error throw exception." },
                { RecommendationType.NullArgument, "Do not handover null-Arguments" },
                { RecommendationType.ErrorFlag, "Do not use error flags. E.g. throw an exception instead." },
                { RecommendationType.InheritanceDependency, "Base classes should not know about their derived classes." },
                { RecommendationType.LimitCondition, "Do not repeatly use the same limit condition. Encapsulate it in a new variable instead." }
            };

        public IEnumerable<Occurence> Occurrences { get; set; }
        public RecommendationType RecommendationType { get; set; }

        public OptimizationRecomendation(RecommendationType recommendationType, IEnumerable<Occurence> occurences)
        {
            RecommendationType = recommendationType;
            Occurrences = occurences;
        }
        
       public string GetCSVString()
        {
            var builder = new StringBuilder();

            foreach (var occurence in Occurrences)
            {
                builder.Append(RecommendationTypeToMessageMapping[RecommendationType]).Append(PrintConstants.Semicolon);
                builder.Append(Formatter.RemoveNewLines(occurence.CodeFragment)).Append(PrintConstants.Semicolon);
                builder.Append(occurence.Line).Append(PrintConstants.Semicolon);
                builder.Append(occurence.File).Append(Environment.NewLine);
            }
            return builder.ToString();
        }
        public string GetCSVHeader()
        {
            return "Recommendation;Codefragment;Line;File\n";
        }
        public bool IsEmpty()
        {
            return !Occurrences.Any();
        }

        public string GetFileName()
        {
            return "/OptimizationRecommendations.csv";
        }
    }
}