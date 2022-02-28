using System;
using System.Collections.Generic;

namespace Skyward.Popcorn.Abstractions
{
    public interface IPopcornExpander
    {
        bool WillHandle(Type sourceType, object instance, IPopcorn popcorn);
        object Expand(Type sourceType, object instance, IReadOnlyList<PropertyReference> includes, IPopcorn popcorn);
    }
}
