using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    internal class InheritanceDataHolder
    {
        public SyntaxTree SyntaxTree { get; set; }
        public List<SyntaxToken> Derivees { get; set; }
        public INamedTypeSymbol BaseType { get; set; }
        
    }


    internal class InheritanceInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int IdentifierToken = 8508;
        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution).ToList();
            var candidates = FindBaseTypeIdentifiersCandidates(documents);
            var derivees = FindAllDerivees(documents);
            var baseTypeAndDerivees = Merge(candidates, derivees).ToList();

            return null;
        }

        private Dictionary<SyntaxTree, List<INamedTypeSymbol>> FindBaseTypeIdentifiersCandidates(IEnumerable<Document> documents)
        {
            var baseTypes = new Dictionary<SyntaxTree, List<INamedTypeSymbol>> ();
            foreach (var document in documents)
            {
                var tree = document.GetSyntaxTreeAsync().Result;
                var typeDeclarations = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>();
                var semanticModel = document.GetSemanticModelAsync().Result;
                foreach (var declaration in typeDeclarations)
                {
                    var symbolInfo = semanticModel.GetDeclaredSymbol(declaration);
                    if (symbolInfo == null) continue;
                    if (symbolInfo.TypeKind == TypeKind.Interface) continue;
                    AddToDictionaryList(baseTypes, tree, symbolInfo);
                }
            }
            return baseTypes;
        }
        
        private Dictionary<TKey, List<TListValue>> AddToDictionaryList<TKey, TListValue> (
            Dictionary<TKey, List<TListValue>> dict, TKey key, TListValue listValue)
        {
            if (dict.ContainsKey(key))
            {
                dict[key].Add(listValue);
            }
            else
            {
                dict.Add(key, new List<TListValue>(new [] { listValue }));
            }
            return null;
        }

        private Dictionary<INamedTypeSymbol, List<SyntaxToken>> FindAllDerivees(IEnumerable<Document> documents)
        {
            var derivees = new Dictionary<INamedTypeSymbol, List<SyntaxToken>>();
            foreach (var document in documents)
            {
                var tree = document.GetSyntaxTreeAsync().Result;
                var baseLists = tree.GetRoot().DescendantNodes().OfType<BaseListSyntax>();
                var semanticModel = document.GetSemanticModelAsync().Result;
                foreach (var list in  baseLists)
                {
                    foreach (var entry in list.Types)
                    {
                        var baseType = entry.DescendantNodes().OfType<IdentifierNameSyntax>().First();
                        var derivee = list.Parent.ChildTokens().First(t => t.RawKind == IdentifierToken);

                        var symbolInfo = semanticModel.GetSymbolInfo(baseType).Symbol as INamedTypeSymbol;
                        if (symbolInfo == null) continue;
                        if (symbolInfo.TypeKind == TypeKind.Interface) continue;
                        AddToDictionaryList(derivees, symbolInfo, derivee);
                    }
                }
            }
            return derivees;
        }

        private IEnumerable<InheritanceDataHolder> Merge(Dictionary<SyntaxTree, List<INamedTypeSymbol>> baseTypeCandidates,
            Dictionary<INamedTypeSymbol, List<SyntaxToken>> derivees)
        {            
            foreach (var baseType in derivees.Keys) // TODO make it inner loop
            {
                foreach (var tree in baseTypeCandidates.Keys)
                {
                    foreach (var candidate in baseTypeCandidates[tree])
                    {
                        // TODO Check Equality Of Semantic Models Symbols
                        if (candidate.Equals(baseType))
                        {
                            yield return new InheritanceDataHolder
                            {
                                BaseType = baseType,
                                SyntaxTree = tree,
                                Derivees = derivees[baseType]
                            };
                        }
                    }
                }
            }    
        }
    }
}