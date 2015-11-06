using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalysis.Domain
{
    internal class DocumentWalker
    {
        public IEnumerable<TNode> GetNodesFromDocument<TNode>(Document sourceDocument)
            where TNode : CSharpSyntaxNode
        {
            var root = sourceDocument.GetSyntaxRootAsync().Result;
            return root.DescendantNodes().OfType<TNode>();
        }

        public IEnumerable<Document> GetAllDocumentsFromSolution(Solution solution)
        {
            return solution.Projects.SelectMany(project => project.Documents);
        }

        public TNode GetContainingNodeOfType<TNode>(SyntaxNode node) where TNode : CSharpSyntaxNode
        {
            var parent = node.Parent;
            while (true)
            {
                if (parent is TNode)
                    return (TNode) parent;
                if (parent.Parent == null)
                    return null;
                parent = parent.Parent;
            }
        }

        public OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<CSharpSyntaxNode> nodes, RecommendationType recommendation)
        {
            var occurences = GenerateRuleViolationOccurences(nodes, document);
            return new OptimizationRecomendation(recommendation, occurences);
        }

        public OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<SyntaxTrivia> trivia, RecommendationType recommendation)
        {
            var occurences = GenerateRuleViolationOccurences(trivia, document);
            return new OptimizationRecomendation(recommendation, occurences);
        }

        private IEnumerable<Occurence> GenerateRuleViolationOccurences(IEnumerable<CSharpSyntaxNode> syntaxNode, Document document)
        {
            var tree = document.GetSyntaxTreeAsync().Result;
            return syntaxNode.Select(declaration => new Occurence()
            {
                File = document.FilePath,
                Line = tree.GetLineSpan(declaration.FullSpan).ToString().Split(' ').Last(),
                CodeFragment = declaration.ToString()
            });
        }

        private IEnumerable<Occurence> GenerateRuleViolationOccurences(IEnumerable<SyntaxTrivia> syntaxTrivia, Document document)
        {
            var tree = document.GetSyntaxTreeAsync().Result;
            return syntaxTrivia.Select(declaration => new Occurence()
            {
                File = document.FilePath,
                Line = tree.GetLineSpan(declaration.FullSpan).ToString().Split(' ').Last(),
                CodeFragment = declaration.ToString()
            });
        }
    }
}