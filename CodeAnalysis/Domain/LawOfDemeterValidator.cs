using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
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

        // TODO Was ist mit List[0].Foo() -> theoretisch Verstoß, aber wohl nicht sinnvoll
        // TODO Was ist mit ...ToString().Split('').... -> Grunddatentypen; Listen, LINQ OK
        private OptimizationRecomendation GetLODViolations(Document document)
        {
            var semanticModel = document.GetSemanticModelAsync().Result;
            var methodInvocations = _documentWalker.GetNodesFromDocument<InvocationExpressionSyntax>(document).ToList();            
            var lodViolations = 
                (from methodInvocation in methodInvocations
                 where semanticModel.GetSymbolInfo(methodInvocation).Symbol != null
                 where !IsInvocationOfContainingType(methodInvocation, semanticModel)
                 where !IsInvocationOfContainingMethodsParameters(methodInvocation, semanticModel)
                 where !IsInvocationOfContainingTypesMembers(methodInvocation, semanticModel)
                 where !IsInvocationOfInMethodCreatedObject(methodInvocation, semanticModel)
                 where !IsStaticInvocation(methodInvocation, semanticModel)
                 where !IsExtensionMethodInvocation(methodInvocation, semanticModel)
                 select methodInvocation).ToList();

            return _documentWalker.CreateRecommendations(document, lodViolations, RecommendationType.LODViolation);
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

        private bool IsInvocationOfContainingMethodsParameters(InvocationExpressionSyntax invocation,
            SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            MethodDeclarationSyntax containingMethod;
            if (_documentWalker.TryGetContainingNodeOfType(invocation, out containingMethod))
            {
                var parameters = containingMethod.ParameterList.Parameters;
                return _documentWalker.IsSymbolInvocationOfNodes(parameters, invocationSymbol, model);
            }
            return false;
        }

        private bool IsInvocationOfContainingTypesMembers(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            if (containingType == null)
                return false;
            var members = containingType.Members;
            return _documentWalker.IsSymbolInvocationOfNodes(members, invocationSymbol, model);
        }

        private bool IsInvocationOfInMethodCreatedObject(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            MethodDeclarationSyntax containingMethod; 
            if (_documentWalker.TryGetContainingNodeOfType(invocation, out containingMethod))
            {
                var objectCreations = containingMethod.DescendantNodes().OfType<LocalDeclarationStatementSyntax>();
                return _documentWalker.IsSymbolInvocationOfNodes(objectCreations, invocationSymbol, model);
            }
            return false;
        }

        private bool IsStaticInvocation(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            return invocationSymbol.IsStatic;
        }

        private bool IsExtensionMethodInvocation(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            return invocationSymbol != null && invocationSymbol.IsExtensionMethod;
        }     
    }
}
 