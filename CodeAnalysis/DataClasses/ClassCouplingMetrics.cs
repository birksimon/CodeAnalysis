using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeAnalysis.Output;

namespace CodeAnalysis.DataClasses
{
    class ClassCouplingMetrics : ICSVPrintable
    {
        public string Class { get; set; }
        public int TotalAmountCalls { get; set; }
        public int TotalInternCalls { get; set; }
        public int TotalExternCalls { get; set; }
        private readonly Dictionary<string, int> _externCalls = new Dictionary<string, int>();

        public ClassCouplingMetrics(string type)
        {
            Class = type;
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
        public string GetCSVHeader()
        {
            return "Class;Calls Total;Internal Calls;External Calls\n";
        }
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Class);
        }
        public string GetFileName()
        {
            return "/ClassCoupling.csv";
        }
        public string GetCSVString()
        {
            var builder = new StringBuilder();
            builder.Append(Class).Append(PrintConstants.Semicolon);
            builder.Append(TotalAmountCalls).Append(PrintConstants.Semicolon);
            builder.Append(TotalInternCalls).Append(PrintConstants.Semicolon);
            builder.Append(TotalExternCalls).Append(Environment.NewLine);
            return builder.ToString();
        }
    }
}