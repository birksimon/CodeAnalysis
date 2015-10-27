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

                    var flagArguments = SearchForFlagArguments(document).ToList();
                    if (flagArguments.Any())
                    {
                        yield return
                            _documentWalker.CreateRecommendations(document, flagArguments,
                                RecommendationType.FlagArgument);
                    }
                }
            }
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

        private IEnumerable<ParameterSyntax> SearchForFlagArguments(Document document)
        {
            var parameterLists = _documentWalker.GetNodesFromDocument<ParameterListSyntax>(document);
            var boolParameters = GetBoolParameters(parameterLists).ToList();
            var conditionals = GetConditionalsInParametersMethods(boolParameters);
            var identifiers = GetIdentifiersInConditionals(conditionals);
            var flagArguments = GetParametersUsedAsIdentifiersInConditionals(boolParameters, identifiers);
            return flagArguments.Distinct();
        }

        private IEnumerable<ParameterSyntax> GetBoolParameters(IEnumerable<ParameterListSyntax> parameterLists)
        {
            return 
                from parameterList in parameterLists
                from parameter in parameterList.Parameters
                where parameter.Type != null
                let parameterType = parameter.Type.ToString().ToUpper()
                where parameterType == "BOOLEAN" || parameterType == "BOOL"
                select parameter;
        }

        private IEnumerable<SyntaxNode> GetConditionalsInParametersMethods(
            IEnumerable<ParameterSyntax> parameters)
        {
            var conditionals = new List<SyntaxNode>();
            foreach (var methodDeclaration in parameters.Select(parameter => parameter.Parent.Parent))
            {
                conditionals.AddRange(methodDeclaration.DescendantNodes().OfType<IfStatementSyntax>());
                conditionals.AddRange(methodDeclaration.DescendantNodes().OfType<ConditionalExpressionSyntax>());
            }
            return conditionals;
        }

        private IEnumerable<IdentifierNameSyntax> GetIdentifiersInConditionals(
            IEnumerable<SyntaxNode> conditionals)
        {
            var identifiers = new List<IdentifierNameSyntax>();
            foreach (var conditional in conditionals)
            {
                identifiers.AddRange(conditional.DescendantNodes().OfType<IdentifierNameSyntax>());
            }
            return identifiers;
        }

        private IEnumerable<ParameterSyntax> GetParametersUsedAsIdentifiersInConditionals(IEnumerable<ParameterSyntax> parameters,
            IEnumerable<IdentifierNameSyntax> identifiers)
        {
            return 
                from parameter in parameters
                from identifier in identifiers.ToList()
                where identifier.Identifier.Value == parameter.Identifier.Value
                select parameter;
        }
    }
}
