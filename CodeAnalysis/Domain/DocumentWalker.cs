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

        public bool HasContainingNodeOfType<TNode>(SyntaxNode node) where TNode : CSharpSyntaxNode
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is TNode)
                {
                    return true;
                }
                parent = parent.Parent;
            }
            return false;
        }

        public bool IsSymbolInvocationOfNodes(IEnumerable<SyntaxNode> nodes, ISymbol invocationSymbol, SemanticModel model)
        {
            return
                (from node in nodes
                 from identifier in node.DescendantNodes().Where((
                 t => t is IdentifierNameSyntax || t is PredefinedTypeSyntax || t is GenericNameSyntax || t is ArrayTypeSyntax))
                 select FindSymbolInfo(model, identifier) into typeSymbol
                 where typeSymbol != null
                 from member in CollectAllMembers(typeSymbol)
                 select member).Any(member => member.Name.Equals(invocationSymbol.Name));
        }

        private ITypeSymbol FindSymbolInfo(SemanticModel model, SyntaxNode parameter)
        {
            return model.GetSymbolInfo(parameter).Symbol as ITypeSymbol;
        }

        private List<ISymbol> CollectAllMembers(ITypeSymbol symbolInfo)
        {
            var members = symbolInfo.GetMembers().ToList();
            var parent = symbolInfo.BaseType;
            while (parent != null)
            {
                members.AddRange(parent.GetMembers());
                parent = parent.BaseType;
            }
            return members;
        }

        //TODO Refactor (DRY!!!)
        public OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<SyntaxNode> nodes, RecommendationType recommendation)
        {
            var occurences = GenerateRuleViolationOccurences(nodes, document).ToList();
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

            foreach (var node in syntaxNode)
            {
                var occ = new Occurence()
                {
                    File = document.FilePath,
                    Line = tree.GetLineSpan(node.FullSpan).ToString().Split(' ').Last(),
                    CodeFragment = node.ToString().Length > 100 ? node.ToString().Substring(0,100) : node.ToString()
                };
                yield return occ;
            }
        }

        private IEnumerable<Occurence> GenerateRuleViolationOccurences(IEnumerable<SyntaxTrivia> syntaxTrivia, Document document)
        {
            var tree = document.GetSyntaxTreeAsync().Result;
            return syntaxTrivia.Select(declaration => new Occurence()
            {
                File = document.FilePath,
                Line = tree.GetLineSpan(declaration.FullSpan).ToString().Split(' ').Last(),
                CodeFragment = declaration.ToString().Length > 100 ? declaration.ToString().Substring(0, 100) : declaration.ToString()
            });
        }

        private IEnumerable<Occurence> GenerateRuleViolationOccurences(List<KeyValuePair<BinaryExpressionSyntax, BinaryExpressionSyntax>> syntaxNodes, Document document)
        {
            var tree = document.GetSyntaxTreeAsync().Result;

            return syntaxNodes.Select(nodePair => new Occurence()
            {
                File = document.FilePath,
                Line = tree.GetLineSpan(nodePair.Key.FullSpan).ToString().Split(' ').Last() + " & " +
                       tree.GetLineSpan(nodePair.Value.FullSpan).ToString().Split(' ').Last(),
                CodeFragment = nodePair.Key + " & " +  nodePair.Value
            });
        }
    }
}