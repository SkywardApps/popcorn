using System;

namespace Skyward.Popcorn
{
#nullable enable
    /// <summary>
    /// This attribute is used to mark properties to always be included
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeAlways : Attribute
    {
    }
}
