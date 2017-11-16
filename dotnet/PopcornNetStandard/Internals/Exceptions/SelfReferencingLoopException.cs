using System;

namespace Skyward.Popcorn
{
    public class SelfReferencingLoopException : Exception
    {
        public SelfReferencingLoopException()
        {
        }

        public SelfReferencingLoopException(string message) : base(message)
        {
        }

        public SelfReferencingLoopException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}