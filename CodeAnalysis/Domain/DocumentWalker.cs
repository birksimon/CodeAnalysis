using System;
using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Domain.Exceptions;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            while (parent != null)
            {
                if (parent is TNode)
                    return (TNode) parent;
                parent = parent.Parent;
            }
            throw new NodeDoesNotExistException($"Node {node} does not have a parent of type {typeof(TNode)}.");
        }

        public TNode GetContainingNodeOfType<TNode> (SyntaxToken token) where TNode:CSharpSyntaxNode
        {
            var parent = token.Parent;
            while (parent != null)
            {
                if (parent is TNode)
                    return (TNode)parent;
                parent = parent.Parent;
            }
            throw new NodeDoesNotExistException($"Node {token} does not have a parent of type {typeof(TNode)}.");
        }

        public bool TryGetContainingNodeOfType<TNode>(SyntaxNode node, out TNode containingNode) where TNode : CSharpSyntaxNode
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is TNode)
                {
                    containingNode = (TNode) parent;
                    return true;
                }
                parent = parent.Parent;
            }
            containingNode = null;
            return false;
        }

        public bool TryGetContainingNodeOfType<TNode>(SyntaxToken token, out TNode containingNode) where TNode : CSharpSyntaxNode
        {
            var parent = token.Parent;
            while (parent != null)
            {
                if (parent is TNode)
                {
                    containingNode = (TNode)parent;
                    return true;
                }
                parent = parent.Parent;
            }
            containingNode = null;
            return false;
        }

        public bool HasContainingNodeOfType<TNode>(SyntaxNode node) where TNode : CSharpSyntaxNode
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is TNode)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        public OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<SyntaxNode> nodes, RecommendationType recommendation)
        {
            var occurences = GenerateRuleViolationOccurences(nodes, document);
            return new OptimizationRecomendation(recommendation, occurences);
        }

        public OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<SyntaxTrivia> trivia, RecommendationType recommendation)
        {
            var occurences = GenerateRuleViolationOccurences(trivia, document);
            return new OptimizationRecomendation(recommendation, occurences);
        }

        public OptimizationRecomendation CreateRecommendations(Document document, List<KeyValuePair<BinaryExpressionSyntax,BinaryExpressionSyntax>> nodes, RecommendationType recommendation)
        {
            var occurences = GenerateRuleViolationOccurences(nodes, document);
            return new OptimizationRecomendation(recommendation, occurences);
        }

        private IEnumerable<Occurence> GenerateRuleViolationOccurences(IEnumerable<SyntaxNode> syntaxNode, Document document)
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

        private IEnumerable<Occurence> GenerateRuleViolationOccurences(List<KeyValuePair<BinaryExpressionSyntax, BinaryExpressionSyntax>> syntaxNodes, Document document)
        {
            var tree = document.GetSyntaxTreeAsync().Result;

            foreach (var nodePair in syntaxNodes)
            {
                yield return new Occurence()
                {
                    File = document.FilePath,
                    Line = tree.GetLineSpan(nodePair.Key.FullSpan).ToString().Split(' ').Last() + " & " +
                           tree.GetLineSpan(nodePair.Value.FullSpan).ToString().Split(' ').Last(),
                    CodeFragment = nodePair.Key + " & " +  nodePair.Value
                };
            }
        }
    }
}