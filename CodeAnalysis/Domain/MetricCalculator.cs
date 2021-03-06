﻿using System;
using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Environment;
using static CodeAnalysis.Domain.RawKindConstants;

namespace CodeAnalysis.Domain
{
    internal class MetricCalculator : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const string DocumentationCommentSeparator = "///";

        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var namespaces = new HashSet<string>();
            var metricCollection = new MetricCollection(solution.FilePath.Split('\\').Last());

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    ConsolePrinter.PrintStatus(Operation.Analyzing, document.FilePath);
                    metricCollection.TotalNumberOfClasses += GetNumberOfClasses(document);
                    metricCollection.TotalNumberOfMethods += GetNumberOfMethods(document);
                    namespaces.UnionWith(GetNumberOfNamespaces(document));
                    metricCollection.CyclomaticComplexity += CalculateCyclomaticComplexity(document);
                    metricCollection.TotalLinesOfCode += CalculateLinesOfCode(document.GetSyntaxRootAsync().Result);
                }
            }
            metricCollection.TotalNumberOfNamespaces = namespaces.Count;
            return new List<ICSVPrintable> { metricCollection };
        }

        private int GetNumberOfClasses(Document sourceDocument)
        {
            var numberOfClasses = 0;
            numberOfClasses += _documentWalker.GetNodesFromDocument<ClassDeclarationSyntax>(sourceDocument).Count();
            numberOfClasses += _documentWalker.GetNodesFromDocument<InterfaceDeclarationSyntax>(sourceDocument).Count();
            numberOfClasses += _documentWalker.GetNodesFromDocument<EnumDeclarationSyntax>(sourceDocument).Count();
            numberOfClasses += _documentWalker.GetNodesFromDocument<StructDeclarationSyntax>(sourceDocument).Count();
            return numberOfClasses;
        }

        private int GetNumberOfMethods(Document sourceDocument)
        {
            var numberOfMethods = 0;
            numberOfMethods += _documentWalker.GetNodesFromDocument<MethodDeclarationSyntax>(sourceDocument).Count();
            numberOfMethods += _documentWalker.GetNodesFromDocument<AccessorDeclarationSyntax>(sourceDocument).Count();
            numberOfMethods += _documentWalker.GetNodesFromDocument<ConstructorDeclarationSyntax>(sourceDocument).Count();
            return numberOfMethods;
        }
        
        private HashSet<string> GetNumberOfNamespaces(Document sourceDocument)
        {
            var namespaces = new HashSet<string>();
            var classDeclarations = _documentWalker.GetNodesFromDocument<ClassDeclarationSyntax>(sourceDocument);
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
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<IfStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<WhileStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<ForStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<ForEachStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<SwitchSectionSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<ContinueStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<GotoStatementSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<CatchClauseSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<ConditionalExpressionSyntax>(sourceDocument).Count();
            cyclomaticComplexity += _documentWalker.GetNodesFromDocument<BinaryExpressionSyntax>(sourceDocument)
                .Count(t => (t.RawKind == CoalesceExpression || t.RawKind == LogicalAndExpression || t.RawKind == LogicalOrExpression));

            cyclomaticComplexity += GetNumberOfMethods(sourceDocument);
            return cyclomaticComplexity;
        }

        public int CalculateLinesOfCode(SyntaxNode node)
        {
            var totalLines = node.SyntaxTree.GetLineSpan(node.FullSpan).EndLinePosition.Line -
                             node.SyntaxTree.GetLineSpan(node.FullSpan).StartLinePosition.Line;
            var amountOfSingleLineCommentsAndEmptyLines = CountSingleLineCommentsAndEmptyLines(node.DescendantTokens());
            var amountOfMultiLineCommentLines = CountMultiLineCommentLines(node.DescendantTokens());
            var amountOfSingleLineBraces = CountSingleLineBraces(node);
            var amountOfDocumentationComments = CountDocumentationComments(node.DescendantTokens());
            var linesOfCode = totalLines
                - amountOfSingleLineCommentsAndEmptyLines 
                - amountOfMultiLineCommentLines 
                - amountOfSingleLineBraces
                - amountOfDocumentationComments;
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

        private int CountSingleLineBraces(SyntaxNode root)
        {
            var braces = root.DescendantTokens().Where(t => t.RawKind == OpenBraceToken 
                                                         || t.RawKind == CloseBraceToken);

            return braces.Select(brace => root.SyntaxTree.GetLineSpan(brace.Span).StartLinePosition.Line)
                .Select(lineNumber => root.SyntaxTree.GetText().Lines[lineNumber])
                .Count(line => line.ToString().Trim().Length == 1);
        }

        private int CountDocumentationComments(IEnumerable<SyntaxToken> tokensInFile)
        {
            return tokensInFile.Select
                (token => (from trivia in token.GetAllTrivia()
                           where trivia.RawKind == SingleLineDocumentationTrivia
                           select trivia))
                .Select(docTrivia => docTrivia.Sum
                (trivia => trivia.ToString().Split(new[] {DocumentationCommentSeparator}, StringSplitOptions.None).Length)).Sum();
        }
    }
}