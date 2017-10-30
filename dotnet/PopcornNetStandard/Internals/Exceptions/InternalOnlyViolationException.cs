using System;
using System.Collections.Generic;
using System.Text;

namespace Skyward.Popcorn
{
    public class InternalOnlyViolationException : Exception
    {

        public InternalOnlyViolationException()
        {
        }

        public InternalOnlyViolationException(string message) : base(message)
        {
        }

        public InternalOnlyViolationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
