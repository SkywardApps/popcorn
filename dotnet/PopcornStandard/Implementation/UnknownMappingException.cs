using System;

namespace Skyward.Popcorn
{
    internal class UnknownMappingException : Exception
    {
        public UnknownMappingException()
        {
        }

        public UnknownMappingException(string message) : base(message)
        {
        }

        public UnknownMappingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}