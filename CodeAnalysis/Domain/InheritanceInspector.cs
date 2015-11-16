using System;
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
        public Document Document { get; set; }
        //public List<TypeDeclarationSyntax> Derivees { get; set; }
        public INamedTypeSymbol Derivee { get; set; }
        public INamedTypeSymbol BaseType { get; set; }
        
    }


    internal class InheritanceInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        private const int IdentifierToken = 8508;
        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution).ToList();
            var candidates = FindBaseTypeCandidates(documents);
            var derivees = FindAllDerivees(documents, candidates);  //TODO Merge into one list
            //var baseTypeAndDerivees = Merge(candidates, derivees).ToList();
            FindDependencies(derivees);

            return null;
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
                    baseTypes.Add(symbolInfo, document);
                }
            }
            return baseTypes;
        }

        private IEnumerable<InheritanceDataHolder> FindAllDerivees(IEnumerable<Document> documents, Dictionary<INamedTypeSymbol, Document> baseTypeCandidates)
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
                        var baseType = entry.DescendantNodes().OfType<IdentifierNameSyntax>().First();
                        var baseTypeSymbol = semanticModel.GetSymbolInfo(baseType).Symbol as INamedTypeSymbol;

                        if (baseTypeSymbol == null) continue;
                        if (!baseTypeCandidates.ContainsKey(baseTypeSymbol)) continue;

                        var derivee = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(entry);
                        var deriveeSymbol = semanticModel.GetDeclaredSymbol(derivee);
                        yield return new InheritanceDataHolder {Document = baseTypeCandidates[baseTypeSymbol], BaseType = baseTypeSymbol, Derivee = deriveeSymbol};
                    }
                }
            }
        }

        /*
        private Dictionary<Document, List<INamedTypeSymbol>> FindBaseTypeIdentifiersCandidates(IEnumerable<Document> documents)
        {
            var baseTypes = new Dictionary<Document, List<INamedTypeSymbol>> ();
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
                    AddToDictionaryList(baseTypes, document, symbolInfo);
                }
            }
            return baseTypes;
        }

       private Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> FindAllDerivees(IEnumerable<Document> documents)
        {
            var derivees = new Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>>();
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
                        var derivee = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(entry);
                        //var derivee = list.Parent.ChildTokens().First(t => t.RawKind == IdentifierToken);

                        var symbolInfo = semanticModel.GetSymbolInfo(baseType).Symbol as INamedTypeSymbol;
                        if (symbolInfo == null) continue;
                        if (symbolInfo.TypeKind == TypeKind.Interface) continue;
                        AddToDictionaryList(derivees, symbolInfo, derivee);
                    }
                }
            }
            return derivees;
        }

        private IEnumerable<InheritanceDataHolder> Merge(Dictionary<Document, List<INamedTypeSymbol>> baseTypeCandidates,
            Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> derivees)
        {
            return from baseType in derivees.Keys
                from document in baseTypeCandidates.Keys
                from candidate in baseTypeCandidates[document]
                where candidate.Equals(baseType)
                select new InheritanceDataHolder
                {BaseType = baseType, Document = document, Derivees = derivees[baseType]};
        }
        */
        private void FindDependencies(IEnumerable<InheritanceDataHolder> baseTypesAndDerivees)
        {
            foreach (var entry in baseTypesAndDerivees)
            {
                var root = entry.Document.GetSyntaxRootAsync().Result;
                var semanticModel = entry.Document.GetSemanticModelAsync().Result;
                var instantiations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

                foreach (var instantiation in instantiations)
                {
                    var instSymbol = semanticModel.GetSymbolInfo(instantiation).Symbol;

                    var instBase = instSymbol.ContainingSymbol;

                    if (instBase.Equals(entry.Derivee))
                    {
                        Console.WriteLine("askldfhansejkfn");
                    }

                }
            }
        }

        private bool IsSymbolInvocationOfNodes(SyntaxNode node, ISymbol invocationSymbol, SemanticModel model)
        {
            return
                (from identifier in node.DescendantNodes().Where((
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

        private void AddToDictionaryList<TKey, TListValue>(
            Dictionary<TKey, List<TListValue>> dict, TKey key, TListValue listValue)
        {
            if (dict.ContainsKey(key))
            {
                dict[key].Add(listValue);
            }
            else
            {
                dict.Add(key, new List<TListValue>(new[] { listValue }));
            }
        }
    }
}