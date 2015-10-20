using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeAnalysis.DataClasses;
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
                where numberSeries.Count() != 0
                select CreateRecommendations(document, numberSeries);
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

        public OptimizationRecomendation CreateRecommendations(Document document, IEnumerable<VariableDeclaratorSyntax> declarations)
        {
            var warning = "Variable name consists of a number series.";
            var suggestion = "Give it a meaningful name.";
            var recommendation = new OptimizationRecomendation(warning, suggestion);
            var tree = document.GetSyntaxTreeAsync().Result;

            foreach (VariableDeclaratorSyntax declaration in declarations)
            {
                var occurrence = new Occurence()
                {
                    File = document.FilePath,
                    Line = tree.GetLineSpan(declaration.FullSpan).ToString().Split(' ').Last(),
                    CodeFragment = declaration.ToString()
                };
                recommendation.Occurrences.Add(occurrence);
            }

            return recommendation;
        }
    }
}
