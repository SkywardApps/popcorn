using System;
using System.Collections.Generic;

namespace Skyward.Popcorn
{
    public interface IPopcornContextAccessor
    {
        IEnumerable<PropertyReference> PropertyReferences { get; set; }
        Type DestinationType { get; set; }
        SortDirection SortDirection { get; set; }
        string SortTarget { get; set; }
    }

    public class PopcornContextAccessor : IPopcornContextAccessor
    {
        public IEnumerable<PropertyReference> PropertyReferences { get; set; }
        public Type DestinationType { get; set; }
        public SortDirection SortDirection { get; set; }
        public string SortTarget { get; set; }
    }
}