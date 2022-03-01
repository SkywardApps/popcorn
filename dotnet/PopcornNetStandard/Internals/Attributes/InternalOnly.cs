using System;
using System.Collections.Generic;
using System.Text;

namespace Skyward.Popcorn
{
#nullable enable
    /// <summary>
    /// This attribute is used to mark methods, classes, or properties as unable to be passed to any client by Popcorn
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method |AttributeTargets.Property,AllowMultiple = false, Inherited = true )]
    public class InternalOnly : Attribute
    {
        private readonly bool _throwException;

        public InternalOnly(bool throwException = true)
        {
            this._throwException = throwException;
        }

        public bool ThrowException { get { return _throwException; } }

    }

}
