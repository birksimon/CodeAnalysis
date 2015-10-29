using System.Collections.Generic;
using CodeAnalysis.DataClasses;
using Microsoft.CodeAnalysis;

namespace CodeAnalysis.Domain
{
    interface ICodeAnalyzer
    {
        IEnumerable<OptimizationRecomendation> Analyze(Solution solution);
    }
}
