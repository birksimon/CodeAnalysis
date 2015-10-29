using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeAnalysis.DataClasses;
using Microsoft.CodeAnalysis;

namespace CodeAnalysis.Domain
{
    class LawOfDemeterVerifyer : ICodeAnalyzer
    {
        public IEnumerable<OptimizationRecomendation> Analyze(Solution solution)
        {
            throw new NotImplementedException();
        }
    }
}
