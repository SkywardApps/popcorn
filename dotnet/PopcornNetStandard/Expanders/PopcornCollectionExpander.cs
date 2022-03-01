using Skyward.Popcorn.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skyward.Popcorn.Expanders
{
#nullable enable
    public class PopcornCollectionExpander : IPopcornExpander
    {
        public bool ShouldApplyIncludes { get => false; }

        public bool WillHandle(Type sourceType, object instance, IPopcorn popcorn)
        {
            // figure out if this is an expandable list, instead
            // Is this a list of items we need to project?
            if (sourceType.GetTypeInfo().GetInterfaces()
                .Any(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return true;
            }

            return false;
        }

        public object Expand(Type sourceType, object originalValue, IReadOnlyList<PropertyReference> includes, IPopcorn popcorn)
        {
            var interfaceType = originalValue.GetType().GetTypeInfo().GetInterfaces()
                    .First(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            // Verify that the generic parameter is something we would expand
            var genericType = interfaceType.GenericTypeArguments[0];

            // Construct a list of dictionaries
            var instantiatedDestinationType = new List<object>();

            // try to assign the data item by item
            foreach (var item in (IEnumerable)originalValue)
            {
                /// If authorization indicates this should not in fact be authorized, skip it
                if (!popcorn.AuthorizeValue(originalValue, "", item))
                {
                    continue;
                }

                if (item == null)
                {
                    // Just assign the null
                    instantiatedDestinationType.Add(item);
                    continue;
                }

                var expandedItem = popcorn.Expand(genericType, item, includes);
                if (expandedItem != null)
                {
                    instantiatedDestinationType.Add(expandedItem);
                }
            }

            return instantiatedDestinationType;
        }

    }
}
