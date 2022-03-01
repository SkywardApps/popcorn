using Skyward.Popcorn.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable
namespace Skyward.Popcorn.Expanders
{
    #nullable enable
    public class PopcornDictionaryExpander : IPopcornExpander
    {
        public bool ShouldApplyIncludes { get => false; }

        public bool WillHandle(Type sourceType, object instance, IPopcorn popcorn)
        {
            if (sourceType.IsConstructedGenericType)
                return sourceType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && sourceType.GenericTypeArguments[0] == typeof(string);
            
            return false;
        }

        public object Expand(Type sourceType, object source, IReadOnlyList<PropertyReference> includes, IPopcorn popcorn)
        {
            var input = (IDictionary)source;

            // Attempt to create a projection object we'll map the data into
            var destinationObject = new Dictionary<string, object>();

            // Iterate over only the requested properties
            foreach (var propertyReference in includes)
            {
                string propertyName = propertyReference.PropertyName;

                // Transform the input value as needed
                object valueToAssign = input[propertyName];

                /// If authorization indicates this should not in fact be authorized, skip it
                if (!popcorn.AuthorizeValue(source, valueToAssign))
                {
                    continue;
                }

                var expandedValue = popcorn.Expand(valueToAssign?.GetType(), valueToAssign, propertyReference.Children);

                if (expandedValue != null)
                    destinationObject[propertyName] = expandedValue;
            }

            return destinationObject;
        }

    }
}
