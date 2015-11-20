using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeAnalysis.Domain.Exceptions
{
    class NoNamedTypeException : Exception
    {
        public NoNamedTypeException(string message) : base(message) { }
        public NoNamedTypeException(SyntaxNode node) : base(node.ToString() + " is not a named type.") { }
        public NoNamedTypeException(string message, Exception inner) : base(message, inner) { }
    }
}
