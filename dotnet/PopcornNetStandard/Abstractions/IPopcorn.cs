using Skyward.Popcorn;
using System;
using System.Collections.Generic;

namespace Skyward.Popcorn.Abstractions
{
    #nullable enable
    public interface IPopcorn 
    {
        bool AuthorizeValue(object originalValue, string propertyName, object? valueToAssign);
        object? GetSourceValue(object source, string propertyName);
        object? Expand(Type sourceType, object? instance, IReadOnlyList<PropertyReference>? includes);
    }
}
