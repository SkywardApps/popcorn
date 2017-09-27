using System;

namespace Skyward.Popcorn
{
    /// <summary>
    /// This attribute is used to mark properties of a subordinate entity to be included by default
    /// Applying this attribute will overwrite any default includes at the main entity projection level
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SubPropertyIncludeByDefault : Attribute
    {
        public SubPropertyIncludeByDefault(string subPropertyDefaultIncludes)
        {
            Includes = subPropertyDefaultIncludes;
        }

        public String Includes { get; set; }
    }
}