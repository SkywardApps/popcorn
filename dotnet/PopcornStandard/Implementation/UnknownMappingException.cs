using System;

namespace Skyward.Popcorn
{
    public class UnknownMappingException : Exception
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