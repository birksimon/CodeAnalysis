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
        public NoNamedTypeException(string node) : base(node + " is not a named type.") { }
        public NoNamedTypeException(string message, Exception inner) : base(message, inner) { }
    }
}
