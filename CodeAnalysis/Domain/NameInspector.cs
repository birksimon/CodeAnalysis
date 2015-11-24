using System;
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
            return 
                (from document in documents
                 let numberSeries = GetNameViolations(document).ToList()
                 select _documentWalker.CreateRecommendations
                 (document, numberSeries,RecommendationType.VariableNameIsNumberSeries));
        }

        private IEnumerable<SyntaxNode> GetNameViolations(Document document)
        {
            var declarations = GetDeclarations(document);
            return declarations.Where(IsNameViolation);
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

        private bool IsNameViolation(SyntaxNode declaration)
        {
            const string numberSeriesRegex = "^[a-zA-Z][0-9]{1,3}$";
            var identifier = declaration.ChildTokens().First(t => t.RawKind == IdentifierToken);
            return Regex.IsMatch(identifier.Value.ToString(), numberSeriesRegex)
                   || identifier.Value.ToString().Length == 1;
        }
    }
}