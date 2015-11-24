using System;

namespace CodeAnalysis.Domain.Exceptions
{
    class InvocationIsAnonymousException : Exception
    {
        public InvocationIsAnonymousException(string message) : base(message){ }
        public InvocationIsAnonymousException(string message, Exception inner) : base(message, inner) { }
    }
}
