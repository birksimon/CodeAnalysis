using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysis.DataClasses
{
    class ClassCouplingMetrics
    {
        public string Class { get; set; }
        public int TotalAmountCalls { get; set; }
        public int TotalInternCalls { get; set; }
        public int TotalExternCalls { get; set; }
        private readonly Dictionary<string, int> _externCalls = new Dictionary<string, int>();

        public ClassCouplingMetrics(BaseTypeDeclarationSyntax type)
        {
            Class = type.Identifier.Text;
        }

        public void AddExternCall(string calledNamespace)
        {
            if (_externCalls.ContainsKey(calledNamespace))
            {
                _externCalls[calledNamespace] ++;
            }
            else
            {
                _externCalls.Add(calledNamespace, 1);
            }
        }

        public List<KeyValuePair<string, int>> GetExternCalls()
        {
            return _externCalls.ToList();
        }
    }
}