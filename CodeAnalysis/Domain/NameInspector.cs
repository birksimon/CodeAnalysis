using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeAnalysis.DataClasses;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.Domain
{
    class NameInspector
    {
        private readonly DocumentWalker _documentWalker = new DocumentWalker();
        public IEnumerable<OptimizationRecomendation> AnalyzeSolution(Solution solution)
        {
            return 
                from project in solution.Projects
                from document in project.Documents
                let numberSeries = GetNamesConsistingOfNumberSeries(document).ToList()
                where numberSeries.Any()
                select _documentWalker.CreateRecommendations(document, numberSeries, RecommendationType.VariableNameIsNumberSeries);
        }

        private IEnumerable<VariableDeclaratorSyntax> GetNamesConsistingOfNumberSeries(Document document)
        {
            const string numberSeriesRegex = "^[a-zA-Z][0-9]{1,3}$";
            var variableDeclarations = _documentWalker.GetNodesFromDocument<VariableDeclarationSyntax>(document);
            var nsNames = new List<VariableDeclaratorSyntax>();

            foreach (var declaration in variableDeclarations)
            {
                nsNames.AddRange(from variable in declaration.Variables
                                 where Regex.IsMatch(variable.Identifier.Value.ToString(), numberSeriesRegex)
                                 select variable);
            }

            return nsNames;
        }
    }
}