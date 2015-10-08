using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using static System.Environment;
/*
TODO: 
 - Is a constructor a method?
 - Count AssemblyInfo.cs and AssemblyAttributes.cs to LoC?
*/
namespace CodeAnalysis
{
    internal class SolutionAnalyzer
    {
        private readonly Solution _solution;
        private const int EndOfLineTrivia = 8539;
        private const int MultiLineTrivia = 8542;
        private const int CloseBraceToken = 8206;

        public SolutionAnalyzer(string solutionPath)
        {
            var msWorkspace = MSBuildWorkspace.Create();
            _solution = msWorkspace.OpenSolutionAsync(solutionPath).Result;
        }

        public MetricCollection Analyze()
        {
            var namespaces = new HashSet<string>();
            var metricCollection = new MetricCollection(_solution.FilePath);

            foreach (var project in _solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    metricCollection.TotalNumberOfClasses += GetNumberOfClasses(document);
                    metricCollection.TotalNumberOfMethods += GetNumberOfMethods(document);
                    namespaces.UnionWith(GetNumberOfNamespaces(document));
                    metricCollection.CyclomaticComplexity += CalculateCyclomaticComplexity(document);
                    metricCollection.TotalLinesOfCode += CalculateLinesOfCode(document);
                }
            }
            metricCollection.TotalNumberOfNamespaces = namespaces.Count;
            return metricCollection;
        }

        private int GetNumberOfClasses(Document sourceDocument)
        {
            var numberOfClasses = 0;
            numberOfClasses += GetNodesFromDocument<ClassDeclarationSyntax>(sourceDocument).Count();
            numberOfClasses += GetNodesFromDocument<InterfaceDeclarationSyntax>(sourceDocument).Count();
            numberOfClasses += GetNodesFromDocument<EnumDeclarationSyntax>(sourceDocument).Count();
            numberOfClasses += GetNodesFromDocument<StructDeclarationSyntax>(sourceDocument).Count();
            return numberOfClasses;
        }

        private int GetNumberOfMethods(Document sourceDocument)
        {
            return GetNodesFromDocument<MethodDeclarationSyntax>(sourceDocument).Count();
        }
        
        private HashSet<string> GetNumberOfNamespaces(Document sourceDocument)
        {
            var namespaces = new HashSet<string>();
            var classDeclarations = GetNodesFromDocument<ClassDeclarationSyntax>(sourceDocument);
            foreach (var declaration in classDeclarations)
            {
                var semanticModel = sourceDocument.GetSemanticModelAsync().Result;
                var symbol = semanticModel.GetDeclaredSymbol(declaration);
                var ns = symbol.ContainingNamespace;
                namespaces.Add(ns.Name);
            }
            return namespaces;
        }

        private int CalculateCyclomaticComplexity(Document sourceDocument)
        {
            int cyclomaticComplexity  = 0;
            cyclomaticComplexity += GetNodesFromDocument<IfStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<WhileStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<ForStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<ForEachStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<SwitchSectionSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<ContinueStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<GotoStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<BinaryExpressionSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<CatchClauseSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<ConditionalExpressionSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<MethodDeclarationSyntax>(sourceDocument).Count();
            return cyclomaticComplexity;
        }

        private int CalculateLinesOfCode(Document sourceDocument)
        {
            var root = sourceDocument.GetSyntaxRootAsync().Result;
            var totalLines = root.SyntaxTree.GetText().Lines.Count;
            var amountOfSingleLineCommentsAndEmptyLines = CountSingleLineCommentsAndEmptyLines(root.DescendantTokens());
            var amountOfMultiLineCommentLines = CountMultiLineCommentLines(root.DescendantTokens());
            var linesOfCode = totalLines - amountOfSingleLineCommentsAndEmptyLines - amountOfMultiLineCommentLines;
            return linesOfCode;
        }

        private int CountSingleLineCommentsAndEmptyLines(IEnumerable<SyntaxToken> tokensInFile)
        {
            var syntaxTokens = tokensInFile.ToList();
            var amountOfSingleLineCommentsAndEmptyLines = syntaxTokens.SelectMany(t => t.LeadingTrivia)
                .Count(trivia => trivia.RawKind == EndOfLineTrivia);
            var lastClosingBrace = syntaxTokens.FindLast(t => t.RawKind == CloseBraceToken);

            if (lastClosingBrace.ValueText != "None")
            {
                amountOfSingleLineCommentsAndEmptyLines += 
                    lastClosingBrace.TrailingTrivia.Count(trivia => trivia.RawKind == EndOfLineTrivia);  
            }

            return amountOfSingleLineCommentsAndEmptyLines;
        }

        private int CountMultiLineCommentLines(IEnumerable<SyntaxToken> tokensInFile)
        {
            return tokensInFile.Select
                (token => (from trivia in token.GetAllTrivia() where trivia.RawKind == MultiLineTrivia select trivia))
                .Select(multiLineComments => multiLineComments.Sum
                (comment => comment.ToString().Split(new[] {NewLine}, StringSplitOptions.None).Count() - 1)).Sum();
        } 

        private IEnumerable<CSharpSyntaxNode> GetNodesFromDocument<TNode>(Document sourceDocument)
            where TNode : CSharpSyntaxNode
        {
            var root = sourceDocument.GetSyntaxRootAsync().Result;
            return root.DescendantNodes().OfType<TNode>();
        }
    }
}