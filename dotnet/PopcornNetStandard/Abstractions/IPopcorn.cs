using Skyward.Popcorn;
using System;
using System.Collections.Generic;

namespace Skyward.Popcorn.Abstractions
{
    public interface IPopcorn 
    {
        bool AuthorizeValue(object originalValue, object item);

        object GetSourceValue(object source, string propertyName);
        object Expand(Type sourceType, object instance, IReadOnlyList<PropertyReference> includes);
    }
}
