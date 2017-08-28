using System;

namespace Skyward.Popcorn
{
    public class DefaultIncludesAttribute : Attribute
    {
        public DefaultIncludesAttribute(string defaultIncludes)
        {
            Includes = defaultIncludes;
        }

        public String Includes { get; set; }
    }
}