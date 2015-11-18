using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    class LimitConditionInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int MinusToken = 8202;
        private const int PlusToken = 8539;
        private const int AsteriskToken = 8199;
        private const int SlashToken = 8221;
        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            
            foreach (var document in documents)
            {
                var semanticModel = document.GetSemanticModelAsync().Result;
                var arithmeticOperationOnVariables = FindArithmeticOperationsOnVariables(document);
                var duplicates = FindReoccuringOperations(arithmeticOperationOnVariables, semanticModel);
                yield return _documentWalker.CreateRecommendations(document, duplicates, RecommendationType.LimitCondition);
            }
        }

        private IEnumerable<BinaryExpressionSyntax> FindArithmeticOperationsOnVariables(Document document)
        {
            var root = document.GetSyntaxRootAsync().Result;
            return root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                .Where(exp => exp.OperatorToken.RawKind == PlusToken || exp.OperatorToken.RawKind == MinusToken ||
                exp.OperatorToken.RawKind == AsteriskToken || exp.OperatorToken.RawKind == SlashToken);
        }

        private List<KeyValuePair<BinaryExpressionSyntax, BinaryExpressionSyntax>> FindReoccuringOperations(
            IEnumerable<BinaryExpressionSyntax> operations, SemanticModel semanticModel)
        {

            var originalList = operations.ToList();
            var copyList = originalList.ToList();
            var duplicates = new List<KeyValuePair<BinaryExpressionSyntax, BinaryExpressionSyntax>>();
            var alreadyFound = new HashSet<BinaryExpressionSyntax>();

            foreach (var oItem in originalList)
            {
                foreach (var cItem in copyList)
                {
                    if (alreadyFound.Contains(oItem) && alreadyFound.Contains(cItem)) continue;

                    if (IsEqualButNotTheSame(oItem, cItem, semanticModel))
                    {
                        alreadyFound.Add(oItem);
                        alreadyFound.Add(cItem);
                        duplicates.Add(new KeyValuePair<BinaryExpressionSyntax, BinaryExpressionSyntax>(oItem, cItem));
                        break;
                    }
                }
            }
            return duplicates;
        }

        //TODO is not working with nested BinaryExpressionSyntaxes
        private bool IsEqualButNotTheSame(BinaryExpressionSyntax exp1, BinaryExpressionSyntax exp2, SemanticModel semanticModel)
        {
            if (exp1.Equals(exp2)) return false;

            var exp1LeftSymbol = semanticModel.GetSymbolInfo(exp1.Left);
            var exp1RightSymbol = semanticModel.GetSymbolInfo(exp1.Right);
            var exp2LeftSymbol = semanticModel.GetSymbolInfo(exp2.Left);
            var exp2RightSymbol = semanticModel.GetSymbolInfo(exp2.Right);

            return exp1.OperatorToken.RawKind == exp2.OperatorToken.RawKind &&
                   ((exp1LeftSymbol.Equals(exp2LeftSymbol) && exp1RightSymbol.Equals(exp2RightSymbol)) ||
                    (exp1LeftSymbol.Equals(exp2RightSymbol) && exp1RightSymbol.Equals(exp2LeftSymbol)));
        }
    }
}
