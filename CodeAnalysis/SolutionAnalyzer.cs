using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using static System.Environment;

namespace CodeAnalysis
{
    internal class SolutionAnalyzer
    {
        private readonly Solution _solution;
        private const int EndOfLineTrivia = 8539;
        private const int MultiLineTrivia = 8542;
        private const int OpenBraceToken = 8205;
        private const int CloseBraceToken = 8206;
        private const int CoalesceExpression = 8688;
        private const int LogicalOrExpression = 8675;
        private const int LogicalAndExpression = 8676;
        private const int SingleLineDocumentationTrivia = 8544;

        public SolutionAnalyzer(string solutionPath)
        {
            var msWorkspace = MSBuildWorkspace.Create();
            _solution = msWorkspace.OpenSolutionAsync(solutionPath).Result.GetIsolatedSolution(); //Isolated is faster
        }

        public MetricCollection Analyze(IEnumerable<string> filesToIgnore = null)
        {
            var namespaces = new HashSet<string>();
            var metricCollection = new MetricCollection(_solution.FilePath.Split('\\').Last());
            var blackList = (filesToIgnore ?? new [] {""}).ToList();
            
            foreach (var project in _solution.Projects)
            {
                var documentsToAnalyze = FilterDocuments(project.Documents, blackList);
                foreach (var document in documentsToAnalyze)
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

        private IEnumerable<Document> FilterDocuments (IEnumerable<Document> documents, IEnumerable<string> blackList)
        {
            var blacklistedDocuments = new HashSet<Document>();
            var originalDocuments = documents.ToList();
            foreach (var item in blackList)
            {
                blacklistedDocuments.UnionWith(from doc in originalDocuments where doc.Name.Contains(item) select doc);
            }
            return originalDocuments.Except(blacklistedDocuments);
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
            var numberOfMethods = 0;
            numberOfMethods += GetNodesFromDocument<MethodDeclarationSyntax>(sourceDocument).Count();
            numberOfMethods += GetNodesFromDocument<AccessorDeclarationSyntax>(sourceDocument).Count();
            numberOfMethods += GetNodesFromDocument<ConstructorDeclarationSyntax>(sourceDocument).Count();
            return numberOfMethods;
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
            cyclomaticComplexity += GetNodesFromDocument<CatchClauseSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<ConditionalExpressionSyntax>(sourceDocument).Count();
            cyclomaticComplexity += GetNodesFromDocument<BinaryExpressionSyntax>(sourceDocument)
                .Count(t => (t.RawKind == CoalesceExpression || t.RawKind == LogicalAndExpression || t.RawKind == LogicalOrExpression));

            cyclomaticComplexity += GetNumberOfMethods(sourceDocument);
            return cyclomaticComplexity;
        }

        private int CalculateLinesOfCode(Document sourceDocument)
        {
            var root = sourceDocument.GetSyntaxRootAsync().Result;
            var totalLines = root.SyntaxTree.GetText().Lines.Count;
            var amountOfSingleLineCommentsAndEmptyLines = CountSingleLineCommentsAndEmptyLines(root.DescendantTokens());
            var amountOfMultiLineCommentLines = CountMultiLineCommentLines(root.DescendantTokens());
            var amountOfSingleLineBraces = CountSingleLineBraces(root);
            var linesOfCode = totalLines
                - amountOfSingleLineCommentsAndEmptyLines 
                - amountOfMultiLineCommentLines 
                - amountOfSingleLineBraces;
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
                (token => (from trivia in token.GetAllTrivia() where (trivia.RawKind == MultiLineTrivia || trivia.RawKind == SingleLineDocumentationTrivia)select trivia))
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

        private IEnumerable<CSharpSyntaxNode> GetNodesFromDocument<TNode>(Document sourceDocument)
            where TNode : CSharpSyntaxNode
        {
            var root = sourceDocument.GetSyntaxRootAsync().Result;
            return root.DescendantNodes().OfType<TNode>();
        }
    }
}