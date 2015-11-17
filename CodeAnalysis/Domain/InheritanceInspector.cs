using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    internal class InheritanceDataHolder
    {
        public Document Document { get; set; }
        public INamedTypeSymbol Derivee { get; set; }
        public INamedTypeSymbol BaseType { get; set; }
        public SyntaxNode Violation { get; set;}
        public InheritanceDataHolder() {}
        public InheritanceDataHolder(InheritanceDataHolder holder)
        {
            Document = holder.Document;
            Derivee = holder.Derivee;
            BaseType = holder.BaseType;
        }
    }

    internal class InheritanceInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();

        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution).ToList();
            var candidates = FindBaseTypeCandidates(documents);
            var baseTypesAndDerivees = FindAllDerivees(documents, candidates); //TODO Merge into one list
            var dependencyViolations = FindDependencies(baseTypesAndDerivees);

            var formattedData = FormatData(dependencyViolations);
            return formattedData.Keys.Select
                (key => _documentWalker.CreateRecommendations
                    (key, formattedData[key], RecommendationType.InheritanceDependency));
        }

        private Dictionary<INamedTypeSymbol, Document> FindBaseTypeCandidates(IEnumerable<Document> documents)
        {
            var baseTypes = new Dictionary<INamedTypeSymbol, Document>();
            foreach (var document in documents)
            {
                var semanticModel = document.GetSemanticModelAsync().Result;
                var root = document.GetSyntaxRootAsync().Result;
                var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();
                foreach (var declaration in typeDeclarations)
                {
                    var symbolInfo = semanticModel.GetDeclaredSymbol(declaration);
                    if (symbolInfo == null) continue;
                    if (symbolInfo.TypeKind == TypeKind.Interface) continue;
                    if (!baseTypes.ContainsKey(symbolInfo)) baseTypes.Add(symbolInfo, document);
                }
            }
            return baseTypes;
        }

        private IEnumerable<InheritanceDataHolder> FindAllDerivees(IEnumerable<Document> documents,
            Dictionary<INamedTypeSymbol, Document> baseTypeCandidates)
        {
            foreach (var document in documents)
            {
                var semanticModel = document.GetSemanticModelAsync().Result;
                var root = document.GetSyntaxRootAsync().Result;
                var baseLists = root.DescendantNodes().OfType<BaseListSyntax>();
                foreach (var list in baseLists)
                {
                    foreach (var entry in list.Types)
                    {
                        if (!entry.DescendantNodes().OfType<IdentifierNameSyntax>().Any()) continue;
                        var baseType = entry.DescendantNodes().OfType<IdentifierNameSyntax>().First();
                        var baseTypeSymbol = semanticModel.GetSymbolInfo(baseType).Symbol as INamedTypeSymbol;
                        if (baseTypeSymbol == null) continue;
                        if (!baseTypeCandidates.ContainsKey(baseTypeSymbol)) continue;
                        var derivee = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(entry);
                        var deriveeSymbol = semanticModel.GetDeclaredSymbol(derivee);
                        if (baseTypeSymbol.Equals(deriveeSymbol)) continue;
                        yield return
                            new InheritanceDataHolder
                            {
                                Document = baseTypeCandidates[baseTypeSymbol],
                                BaseType = baseTypeSymbol,
                                Derivee = deriveeSymbol
                            };
                    }
                }
            }
        }

        private IEnumerable<InheritanceDataHolder> FindDependencies(IEnumerable<InheritanceDataHolder> baseTypesAndDerivees)
        {
            List<InheritanceDataHolder> dependencyViolations = new List<InheritanceDataHolder>();
            var baseTypesAndDeriveesList = baseTypesAndDerivees.ToList();
            foreach (var entry in baseTypesAndDeriveesList)
            {
                var root = entry.Document.GetSyntaxRootAsync().Result;
                var semanticModel = entry.Document.GetSemanticModelAsync().Result;
                dependencyViolations.AddRange(FindInstantiationsOfSubClasses(entry, root, semanticModel));
                dependencyViolations.AddRange(FindInvocationDependancies(entry, root, semanticModel));
            }            
            return dependencyViolations;
        }

        // TODO Make a generic method for both of them
        private IEnumerable<InheritanceDataHolder> FindInstantiationsOfSubClasses
            (InheritanceDataHolder baseTypeAndDerivees, SyntaxNode root, SemanticModel semanticModel)
        {
            var instantiations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
            foreach (var instantiation in instantiations)
            {
                var instSymbol = semanticModel.GetSymbolInfo(instantiation).Symbol;
                if (instSymbol == null) continue;
                var instBase = instSymbol.ContainingSymbol;
                if (instBase.Equals(baseTypeAndDerivees.Derivee))
                {
                    var holder = new InheritanceDataHolder(baseTypeAndDerivees) {Violation = instantiation};
                    yield return holder;
                }
            }
        }

        private IEnumerable<InheritanceDataHolder> FindInvocationDependancies
            (InheritanceDataHolder baseTypeAndDerivees, SyntaxNode root, SemanticModel semanticModel)
        {
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var instSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
                var instBase = instSymbol.ContainingSymbol;
                if (instBase.Equals(baseTypeAndDerivees.Derivee))
                {
                    var holder = new InheritanceDataHolder(baseTypeAndDerivees) { Violation = invocation };
                    yield return holder;
                }
            }
        }

        private Dictionary<Document, List<SyntaxNode>> FormatData(IEnumerable<InheritanceDataHolder> holder)
        {
            var dict = new Dictionary<Document, List<SyntaxNode>>();
            foreach (var entry in holder)
            {
                if (dict.ContainsKey(entry.Document))
                {
                    dict[entry.Document].Add(entry.Violation);
                }
                else
                {
                    dict.Add(entry.Document, new List<SyntaxNode>(new [] {entry.Violation}));
                }
            }
            return dict;
        }
    }
}