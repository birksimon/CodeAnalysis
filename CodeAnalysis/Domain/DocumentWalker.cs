using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
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

        public IEnumerable<Occurence> GenerateRuleViolationOccurences(IEnumerable<CSharpSyntaxNode> syntaxNode, Document document)
        {
            var tree = document.GetSyntaxTreeAsync().Result;
            return syntaxNode.Select(declaration => new Occurence()
            {
                File = document.FilePath,
                Line = tree.GetLineSpan(declaration.FullSpan).ToString().Split(' ').Last(),
                CodeFragment = declaration.ToString()
            });
        }
    }
}