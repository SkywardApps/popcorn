using System;

namespace Skyward.Popcorn
{
    /// <summary>
    /// This exception is for defaults being declared in the mapping function and at the attribute level on a projection
    /// </summary>
    public class MultipleDefaultsException : Exception
    {
        public MultipleDefaultsException()
        {
        }

        public MultipleDefaultsException(string message) : base(message)
        {
        }

        public MultipleDefaultsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}