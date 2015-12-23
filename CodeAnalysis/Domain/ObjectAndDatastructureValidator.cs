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

        private ClassType DetermineClassType(INamedTypeSymbol classType)
        {
            var members = classType.GetMembers();
            var hasPublicProperties = false;
            var hasPublicFields = false;
            var hasMethods = false;
            var hasNotOnlyConstOrReadonlyFields = false;

            foreach (var member in members)
            {
                if (IsAMethods(member)) hasMethods = true;
                if (IsAPublicProperty(member)) hasPublicProperties = true;
                if (IsNotAReadonlyFieldOrAConstant(member)) hasNotOnlyConstOrReadonlyFields = true;
                if (IsAPublicField(member)) hasPublicFields = true;
            }
            var isDataStructure = hasPublicProperties || hasPublicFields;
            var isObject = hasMethods;
            if (isDataStructure && isObject && hasNotOnlyConstOrReadonlyFields) return ClassType.Hybrid;
            if (isObject) return ClassType.Object;
            if (isDataStructure) return ClassType.DataStructure;
            return ClassType.Other;
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

        private bool IsAMethods(ISymbol member)
        {
            if (member is IMethodSymbol)
            {
                var methodMember = member as IMethodSymbol;
                if (methodMember.MethodKind == MethodKind.Constructor) return false;
                if (methodMember.MethodKind == MethodKind.SharedConstructor) return false;
                if (methodMember.MethodKind == MethodKind.PropertyGet) return false;
                if (methodMember.MethodKind == MethodKind.PropertySet) return false;
                if (methodMember.MethodKind == MethodKind.Destructor) return false;
                if (methodMember.MethodKind == MethodKind.StaticConstructor) return false;
                return true;
            }
            return false;
        }

        private bool IsAPublicProperty(ISymbol member)
        {
            if (member is IPropertySymbol)
            {
                var propertyMember = member as IPropertySymbol;
                var accessability = propertyMember.DeclaredAccessibility;
                if (accessability.Equals(Accessibility.Public)) return true;
            }
            return false;
        }

        private bool IsNotAReadonlyFieldOrAConstant (ISymbol member) {
            if (member is IFieldSymbol)
            {
                var fieldMember = member as IFieldSymbol;
                if (!fieldMember.IsConst) return true;
                if (!fieldMember.IsReadOnly) return true;
            }
            return false;
        }

        private bool IsAPublicField(ISymbol member)
        {
            if (member is IFieldSymbol)
            {
                var fieldMember = member as IFieldSymbol;
                var accessability = fieldMember.DeclaredAccessibility;
                if (accessability.Equals(Accessibility.Public)) return true;
            }
            return false;
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
            var methodInvocations = new List<ExpressionSyntax>();
            methodInvocations.AddRange(_documentWalker.GetNodesFromDocument<InvocationExpressionSyntax>(document));
            methodInvocations.AddRange(_documentWalker.GetNodesFromDocument<MemberAccessExpressionSyntax>(document));
            var violations = new List<ExpressionSyntax>();

            foreach (var inv in methodInvocations)
            {
                if (semanticModel.GetSymbolInfo(inv).Symbol == null) continue;
                if (IsInvocationOfDataStructure(inv, semanticModel)) continue;
                if (IsInvocationOfContainingType(inv, semanticModel)) continue;
                if (IsInvocationOfContainingMethodsParameters(inv, semanticModel)) continue;
                if (IsInvocationOfContainingTypesMembers(inv, semanticModel)) continue;
                if (IsInvocationOfInMethodCreatedObject(inv, semanticModel)) continue;
                if (IsStaticInvocation(inv, semanticModel)) continue;
                if (IsExtensionMethodInvocation(inv, semanticModel)) continue;
                violations.Add(inv);
            }
            return _documentWalker.CreateRecommendations(document, violations, RecommendationType.LODViolation);
        }

        private bool IsInvocationOfDataStructure(ExpressionSyntax invocation, SemanticModel model)
        {
            var symbol = model.GetSymbolInfo(invocation).Symbol;
            var containingType = symbol.ContainingType;
            if (containingType == null) return false; //TODO corrent?
            return DetermineClassType(containingType) == ClassType.DataStructure;
        }
        
        private bool IsInvocationOfContainingType(ExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (invocationSymbol == null) return true;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            var methodDeclarations = containingType.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var symbolToEvaluate = invocationSymbol.IsGenericMethod ? invocationSymbol.ConstructedFrom : invocationSymbol;
            return methodDeclarations
                    .Select(declaration => model.GetDeclaredSymbol(declaration))
                    .Any(declarationSymbol => symbolToEvaluate.Equals(declarationSymbol));
        }

        private bool IsInvocationOfContainingMethodsParameters(ExpressionSyntax invocation,
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

        private bool IsInvocationOfContainingTypesMembers(ExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            if (containingType == null)
                return false;
            var members = containingType.Members;
            return _documentWalker.IsSymbolInvocationOfNodes(members, invocationSymbol, model);
        }

        private bool IsInvocationOfInMethodCreatedObject(ExpressionSyntax invocation, SemanticModel model)
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

        private bool IsStaticInvocation(ExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            return invocationSymbol.IsStatic;
        }

        private bool IsExtensionMethodInvocation(ExpressionSyntax invocation, SemanticModel model)
        {
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            return invocationSymbol != null && invocationSymbol.IsExtensionMethod;
        }     
    }
}
 