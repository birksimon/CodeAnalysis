using System;
using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    internal class LawOfDemeterValidator : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();

        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            return documents.Select(GetLODViolations);
        }

        private OptimizationRecomendation GetLODViolations(Document document)
        {
            var semanticModel = document.GetSemanticModelAsync().Result;
            var methodInvocations = _documentWalker.GetNodesFromDocument<InvocationExpressionSyntax>(document);
            foreach (var methodInvocation in methodInvocations)
            {
                var tempvar1 = IsInvocationOfContainingType(methodInvocation, semanticModel);
                var tempvar2 = IsInvocationOfContainingMethodsParameters(methodInvocation, semanticModel);
                var tempvar3 = IsInvocationOfContainingTypesMembers(methodInvocation, semanticModel);
            }

            return new OptimizationRecomendation();
        }

        private bool IsInvocationOfContainingType(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = ModelExtensions.GetSymbolInfo(model, invocation).Symbol;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            var methodDeclarations = containingType.DescendantNodes().OfType<MethodDeclarationSyntax>();
            return methodDeclarations
                .Select(declaration => ModelExtensions.GetDeclaredSymbol(model, declaration))
                .Any(declarationSymbol => invocationSymbol.Equals(declarationSymbol));
        }


        // TODO Zugriff auf MembersVariables?
        // TODO Bei Listen fehlen Methoden der Enumerable Klasse. Wie?!
        private bool IsInvocationOfContainingMethodsParameters(InvocationExpressionSyntax invocation,
            SemanticModel model)
        {
            var invocationSymbol = ModelExtensions.GetSymbolInfo(model, invocation).Symbol;
            var containingMethod = _documentWalker.GetContainingNodeOfType<MethodDeclarationSyntax>(invocation);
            var parameters = containingMethod.ParameterList.Parameters;

            return (from parameterTypes in parameters.Select(x => x.DescendantNodes()
                .Where(t => t is IdentifierNameSyntax || t is PredefinedTypeSyntax || t is GenericNameSyntax || t is ArrayTypeSyntax))
                from type in parameterTypes
                select FindSymbolInfo(model, type)
                into symbolInfo
                where symbolInfo != null
                select CollectAllMembers(symbolInfo)).Any(members => members.Contains(invocationSymbol));
        }

        private bool IsInvocationOfContainingTypesMembers(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = ModelExtensions.GetSymbolInfo(model, invocation).Symbol;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            var members = containingType.Members.OfType<FieldDeclarationSyntax>();

            return (from parameterTypes in members.Select(x => x.DescendantNodes()
               .Where(t => t is IdentifierNameSyntax || t is PredefinedTypeSyntax || t is GenericNameSyntax || t is ArrayTypeSyntax))
                    from type in parameterTypes
                    select FindSymbolInfo(model, type)
               into symbolInfo
                    where symbolInfo != null
                    select CollectAllMembers(symbolInfo)).Any(member => member.Contains(invocationSymbol));
        }

        private ITypeSymbol FindSymbolInfo(SemanticModel model, SyntaxNode parameter)
        {
            try
            {
                var symbolInfo = (INamedTypeSymbol) model.GetSymbolInfo(parameter).Symbol;
                return symbolInfo;
            }
            catch (InvalidCastException)
            {
                try
                {
                    var symbolInfo = (IArrayTypeSymbol) model.GetSymbolInfo(parameter).Symbol;
                    return symbolInfo;
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
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

        private void TestFunccc()
        {
            _documentWalker.ToString();

            var tree = CSharpSyntaxTree.ParseText(@"
    public class MyClass
    {
        public void MyMethod()
        {
        }
    }");

            var syntaxRoot = tree.GetRoot();
            var MyClass = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var MyMethod = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First();


        }
    }
}
 