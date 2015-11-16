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
                    var candidate = declaration.ChildTokens().First(t => t.RawKind == IdentifierToken);
                    IdentifierNameSyntax identifierNameSyntax;
                    var symbolInfo = semanticModel.GetDeclaredSymbol(declaration);
                    if (symbolInfo == null) continue;
                    if (symbolInfo.TypeKind == TypeKind.Interface) continue;

                    if (baseTypes.ContainsKey(tree))
                    {
                        baseTypes[tree].Add(symbolInfo);
                    }
                    else
                    {
                        baseTypes.Add(tree, new List<INamedTypeSymbol>(new[] { symbolInfo }));
                    }
                }
            }
            return baseTypes;
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
                        //var baseType = entry.DescendantTokens().First(t => t.RawKind == IdentifierToken);
                        var baseType = entry.DescendantNodes().OfType<IdentifierNameSyntax>().First();
                        var derivee = list.Parent.ChildTokens().First(t => t.RawKind == IdentifierToken);

                      // TypeDeclarationSyntax typeDeclarationSyntax;
                      //  if (_documentWalker.TryGetContainingNodeOfType<TypeDeclarationSyntax>(baseType, out typeDeclarationSyntax)) 
                      //  {
                            var symbolInfo = semanticModel.GetSymbolInfo(baseType).Symbol as INamedTypeSymbol;
                            if (symbolInfo == null) continue;
                            if (symbolInfo.TypeKind == TypeKind.Interface) continue;


                            if (derivees.ContainsKey(symbolInfo))
                            {
                                derivees[symbolInfo].Add(derivee);
                            }
                            else
                            {
                                derivees.Add(symbolInfo, new List<SyntaxToken>(new[] { derivee }));
                            }
                       // }
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
/*
internal class InheritanceInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private Dictionary<IdentifierNameSyntax, InheritanceDataHolder> _baseAndDerivedTypes
            = new Dictionary<IdentifierNameSyntax, InheritanceDataHolder>();

        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            FindAllBaseAndDerivedTypes(solution);
            //Find Dependencies -> Instanziierung, zugriff auf Static Attribute/Methoden
            var blub = FindDependenciesFromDerivedTypes();
            return new List<OptimizationRecomendation>();

        }

        private void FindAllBaseAndDerivedTypes(Solution solution)
        {
            var docs = _documentWalker.GetAllDocumentsFromSolution(solution);
            foreach (var doc in docs)
            {
                var semanticModel = doc.GetSemanticModelAsync().Result;
                var baseTypeCandidates = _documentWalker.GetNodesFromDocument<BaseTypeSyntax>(doc).ToList();
                if (!baseTypeCandidates.Any()) continue;
                List<IdentifierNameSyntax> identifiersOfCandidates = baseTypeCandidates.Select
                    (type => type.DescendantNodes().OfType<IdentifierNameSyntax>().First()).ToList();
                var baseTypes = ValidateFoundTypes(identifiersOfCandidates, doc);
                var derivedType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(baseTypeCandidates.First());
                StoreInDictionary(baseTypes, derivedType, semanticModel);
            }
        }

        private Dictionary<IdentifierNameSyntax, InheritanceDataHolder> FindDependenciesFromDerivedTypes()
        {
            var matches = new Dictionary<IdentifierNameSyntax, InheritanceDataHolder>();
            foreach (var baseType in _baseAndDerivedTypes.Keys)
            {
                var semanticModel = _baseAndDerivedTypes[baseType].Model;
                var objectCreations = baseType.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                var objectIdentifiers = objectCreations.Select(creation => creation.DescendantNodes().OfType<IdentifierNameSyntax>().First()).ToList();

                var derivees = _baseAndDerivedTypes[baseType].Derivees;
                
                foreach (var identifier in objectIdentifiers)
                {
                    var creationSymbol = semanticModel.GetSymbolInfo(identifier);
                    foreach (var derivee in derivees)
                    {
                        var deriveeSymbol = semanticModel.GetSymbolInfo(derivee);
                        if (deriveeSymbol.Equals(creationSymbol))
                            matches.Add(baseType, _baseAndDerivedTypes[baseType]);
                    }
                }
            }
            return matches;
        }

        private IEnumerable<IdentifierNameSyntax> ValidateFoundTypes(IEnumerable<IdentifierNameSyntax> identifiers, Document doc)
        {
            var semanticModel = doc.GetSemanticModelAsync().Result;




            return from identifier in identifiers
                   let typeSymbol = semanticModel.GetSymbolInfo(identifier).Symbol as INamedTypeSymbol
                   where typeSymbol != null where typeSymbol.TypeKind != TypeKind.Interface
                   select identifier;
        }

        private void StoreInDictionary(IEnumerable<IdentifierNameSyntax> baseTypes, TypeDeclarationSyntax derivedType, SemanticModel model)
        {
            foreach (var type in baseTypes)
            {
                if (_baseAndDerivedTypes.ContainsKey(type))
                {
                    _baseAndDerivedTypes[type].AddDerivee(derivedType);
                }
                else
                {
                    var holder = new InheritanceDataHolder {Model = model};
                    holder.AddDerivee(derivedType);
                    _baseAndDerivedTypes.Add(type, holder);   
                }
            }
        }

    }
}
*/