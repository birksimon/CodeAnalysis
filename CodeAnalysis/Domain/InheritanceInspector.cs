using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.Enums;
using CodeAnalysis.Output;
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
        
        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution).ToList();
            var candidates = FindBaseTypeCandidates(documents);
            var baseTypesAndDerivees = FindAllDerivees(documents, candidates); 
            var dependencyViolations = FindViolations(baseTypesAndDerivees);

            var formattedData = FormatData(dependencyViolations);

            return formattedData.Select
                (keyValuePair => _documentWalker.CreateRecommendations(keyValuePair.Key, keyValuePair.Value,
                    RecommendationType.InheritanceDependency)).Cast<ICSVPrintable>();
        }

        private Dictionary<Document, List<INamedTypeSymbol>> FindBaseTypeCandidates(IEnumerable<Document> documents)
        {
            var baseTypes = new Dictionary<Document, List<INamedTypeSymbol>>();
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
                    AddToDictionariesList(baseTypes, document, symbolInfo);
                }
            }
            return baseTypes;
        }

        private IEnumerable<InheritanceDataHolder> FindAllDerivees(IEnumerable<Document> documents,
            Dictionary<Document, List<INamedTypeSymbol>> baseTypeCandidates)
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

                        Document doc;
                        if (!DictionaryContainsValueInList(baseTypeCandidates, baseTypeSymbol, out doc)) continue;

                        var derivee = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(entry);
                        var deriveeSymbol = semanticModel.GetDeclaredSymbol(derivee);
                        if (baseTypeSymbol.Equals(deriveeSymbol)) continue;

                        yield return
                            new InheritanceDataHolder
                            {
                                Document = doc,
                                BaseType = baseTypeSymbol,
                                Derivee = deriveeSymbol
                            };
                    }
                }
            }
        }

        private IEnumerable<InheritanceDataHolder> FindViolations(IEnumerable<InheritanceDataHolder> baseTypesAndDerivees)
        {
            List<InheritanceDataHolder> dependencyViolations = new List<InheritanceDataHolder>();
            var baseTypesAndDeriveesList = baseTypesAndDerivees.ToList();
            foreach (var entry in baseTypesAndDeriveesList)
            {
                var root = entry.Document.GetSyntaxRootAsync().Result;
                var semanticModel = entry.Document.GetSemanticModelAsync().Result;
                dependencyViolations.AddRange(FindDependencies<ObjectCreationExpressionSyntax>(entry, root, semanticModel));
                dependencyViolations.AddRange(FindDependencies<InvocationExpressionSyntax>(entry, root, semanticModel));
            }            
            return dependencyViolations;
        }

        private IEnumerable<InheritanceDataHolder> FindDependencies <T>
            (InheritanceDataHolder baseTypeAndDerivees, SyntaxNode root, SemanticModel semanticModel) where T:SyntaxNode
        {
            var classDeclaration = root
                .DescendantNodes()
                .OfType<TypeDeclarationSyntax>().First
                (dec => semanticModel.GetDeclaredSymbol(dec).Equals(baseTypeAndDerivees.BaseType));


            var instantiations = classDeclaration.DescendantNodes().OfType<T>();
            foreach (var instantiation in instantiations)
            {
                var instSymbol = semanticModel.GetSymbolInfo(instantiation).Symbol;
                if (instSymbol == null) continue;
                var instBase = instSymbol.ContainingSymbol;
                if (!instBase.Equals(baseTypeAndDerivees.Derivee)) continue;
                var holder = new InheritanceDataHolder(baseTypeAndDerivees) { Violation = instantiation };
                yield return holder;
            }
        }

        private Dictionary<Document, List<SyntaxNode>> FormatData(IEnumerable<InheritanceDataHolder> holder)
        {
            var dict = new Dictionary<Document, List<SyntaxNode>>();
            foreach (var entry in holder)
            {
                AddToDictionariesList(dict, entry.Document, entry.Violation);
            }
            return dict;
        }

        public void AddToDictionariesList<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key].Add(value);
            }
            else
            {
                dict.Add(key, new List<TValue>(new[] { value }));
            }
        }

        private bool DictionaryContainsValueInList<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TValue valueParam, out TKey keyParam)
        {
            foreach (var list in  dict.Values)
            {
                foreach (var item in list)
                {
                    if (item.Equals(valueParam))
                    {
                        foreach (var key in dict.Keys)
                        {
                            if (dict[key].Equals(list))
                            {
                                keyParam = key;
                                return true;
                            }
                        }
                    }
                }
            }
            keyParam = default(TKey);
            return false;
        }
    }
}