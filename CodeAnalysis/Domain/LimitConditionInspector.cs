using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CodeAnalysis.Domain.RawKindConstants;

namespace CodeAnalysis.Domain
{
    class LimitConditionInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        
        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            return (
                from document in documents
                let semanticModel = document.GetSemanticModelAsync().Result
                let arithmeticOperationOnVariables = FindArithmeticOperationsOnVariables(document)
                let duplicates = FindReoccuringOperations(arithmeticOperationOnVariables, semanticModel)
                select _documentWalker.CreateRecommendations(document, duplicates, RecommendationType.LimitCondition));
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
            var copiedList = originalList.ToList();
            var duplicates = new List<KeyValuePair<BinaryExpressionSyntax, BinaryExpressionSyntax>>();
            var alreadyFound = new HashSet<BinaryExpressionSyntax>();

            foreach (var oItem in originalList)
            {
                foreach (var cItem in copiedList)
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

        private bool IsEqualButNotTheSame(BinaryExpressionSyntax exp1, BinaryExpressionSyntax exp2, SemanticModel semanticModel)
        {
            if (exp1.Equals(exp2)) return false;

            var exp1AndNestings = exp1.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().ToList();
            var exp2AndNestings = exp2.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().ToList();

            if (exp1AndNestings.Count() != exp2AndNestings.Count()) return false;

            for (var i = 0; i < exp1AndNestings.Count(); i++)
            {
                if (!IsOperatorEqual(exp1AndNestings[i], exp2AndNestings[i])) return false;
                if (!AreContainedLiteralExpressionsEqual(exp1AndNestings[i], exp2AndNestings[i])) return false;
                if (!AreContainedSymbolsEqual(exp1AndNestings[i], exp2AndNestings[i], semanticModel)) return false;
            }
            return true;
        }

        private bool IsOperatorEqual(BinaryExpressionSyntax exp1, BinaryExpressionSyntax exp2)
        {
            return exp1.OperatorToken.RawKind == exp2.OperatorToken.RawKind;
        }

        private bool AreContainedLiteralExpressionsEqual(BinaryExpressionSyntax exp1, BinaryExpressionSyntax exp2)
        {
            var exp1LeftLiteral = exp1.Left as LiteralExpressionSyntax;
            var exp1RightLiteral = exp1.Right as LiteralExpressionSyntax;
            var exp2LeftLiteral = exp2.Left as LiteralExpressionSyntax;
            var exp2RightLiteral = exp2.Right as LiteralExpressionSyntax;

            if (exp1LeftLiteral != null && exp2LeftLiteral != null)
                if (!(exp1LeftLiteral.Token.Value.Equals(exp2LeftLiteral.Token.Value)))
                    return false;
            if (exp1RightLiteral != null && exp2RightLiteral != null)
                if (!exp1RightLiteral.Token.Value.Equals(exp2RightLiteral.Token.Value))
                    return false;
            return AreNullValuesDistributedSymmetrically(exp1LeftLiteral, exp1RightLiteral, exp2LeftLiteral, exp2RightLiteral);
        }

        private bool AreContainedSymbolsEqual(BinaryExpressionSyntax exp1, BinaryExpressionSyntax exp2,
            SemanticModel semanticModel)
        {
            var exp1LeftSymbol = semanticModel.GetSymbolInfo(exp1.Left).Symbol;
            var exp1RightSymbol = semanticModel.GetSymbolInfo(exp1.Right).Symbol;
            var exp2LeftSymbol = semanticModel.GetSymbolInfo(exp2.Left).Symbol;
            var exp2RightSymbol = semanticModel.GetSymbolInfo(exp2.Right).Symbol;

            if (exp1LeftSymbol != null && exp2LeftSymbol != null)
                if (!(exp1LeftSymbol.Equals(exp2LeftSymbol))) return false;
            if (exp1RightSymbol != null && exp2RightSymbol != null)
                if (!(exp1RightSymbol.Equals(exp2RightSymbol))) return false;
            return AreNullValuesDistributedSymmetrically(exp1LeftSymbol, exp1RightSymbol, exp2LeftSymbol, exp2RightSymbol);
        }

        private bool AreNullValuesDistributedSymmetrically(object exp1Left, object exp1Right, object exp2Left,
            object exp2Right)
        {
            if (exp1Left == null && exp2Left != null) return false;
            if (exp1Left != null && exp2Left == null) return false;
            if (exp1Right == null && exp2Right != null) return false;
            if (exp1Right != null && exp2Right == null) return false;
            return true;
        }
    }
}
