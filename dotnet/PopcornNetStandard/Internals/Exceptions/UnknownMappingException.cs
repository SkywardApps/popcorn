using System;

namespace Skyward.Popcorn
{
    public class UnknownMappingException : Exception
    {
        public string PropertyName { get; set; }

        public UnknownMappingException()
        {
        }

        public UnknownMappingException(string name) : base($"Unrecognized property reference: {name}")
        {
            PropertyName = name;
        }

        public UnknownMappingException(string name, Exception innerException) : base($"Unrecognized property reference: {name}", innerException)
        {
            PropertyName = name;
        }
    }
}