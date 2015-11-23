using System.Collections.Generic;
using CodeAnalysis.Output;
using Microsoft.CodeAnalysis;

namespace CodeAnalysis.Domain
{
    interface ICodeAnalyzer
    {
        IEnumerable<ICSVPrintable> Analyze(Solution solution);
    }
}
