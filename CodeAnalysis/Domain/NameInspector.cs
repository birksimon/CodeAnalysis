using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CodeAnalysis.Domain.RawKindConstants;

namespace CodeAnalysis.Domain
{
    class NameInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();

        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            foreach (var document in documents)
            {
                var numberSeriesNames = GetNamesConsistingOfNumberSeries(document);
                var singleCharNames = GetNamesConsistingOfSingleChars(document);
                yield return _documentWalker.CreateRecommendations
                    (document, numberSeriesNames, RecommendationType.VariableNameIsNumberSeries);
                yield return _documentWalker.CreateRecommendations
                 (document, singleCharNames, RecommendationType.VariableNameConsistsOfSingleChar);
            }
        }

        private IEnumerable<SyntaxNode> GetNamesConsistingOfNumberSeries(Document document)
        {
            var declarations = GetDeclarations(document);
            return declarations.Where(IsNumberSeriesViolation);
        }

        private IEnumerable<SyntaxNode> GetNamesConsistingOfSingleChars(Document document)
        {
            var declarations = GetDeclarations(document);
            return declarations.Where(IsSingleCharViolation);
        }

        private IEnumerable<SyntaxNode> GetDeclarations(Document document)
        {
            var declarations = new List<SyntaxNode>();
            declarations.AddRange(_documentWalker.GetNodesFromDocument<TypeDeclarationSyntax>(document));
            declarations.AddRange(_documentWalker.GetNodesFromDocument<VariableDeclaratorSyntax>(document));
            declarations.AddRange(_documentWalker.GetNodesFromDocument<MethodDeclarationSyntax>(document));
            declarations.AddRange(_documentWalker.GetNodesFromDocument<ParameterSyntax>(document));
            return declarations;
        }

        // TODO: Exception e is allowed                    
        private bool IsNumberSeriesViolation(SyntaxNode declaration)
        {
            const string numberSeriesRegex = "^[a-zA-Z][0-9]{1,3}$";
            var identifier = declaration.ChildTokens().First(t => t.RawKind == IdentifierToken);
           // if (IsInLoop(declaration)) return false;
           // if (IsInLamda(declaration)) return false;
            return Regex.IsMatch(identifier.Value.ToString(), numberSeriesRegex);
        }

        private bool IsSingleCharViolation(SyntaxNode declaration)
        {
            var identifier = declaration.ChildTokens().First(t => t.RawKind == IdentifierToken);
            if (IsInLoop(declaration)) return false;
            if (IsInLamda(declaration)) return false;
            return identifier.Value.ToString().Length == 1;
        }

        private bool IsInLoop(SyntaxNode declaration)
        {
            return _documentWalker.HasContainingNodeOfType<WhileStatementSyntax>(declaration) ||
                   _documentWalker.HasContainingNodeOfType<ForStatementSyntax>(declaration) ||
                   _documentWalker.HasContainingNodeOfType<ForEachStatementSyntax>(declaration);
        }

        private bool IsInLamda(SyntaxNode declaration)
        {
            return _documentWalker.HasContainingNodeOfType<LambdaExpressionSyntax>(declaration);
        }
    }
}