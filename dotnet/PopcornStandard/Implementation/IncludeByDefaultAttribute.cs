using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeByDefault : Attribute
    {
        private bool includeByDefault = true;
    }
}
