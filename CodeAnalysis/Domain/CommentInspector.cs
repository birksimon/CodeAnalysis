using System;
using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CodeAnalysis.Domain
{
    internal class CommentInspector
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int SingleLineCommentTrivia = 8541;
        private const int SufficientBlockSize = 10;

        public IEnumerable<OptimizationRecomendation> AnalyzeSolution(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var headliningComments = SearchForHeadliningComments(document).ToList();
                    if (headliningComments.Any())
                    {
                        yield return _documentWalker.CreateRecommendations(document, headliningComments,
                            RecommendationType.CommentHeadline);
                    }
                }
            }
        }

        //TODO Not working as intended, alomost all matches are false positives
        public IEnumerable<SyntaxTrivia> SearchForHeadliningComments(Document document)
        {
            var comments = GetAllComments(document);
            List<SyntaxTrivia> list = new List<SyntaxTrivia>();
            foreach (var comment in comments)
            {
                IEnumerable<TextLine> followingCodeLines = GetFollowingLinesUntilBlankLine(comment.Token).ToList();
                if (followingCodeLines.Count() >= 3 
                    && IsLineAComment(followingCodeLines.Last().LineNumber + 1, comment.SyntaxTree))
                {
                    list.Add(comment);
                }
            }
            return 
                list;
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
                if (isLineInTree(comment.SyntaxTree.GetRoot(), lineNo + i))
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
            if (isLineInTree(tree.GetRoot(), lineNumber))
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

        private bool isLineInTree(SyntaxNode rootNode, int lineNumber)
        {
            var endLine = rootNode.SyntaxTree.GetLineSpan(rootNode.FullSpan).EndLinePosition.Line;
            if (endLine < lineNumber)
            {
                return false;
            }
            return true;
        }
    }
}