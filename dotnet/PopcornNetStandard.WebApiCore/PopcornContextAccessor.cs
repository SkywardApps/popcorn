using Skyward.Popcorn.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skyward.Popcorn
{
    public interface IPopcornContextAccessor
    {
        IEnumerable<PropertyReference> PropertyReferences { get; set; }
        IPopcorn Popcorn { get; set; }

        IEnumerable<PropertyReference> ApplyPropertyReferences<T>();
        bool IsPropertyReferenced<T>(string name);
    }

    public class PopcornContextAccessor : IPopcornContextAccessor
    {
        public bool IsPropertyReferenced<T>(string name)
        {
            return Popcorn.DeterminePropertyReferences<T>(PropertyReferences.ToList()).ContainsKey(name);
        }

        public IEnumerable<PropertyReference> ApplyPropertyReferences<T>()
        {
            return Popcorn.DeterminePropertyReferences<T>(PropertyReferences.ToList()).Values;
        }

        public IPopcorn Popcorn { get; set; }
        public IEnumerable<PropertyReference> PropertyReferences { get; set; }
    }
}