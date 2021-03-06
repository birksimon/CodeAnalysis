﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using static CodeAnalysis.Domain.RawKindConstants;

namespace CodeAnalysis.Domain
{
    internal class CommentInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int SufficientBlockSize = 10;

        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);

            var result = documents.AsParallel().SelectMany(GetRecomendationsForDocument);

            //var result = new List<OptimizationRecomendation>();
            //Parallel.ForEach(documents, document =>
            //{
            //    result.Add(GetRecommendationsForHeadliningComments(document));
            //    result.Add(GetRecommendationsForCodeInComments(document));
            //    result.Add(GetRecommendationsForDocumentationOnPrivateSoftwareUnits(document));
            //});
            return result;
        }

        private IEnumerable<ICSVPrintable> GetRecomendationsForDocument(Document document)
        {
            yield return GetRecommendationsForHeadliningComments(document);
            yield return GetRecommendationsForCodeInComments(document);
            yield return GetRecommendationsForDocumentationOnPrivateSoftwareUnits(document);
        }

        private OptimizationRecomendation GetRecommendationsForHeadliningComments(Document document)
        {
            var headliningComments = SearchForHeadliningComments(document);
            return _documentWalker.CreateRecommendations(document, headliningComments,
                RecommendationType.CommentHeadline);
        }

        private OptimizationRecomendation GetRecommendationsForDocumentationOnPrivateSoftwareUnits(Document document)
        {
            var documentationOnPrivateCode = SearchForDocumentationOnPrivateCode(document).ToList();
            return _documentWalker.CreateRecommendations(document, documentationOnPrivateCode, RecommendationType.DocumentationOnPrivateSoftwareUnits);
        }

        private IEnumerable<SyntaxTrivia> SearchForDocumentationOnPrivateCode(Document document)
        {
            var root = document.GetSyntaxRootAsync().Result;
            var allPrivateAccessModifiers = root.DescendantTokens().Where(token => token.RawKind == PrivateToken);
            var modifiersWithDocumentation = allPrivateAccessModifiers.Where(token => token.HasLeadingTrivia);
            var privateDocumentation = from tokens in modifiersWithDocumentation
                                       from trivia in tokens.LeadingTrivia
                                       where trivia.RawKind == SingleLineDocumentationTrivia
                                       select trivia;
            return privateDocumentation;
        }

        private OptimizationRecomendation GetRecommendationsForCodeInComments(Document document)
        {
            var codeInComments = SearchForCodeInComments(document).ToList();
            return _documentWalker.CreateRecommendations(document, codeInComments, RecommendationType.CodeInComment);
        }

        private IEnumerable<SyntaxTrivia> SearchForCodeInComments(Document document)
        {
            var comments = GetAllComments(document);
            return comments.Where(IsCode);
        }

        private bool IsCode(SyntaxTrivia comment)
        {
            Regex singeLineCommentRegex = new Regex(@"^\s*//.*;\s*$");
            return singeLineCommentRegex.IsMatch(comment.ToString());
        }

        public IEnumerable<SyntaxTrivia> SearchForHeadliningComments(Document document)
        {
            var comments = GetAllComments(document);
            return
                (from comment in comments
                 let followingCodeLines = GetFollowingLinesUntilBlankLine(comment.Token).ToList()
                 where followingCodeLines.Count() >= 3 && IsLineAComment(followingCodeLines.Last().LineNumber + 1, comment.SyntaxTree)
                 select comment).ToList();
        }

        public IEnumerable<SyntaxTrivia> GetAllComments(Document document)
        {
            var tree = document.GetSyntaxRootAsync().Result;
            var tokens = tree.DescendantTokens();
            return tokens.SelectMany(t => t.LeadingTrivia)
                .Where(trivia => trivia.RawKind == SingleLineCommentTrivia);
        }

        private IEnumerable<TextLine> GetFollowingLinesUntilBlankLine(SyntaxToken comment)
        {
            var lineNo = comment.SyntaxTree.GetLineSpan(comment.FullSpan).StartLinePosition.Line;
            var followingCode = new List<TextLine>();

            for (int i = 1; i <= SufficientBlockSize; i++)
            {
                if (IsLineInTree(comment.SyntaxTree.GetRoot(), lineNo + i))
                {
                    var line = GetCodeInLine(comment.SyntaxTree, lineNo + i);
                    followingCode.Add(line);
                    if (line.ToString() == "")
                        break;
                }
            }
            return followingCode;
        }

        private bool IsLineAComment(int lineNumber, SyntaxTree tree)
        {
            if (IsLineInTree(tree.GetRoot(), lineNumber))
            {
                var line = GetCodeInLine(tree, lineNumber);
                return line.ToString().StartsWith("//");
            }
            return false;
        }

        private TextLine GetCodeInLine(SyntaxTree syntaxTree, int lineNumber)
        {
            return syntaxTree.GetText().Lines[lineNumber];
        }

        private bool IsLineInTree(SyntaxNode rootNode, int lineNumber)
        {
            var endLine = rootNode.SyntaxTree.GetLineSpan(rootNode.FullSpan).EndLinePosition.Line;
            return !(endLine < lineNumber);
        }
    }
}