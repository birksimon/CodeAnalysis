using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    class FunctionInspector
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int MaxParameters = 3;

        public IEnumerable<OptimizationRecomendation> AnalyzeSolution(Solution solution)
        {
            return 
                from project in solution.Projects
                from document in project.Documents
                let functions = SearchForFunctionsWithTooManyArguments(document)
                where functions.Any()
                select CreateRecommendations(document, functions);
        }

        private IEnumerable<ParameterListSyntax> SearchForFunctionsWithTooManyArguments(Document document)
        {
            var parameterLists = _documentWalker.GetNodesFromDocument<ParameterListSyntax>(document);
            return parameterLists.Where(list => list.Parameters.Count > MaxParameters);
        }

        private OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<ParameterListSyntax> functions)
        {
            var occurences = _documentWalker.GenerateRuleViolationOccurences(functions, document);
            return new OptimizationRecomendation(RecommendationType.FunctionWithTooManyArguments, occurences);
        }
    }
}
