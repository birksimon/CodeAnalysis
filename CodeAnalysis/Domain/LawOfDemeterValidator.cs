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

            // Why is this necessary?!
            //methodInvocations.RemoveAll(item => item == null);

            var lodViolations = new List<InvocationExpressionSyntax>();
            foreach (var methodInvocation in methodInvocations)
            {
                if (semanticModel.GetSymbolInfo(methodInvocation).Symbol == null) 
                    continue;
                if (IsInvocationOfContainingType(methodInvocation, semanticModel))
                    continue;
                if (IsInvocationOfContainingMethodsParameters(methodInvocation, semanticModel))
                    continue;
                if (IsInvocationOfContainingTypesMembers(methodInvocation, semanticModel))
                    continue;
                if (IsInvocationOfInMethodCreatedObject(methodInvocation, semanticModel))
                    continue;
                if (IsStaticInvocation(methodInvocation, semanticModel))
                    continue;
                if (IsExtensionMethodInvocation(methodInvocation, semanticModel))
                    continue;
                lodViolations.Add(methodInvocation);
            }
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
            var containingMethod = _documentWalker.GetContainingNodeOfType<MethodDeclarationSyntax>(invocation);
            if (containingMethod == null)
                return false;
            var parameters = containingMethod.ParameterList.Parameters;
            return IsSymbolInvocationOfNodes(parameters, invocationSymbol, model);
        }

        private bool IsInvocationOfContainingTypesMembers(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            if (containingType == null)
                return false;
            var members = containingType.Members;
            return IsSymbolInvocationOfNodes(members, invocationSymbol, model);
        }

        private bool IsInvocationOfInMethodCreatedObject(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            var containingMethod = _documentWalker.GetContainingNodeOfType<MethodDeclarationSyntax>(invocation);
            if (containingMethod == null)
                return false;
            var objectCreations = containingMethod.DescendantNodes().OfType<LocalDeclarationStatementSyntax>();
            return IsSymbolInvocationOfNodes(objectCreations, invocationSymbol, model);
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

        private bool IsSymbolInvocationOfNodes(IEnumerable<SyntaxNode> nodes, ISymbol invocationSymbol, SemanticModel model )
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
    }
}
 