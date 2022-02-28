﻿using Skyward.Popcorn.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Skyward.Popcorn.Expanders
{
    public class PopcornBlindExpander : IPopcornExpander
    {
        public bool WillHandle(Type sourceType, object instance, IPopcorn popcorn)
        {
            if (!sourceType.GetTypeInfo().IsClass) // False if a simple type
                return false;

            return true;
        }

        public object Expand(Type sourceType, object source, IReadOnlyList<PropertyReference> includes, IPopcorn popcorn)
        {
            // Attempt to create a projection object we'll map the data into
            var destinationObject = new Dictionary<string, object>();

            // Iterate over only the requested properties
            foreach (var propertyReference in includes)
            {
                string propertyName = propertyReference.PropertyName;

                // Transform the input value as needed
                object valueToAssign = popcorn.GetSourceValue(source, propertyName);

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