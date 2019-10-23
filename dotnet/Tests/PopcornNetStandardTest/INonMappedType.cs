using System.Collections.Generic;

namespace PopcornNetStandardTest
{
    public interface INonMappedType
    {
        List<ExpanderTests.NonMappedType> Children { get; set; }
        string Name { get; set; }
        string Title { get; set; }
    }
}