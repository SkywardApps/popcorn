using System;

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
