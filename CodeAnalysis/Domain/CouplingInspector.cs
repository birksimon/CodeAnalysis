using System.Collections.Generic;
using System.Linq;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Domain.Exceptions;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    class CouplingInspector : ICodeAnalyzer
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        public IEnumerable<ICSVPrintable> Analyze(Solution solution)
        {
            return  AnalyzeCoupling(solution).ToList();
        }

        // TODO Refactor
        private IEnumerable<ClassCouplingMetrics> AnalyzeCoupling(Solution solution)
        {
            var documents = _documentWalker.GetAllDocumentsFromSolution(solution);
            foreach (var document in documents)
            {
                var typeDeclarations = _documentWalker.GetNodesFromDocument<TypeDeclarationSyntax>(document);
                var semanticModel = document.GetSemanticModelAsync().Result;

                foreach (var type in typeDeclarations)
                {
                    var classCouplingMetrics = new ClassCouplingMetrics(type.Identifier.Text);
                    var invocations = type.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    foreach (var invocation in invocations)
                    {
                        classCouplingMetrics.TotalAmountCalls ++;
                        string nameSpace;
                        if (IsInternal(invocation, semanticModel, out nameSpace))
                        {
                            classCouplingMetrics.TotalInternCalls++;
                        }
                        else
                        {
                            classCouplingMetrics.TotalExternCalls++;
                            classCouplingMetrics.AddExternCall(nameSpace);
                        }
                    }
                    yield return classCouplingMetrics;
                }
            }
        }

        private bool IsInternal(SyntaxNode invocation, SemanticModel model, out string nameSpace)
        {
            var containingType = _documentWalker.GetContainingNodeOfType<TypeDeclarationSyntax>(invocation);
            var typeSymbol = model.GetDeclaredSymbol(containingType) as INamedTypeSymbol;
            if (typeSymbol == null) throw new NoNamedTypeException(containingType.ToString());
            nameSpace = typeSymbol.ContainingNamespace.Name;
            var invocationSymbol = model.GetSymbolInfo(invocation).Symbol;
            if (invocationSymbol == null) return true;  // No Symbol -> Anonymous Method -> is Internal
            var members = containingType.Members;
            return _documentWalker.IsSymbolInvocationOfNodes(members, invocationSymbol, model);
        }
    }
}
