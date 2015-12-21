using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CodeAnalysis.Domain.RawKindConstants;

namespace CodeAnalysis.Domain
{
    internal class ObjectAndDatastructureValidator : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();

        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            foreach (var document in documents)
            {
                yield return GetLODViolations(document);
                yield return FindHybridDatastructures(document);
            }
        }

        private ICSVPrintable FindHybridDatastructures(Document document)
        {
            var typeDeclarations = _documentWalker.GetNodesFromDocument<TypeDeclarationSyntax>(document);
            var hybridStructures = typeDeclarations.Where
                (declaration => DetermineClassType(declaration) == ClassType.Hybrid).ToList();
            return _documentWalker.CreateRecommendations(document, hybridStructures, RecommendationType.HybridDataStructure);
        }

        private ClassType DetermineClassType(TypeDeclarationSyntax declaration)
        {
            if (declaration.Keyword.RawKind == InterfaceKeywordToken) return ClassType.Other;
            var isDataStructure = HasPublicProperties(declaration) || HasPublicFields(declaration);
            var isObject = HasMethods(declaration);
            if (isDataStructure && isObject && HasNotOnlyConstOrReadonlyFields(declaration)) return ClassType.Hybrid;
            if (isObject) return ClassType.Object;
            if (isDataStructure) return ClassType.DataStructure;
            return ClassType.Other;
        }

        private bool HasPublicProperties(TypeDeclarationSyntax declaration)
        {
            var properties = declaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            return (properties.Where(property => property.DescendantTokens().Any(t => t.RawKind == PublicToken))
                .Any(property => property.AccessorList.Accessors.Count == 2));
        }

        private bool HasPublicFields(TypeDeclarationSyntax declaration)
        {
            var fields = declaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
            return fields.Any(field => field.DescendantTokens().Any(t => t.RawKind == PublicToken));
        }

        private bool HasMethods(TypeDeclarationSyntax declaration)
        {
            var methods = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            return methods.Any();
        }

        private bool HasNotOnlyConstOrReadonlyFields(TypeDeclarationSyntax declaration)
        {
            var fields = declaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields)
            {
                if (field.ChildTokens().Any(t => t.RawKind == PublicToken))
                {
                    if (!field.ChildTokens().Any(t => t.RawKind == ConstToken || t.RawKind == ReadOnlyToken))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        // TODO Was ist mit List[0].Foo() -> theoretisch Verstoß, aber wohl nicht sinnvoll
        // TODO Was ist mit ...ToString().Split('').... -> Grunddatentypen; Listen, LINQ OK
        private ICSVPrintable GetLODViolations(Document document)
        {
            var semanticModel = document.GetSemanticModelAsync().Result;
            var methodInvocations = _documentWalker.GetNodesFromDocument<InvocationExpressionSyntax>(document).ToList();            
            var lodViolations = 
                (from methodInvocation in methodInvocations
                 where semanticModel.GetSymbolInfo(methodInvocation).Symbol != null
                 where !IsDataStructure(methodInvocation)
                 where !IsInvocationOfContainingType(methodInvocation, semanticModel)
                 where !IsInvocationOfContainingMethodsParameters(methodInvocation, semanticModel)
                 where !IsInvocationOfContainingTypesMembers(methodInvocation, semanticModel)
                 where !IsInvocationOfInMethodCreatedObject(methodInvocation, semanticModel)
                 where !IsStaticInvocation(methodInvocation, semanticModel)
                 where !IsExtensionMethodInvocation(methodInvocation, semanticModel)
                 select methodInvocation).ToList();
            var recommendations = _documentWalker.CreateRecommendations(document, lodViolations, RecommendationType.LODViolation);
            return recommendations;
        }

        private bool IsDataStructure(InvocationExpressionSyntax invocation)
        {
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            return DetermineClassType(containingType) != ClassType.DataStructure;
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
 