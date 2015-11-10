using System;

namespace CodeAnalysis.Domain.Exceptions
{
    class NodeDoesNotExistException : Exception
    {
        public NodeDoesNotExistException(string message) : base(message){ }
        public NodeDoesNotExistException(string message, Exception inner) : base(message, inner) { }
    }
}
