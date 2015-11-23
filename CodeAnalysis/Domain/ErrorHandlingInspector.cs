using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CodeAnalysis.Domain.RawKindConstants;

namespace CodeAnalysis.Domain
{
    class ErrorHandlingInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();


        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            foreach (var document in documents)
            {
                yield return FindNullReturnValues(document);
                yield return FindNullArguments(document);
                yield return FindErrorFlags(document);
            }
        }

        private OptimizationRecomendation FindNullReturnValues(Document doc)
        {
            var returnStatements = _documentWalker.GetNodesFromDocument<ReturnStatementSyntax>(doc);
            var nullReturns = FilterForNullReturns(returnStatements);
            return _documentWalker.CreateRecommendations(doc, nullReturns, RecommendationType.NullReturn);
        }

        private OptimizationRecomendation FindNullArguments(Document doc)
        {
            var invocations = _documentWalker.GetNodesFromDocument<InvocationExpressionSyntax>(doc);
            var argumentLists = invocations.Select(invocation => invocation.ArgumentList).ToList();
            var nullArguments = FilterForNullArguments(argumentLists);
            return _documentWalker.CreateRecommendations(doc, nullArguments, RecommendationType.NullArgument);
        }

        private OptimizationRecomendation FindErrorFlags(Document doc)
        {
            var returnStatements = _documentWalker.GetNodesFromDocument<ReturnStatementSyntax>(doc);
            var errorFlagCandidates = returnStatements.Where(IsAbsoluteValue).ToList();
            return _documentWalker.CreateRecommendations(doc, errorFlagCandidates, RecommendationType.ErrorFlag);
        }

        public bool IsAbsoluteValue(ReturnStatementSyntax returnStatement)
        {
            return returnStatement.ChildNodes().OfType<LiteralExpressionSyntax>().Any(n => n.RawKind == NumericLiteralExpression)
                || returnStatement.ChildNodes().OfType<PrefixUnaryExpressionSyntax>().Any(n => n.RawKind == UnaryMinusExpression);
        }
       
        private IEnumerable<ArgumentListSyntax> FilterForNullArguments(IEnumerable<ArgumentListSyntax> argumentLists)
        {
            var arguments = new List<ArgumentSyntax>();
            foreach (var argumentList in argumentLists)
            {
                arguments.AddRange(argumentList.Arguments);
            }
            return from argument in arguments
                   where argument.ChildNodes().OfType<LiteralExpressionSyntax>().Any(r => r.RawKind == NullLiteralExpresision)
                   select argument.Parent as ArgumentListSyntax;
        }

        private IEnumerable<ReturnStatementSyntax> FilterForNullReturns(IEnumerable<ReturnStatementSyntax> returnStatements)
        {
            return from nullReturns in returnStatements
                   where nullReturns.ChildNodes().OfType<LiteralExpressionSyntax>().Any(r => r.RawKind == NullLiteralExpresision)
                   select nullReturns;
        }
    }
}
