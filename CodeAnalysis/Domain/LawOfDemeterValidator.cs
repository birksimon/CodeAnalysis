using System;
using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using Microsoft.CodeAnalysis;
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
            }

            return new OptimizationRecomendation();
        }

        private bool IsInvocationOfContainingType(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            var methodDeclarations = containingType.DescendantNodes().OfType<MethodDeclarationSyntax>();
            return methodDeclarations
                .Select(declaration => model.GetDeclaredSymbol(declaration))
                .Any(declarationSymbol => invocationSymbol.Equals(declarationSymbol));
        }


        // TODO Zugriff auf MembersVariables?
        // TODO Bei Listen fehlen Methoden der Enumerable Klasse. Wie?!
        // Bei diesem (unsinnigen) Beispiel funktioniert x.ChildNodes().First() nicht -> muss IdentifierNameSyntax sein
        // private bool IsInvocationOfContainingMethodsParameters(InvocationExpressionSyntax invocation,
        // [Serializable] SemanticModel model)
        private bool IsInvocationOfContainingMethodsParameters(InvocationExpressionSyntax invocation,
          SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            var containingMethod = _documentWalker.GetContainingNodeOfType<MethodDeclarationSyntax>(invocation);
            var parameters = containingMethod.ParameterList.Parameters;

            return (from parameterTypes in parameters.Select(x => x.DescendantNodes()
                    .Where(t => t is IdentifierNameSyntax || t is PredefinedTypeSyntax || t is GenericNameSyntax || t is ArrayTypeSyntax))
                    from type in parameterTypes
                    select FindSymbolInfo(model, type) into symbolInfo
                    where symbolInfo != null
                    select CollectAllMembers(symbolInfo)).Any(members => members.Contains(invocationSymbol));
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
    }
}