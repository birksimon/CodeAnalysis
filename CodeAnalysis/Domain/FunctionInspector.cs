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
        private const int MaxFunctionLOC = 20;

        public IEnumerable<OptimizationRecomendation> AnalyzeSolution(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var functionArgumentViolations = SearchForFunctionsWithTooManyArguments(document).ToList();
                    if (functionArgumentViolations.Any())
                    {
                        yield return _documentWalker.CreateRecommendations
                            (document, functionArgumentViolations, RecommendationType.FunctionWithTooManyArguments);
                    }

                    var functionSizeViolations = SearchForTooLongFunctions(document).ToList();
                    if (functionSizeViolations.Any())
                    {
                        yield return _documentWalker.CreateRecommendations
                            (document, functionSizeViolations, RecommendationType.FunctionIsTooBig);
                    }
                }
            }

            //return 
            //    from project in solution.Projects
            //    from document in project.Documents
            //    let functions = SearchForFunctionsWithTooManyArguments(document)
            //    where functions.Any()
            //    select _documentWalker.CreateRecommendations(document, functions);

        }

        private IEnumerable<ParameterListSyntax> SearchForFunctionsWithTooManyArguments(Document document)
        {
            var parameterLists = _documentWalker.GetNodesFromDocument<ParameterListSyntax>(document);
            return parameterLists.Where(list => list.Parameters.Count > MaxParameters);
        }

        private IEnumerable<MethodDeclarationSyntax> SearchForTooLongFunctions(Document document)
        {
            var metricCalculator = new MetricCalculator();
            var methodDeclarations = _documentWalker.GetNodesFromDocument<MethodDeclarationSyntax>(document);           
            return methodDeclarations
                .Where(declaration => metricCalculator.CalculateLinesOfCode(declaration) > MaxFunctionLOC);
        }
    }
}
