using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    class ErrorHandlingInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int NullLiteralExpresision = 8754;

        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            foreach (var document in documents)
            {
                yield return FindNullReturnValues(document);
            }
        }

        private OptimizationRecomendation FindNullReturnValues(Document doc)
        {
            var returnStatements = _documentWalker.GetNodesFromDocument<ReturnStatementSyntax>(doc);
            var nullReturns = FilterForNullReturns(returnStatements);
            return _documentWalker.CreateRecommendations(doc, nullReturns, RecommendationType.NullReturn);
        }

        private IEnumerable<ReturnStatementSyntax> FilterForNullReturns(IEnumerable<ReturnStatementSyntax> returnStatements)
        {
            return from nullReturns in returnStatements
                   where nullReturns.ChildNodes().OfType<LiteralExpressionSyntax>().Any(r => r.RawKind == NullLiteralExpresision)
                   select nullReturns;
        }
    }
}
