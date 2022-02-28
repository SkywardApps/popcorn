using System;
using System.Runtime.Serialization;

namespace Skyward.Popcorn
{
    [Serializable]
    internal class MultipleDefaultsException : Exception
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

        protected MultipleDefaultsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}