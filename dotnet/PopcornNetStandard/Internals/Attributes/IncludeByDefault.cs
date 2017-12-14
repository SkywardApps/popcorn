using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn
{
    /// <summary>
    /// This attribute is used to mark properties to be included by default at the main entity level
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeByDefault : Attribute
    {
    }
}
