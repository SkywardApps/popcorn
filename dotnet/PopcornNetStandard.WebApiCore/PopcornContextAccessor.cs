using System;
using System.Collections.Generic;

namespace Skyward.Popcorn
{
    public interface IPopcornContextAccessor
    {
        IEnumerable<PropertyReference> PropertyReferences { get; set; }
    }

    public class PopcornContextAccessor : IPopcornContextAccessor
    {
        public IEnumerable<PropertyReference> PropertyReferences { get; set; }
    }
}