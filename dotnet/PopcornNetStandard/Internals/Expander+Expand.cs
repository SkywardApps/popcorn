﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;
    /*
    /// <summary>
    /// The expansions algorithms
    /// </summary>
    public partial class Expander
    {
        /// <summary>
        /// Query if this is a Dictionary<string, object>
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        protected bool WillExpandDictionary(Type sourceType)
        {
            if (sourceType.IsConstructedGenericType)
                return sourceType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && sourceType.GenericTypeArguments[0] == typeof(string);
            else
                return false;
        }

        /// <summary>
        /// Query if this is a collection of a mapped type
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        protected bool WillExpandCollection(Type sourceType)
        {
            var blindAssignment = BlindHandlers.Where(kv => kv.Key.IsAssignableFrom(sourceType)).Select(kv => kv.Value).FirstOrDefault();
            if (blindAssignment != null)
                return false;

            // figure out if this is an expandable list, instead
            // Is this a list of items we need to project?
            if (sourceType.GetTypeInfo().GetInterfaces()
                .Any(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var interfaceType = sourceType.GetTypeInfo().GetInterfaces()
                    .First(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                // Verify that the generic parameter is something we would expand
                var genericType = interfaceType.GenericTypeArguments[0];
                if (this.WillExpandType(genericType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Query if this is a type that can be expanded with no projected type, i.e. blind
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        protected bool WillExpandBlind(Type sourceType)
        {
            var blindAssignment = BlindHandlers.Where(kv => kv.Key.IsAssignableFrom(sourceType)).Select(kv => kv.Value).FirstOrDefault();
            if (blindAssignment != null)
                return true;

            if (WillExpandCollection(sourceType))
                return false;

            if (!sourceType.GetTypeInfo().IsClass) // False if a simple type
                return false;


            if (!(sourceType.GetTypeInfo().GetInterfaces() // True if the object doesn't have an IEnumerable interface
                .Any(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
                return true;

            return false;
        }

        protected bool WillAssignDirect(Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            if (sourceType == typeof(bool))
                return true;

            switch (Type.GetTypeCode(sourceType))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Test if an object is authorized in a given context, from the given source.
        /// Source may be an object (if value was from a property) or the collection 
        /// (if value is contained within it).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="valueToAssign"></param>
        /// <returns>True if authorized, false if rejected</returns>
        private bool AuthorizeValue(object source, ContextType context, object valueToAssign)
        {
            if (valueToAssign == null)
                return true;

            var sourceType = source.GetType();
            var assignType = valueToAssign.GetType();

            if (Mappings.ContainsKey(assignType))
            {
                foreach (var authorization in Mappings[assignType].Authorizers)
                    if (!authorization(source, context, valueToAssign))
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Take a dictionarhy, and transfer properties requested into a dictionary
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        protected object ExpandBlindDictionary(object source, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            visited = UniqueVisit(source, visited);

            Type sourceType = source.GetType();

            var blindAssignment = BlindHandlers.Where(kv => kv.Key.IsAssignableFrom(sourceType)).Select(kv => kv.Value).FirstOrDefault();
            if (blindAssignment != null)
            {
                return blindAssignment.Item2(source, context);
            }

            var input = (IDictionary)source;
            if (!includes.Any())
            {
                var keys = input.Keys.Cast<string>();
                includes = keys.Select(key => new PropertyReference { PropertyName = key });
            }

            // Attempt to create a projection object we'll map the data into
            var destinationObject = new Dictionary<string, object>();

            PopcornEntityDefinition mappingDefinition = null;

            // Allow any actions to run ahead of mapping
            if (Mappings.ContainsKey(sourceType))
            {
                foreach (var action in Mappings[sourceType].BeforeExpansion)
                    action(destinationObject, source, context);

                mappingDefinition = Mappings[source.GetType()];
            }

            // Iterate over only the requested properties
            foreach (var propertyReference in includes)
            {
                string propertyName = propertyReference.PropertyName;

                if (mappingDefinition != null)
                {
                    var preparers = mappingDefinition.PrepareProperties;

                    // See if there's a propertyPreparer
                    if (preparers.ContainsKey(propertyName))
                    {
                        preparers[propertyName](destinationObject, null, source, context);
                    }
                }

                // Transform the input value as needed
                object valueToAssign = input[propertyName];

                var translators = mappingDefinition?.DefaultDestination()?.Translators;
                // if there's a custom entry for this, it gets first crack
                if (translators != null && translators.ContainsKey(propertyName))
                {
                    valueToAssign = translators[propertyName](source, context);
                }

                /// If authorization indicates this should not in fact be authorized, skip it
                if (!AuthorizeValue(source, context, valueToAssign))
                {
                    continue;
                }

                if (WillExpand(valueToAssign))
                {
                    valueToAssign = Expand(valueToAssign, context, propertyReference.Children, visited);
                }

                if (valueToAssign != null)
                    destinationObject[propertyName] = valueToAssign;
            }

            // Allow any actions to run after the mapping
            /// @Todo should this be in reverse order so we have a nested stack style FILO?
            if (Mappings.ContainsKey(sourceType))
                foreach (var action in Mappings[sourceType].DefaultDestination().AfterExpansion)
                    action(destinationObject, source, context);

            return destinationObject;
        }

        /// <summary>
        /// Take a complex object, and transfer properties requested into a dictionary
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        protected object ExpandBlindObject(object source, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            visited = UniqueVisit(source, visited);

            Type sourceType = source.GetType();

            var blindAssignment = BlindHandlers.Where(kv => kv.Key.IsAssignableFrom(sourceType)).Select(kv => kv.Value).FirstOrDefault();
            if (blindAssignment != null)
            {
                return blindAssignment.Item2(source, context);
            }

            includes = ConstructIncludes(includes, sourceType, null);

            if (!includes.Any())
                return null;

            // Attempt to create a projection object we'll map the data into
            var destinationObject = new Dictionary<string, object>();

            PopcornEntityDefinition mappingDefinition = null;

            // Allow any actions to run ahead of mapping
            if (Mappings.ContainsKey(sourceType))
            {
                foreach (var action in Mappings[sourceType].BeforeExpansion)
                    action(destinationObject, source, context);

                mappingDefinition = Mappings[source.GetType()];
            }

            // Iterate over only the requested properties
            foreach (var propertyReference in includes)
            {
                string propertyName = propertyReference.PropertyName;

                if (mappingDefinition != null)
                {
                    var preparers = mappingDefinition.PrepareProperties;

                    // See if there's a propertyPreparer
                    if (preparers.ContainsKey(propertyName))
                    {
                        preparers[propertyName](destinationObject, null, source, context);
                    }
                }

                // Transform the input value as needed
                object valueToAssign = GetSourceValue(source, context, propertyName, mappingDefinition?.DefaultDestination()?.Translators);

                /// If authorization indicates this should not in fact be authorized, skip it
                if (!AuthorizeValue(source, context, valueToAssign))
                {
                    continue;
                }

                if (WillExpand(valueToAssign))
                {
                    valueToAssign = Expand(valueToAssign, context, propertyReference.Children, visited);
                }

                if(valueToAssign != null)
                    destinationObject[propertyName] = valueToAssign;
            }

            // Allow any actions to run after the mapping
            /// @Todo should this be in reverse order so we have a nested stack style FILO?
            if (Mappings.ContainsKey(sourceType))
                foreach (var action in Mappings[sourceType].DefaultDestination().AfterExpansion)
                    action(destinationObject, source, context);

            return destinationObject;
        }


        /// <summary>
        /// Take a complex object, and transfer properties requested into a dictionary
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        protected Dictionary<string, object> ExpandJObject(JObject source, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            visited = UniqueVisit(source, visited);

            // Attempt to create a projection object we'll map the data into
            var destinationObject = new Dictionary<string, object>();

            var propertyNames = (includes?.Any() ?? false)
                ? includes.Select(pr => pr.PropertyName)
                : ((IEnumerable<KeyValuePair<string, JToken>>)source).Select(kv => kv.Key);

            // Iterate over only the requested properties
            foreach (var propertyName in propertyNames)
            {
                // Transform the input value as needed
                object valueToAssign = source.Value<JToken>(propertyName);

                destinationObject[propertyName] = valueToAssign;
            }

            return destinationObject;
        }


        /// <summary>
        /// Retrieve a value to be assigned to a property on the projection.
        /// This may mean invoking a translator, retrieving a property, or executing a method.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="propertyName"></param>
        /// <param name="translators"></param>
        /// <returns></returns>
        private object GetSourceValue(object source, ContextType context, string propertyName, Dictionary<string, Func<object, ContextType, object>> translators = null)
        {
            object valueToAssign = null;
            Type sourceType = source.GetType();
            var matchingProperty = sourceType.GetTypeInfo().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var matchingMethod = sourceType.GetTypeInfo().GetMethod(propertyName, BindingFlags.Instance | BindingFlags.Public);

            // Check if the value is marked as InternalOnly
            InternalOnlyAttribute[] attributes = new InternalOnlyAttribute[2];
            attributes[0] = matchingProperty?.GetCustomAttribute<InternalOnlyAttribute>();
            attributes[1] = matchingMethod?.GetCustomAttribute<InternalOnlyAttribute>();

            string[] names = new string[2];
            names[0] = matchingProperty?.Name + " property";
            names[1] = matchingMethod?.Name + " method";

            for(int i = 0; i < attributes.Length; i++)
            {
                var internalOnlyAttr = attributes[i];

                if (internalOnlyAttr == null)
                    continue;

                if (internalOnlyAttr.ThrowException)
                    throw new InternalOnlyViolationException(
                        string.Format("Expand: {0} inside {1} class is marked [InternalOnly]", names[i], sourceType.Name));

                return null;
            }

            // if there's a custom entry for this, it gets first crack
            if (translators != null && translators.ContainsKey(propertyName))
            {
                valueToAssign = translators[propertyName](source, context);
            }

            // if there's an existing property on the source, try to blind-assign it
            else if (matchingProperty != null)
            {
                valueToAssign = matchingProperty.GetValue(source);
            }

            // If there's a simple method we can call, invoke it and assign the results
            else if (matchingMethod != null
                && matchingMethod.ReturnType != null
                && matchingMethod.ReturnType != typeof(void))
            {
                var parameterList = matchingMethod.GetParameters();
                if (!parameterList.Any())
                    valueToAssign = matchingMethod.Invoke(source, new object[] { });
                else
                {
                    List<object> parameterArray = new List<object>();
                    foreach (var parameter in parameterList)
                    {
                        var parameterValue = ServiceProvider.GetService(parameter.ParameterType);
                        if (parameterValue != null)
                            parameterArray.Add(parameterValue);
                    }

                    if (parameterArray.Count() == parameterList.Count())
                    {
                        valueToAssign = matchingMethod.Invoke(source, parameterArray.ToArray());
                    }
                    else
                    {
                        // Couldn't map it, but it was explicitly requested, so throw an error
                        throw new InvalidCastException(propertyName);
                    }
                }
            }
            else
            {
                // Couldn't map it, but it was explicitly requested, so throw an error
                throw new InvalidCastException(propertyName);
            }

            return valueToAssign;
        }

        /// <summary>
        /// Verify that we have the appropriate include list for a type, taking into account any requested,
        /// or otherwise defaults supplied.  
        /// </summary>
        /// <param name="includes"></param>
        /// <param name="sourceType"></param>
        /// <param name="destType"></param>
        /// <returns></returns>
        private IEnumerable<PropertyReference> ConstructIncludes(IEnumerable<PropertyReference> includes, Type sourceType, Type destType)
        {
            // Out of the gate we want to first see if the only property to be included is a wildcard
            if ((includes.Count() == 1) && (includes.Any(i => i.PropertyName == "*")))
            {
                var wildCardIncludes = new List<PropertyReference> { };
                var mapDef = new PopcornEngityDefinition();

                // Check to see if the object is to be blind expanded and make the destination the same as the source if it is
                if (destType == null)
                {
                    destType = sourceType;
                }
                else // in the case that the object isn't to be blind expanded get the proper mapping
                {
                    Mappings.TryGetValue(sourceType, out mapDef);
                }

                // Have all of the destination type properties set to be included
                foreach (PropertyInfo info in destType.GetTypeInfo().GetProperties())
                {
                    var matchingSourceProp = sourceType.GetTypeInfo().GetProperty(info.Name);

                    // Make sure that the property isn't marked as InternalOnly on the sourceType
                    // Which is only an issue if they marked the type to throw an error if it's requested
                    if (matchingSourceProp != null)
                    {
                        if (matchingSourceProp.GetCustomAttributes().Any(att => att.GetType() == typeof(InternalOnlyAttribute)))
                        {
                            // Only add the property if it isn't marked InternalOnly
                            continue;
                        }
                    }

                    // Also make sure that the property exists on the destination type in some capacity
                    // This will never be hit by a blindly expanded object as the source and destination type are identical
                    if (matchingSourceProp == null)
                    {
                        try
                        {
                            // Check to see if there are any translators that would apply the object to the projection ultimately
                            var transTest = mapDef.DefaultDestination().Translators[info.Name];
                        }
                        catch (Exception)
                        {
                            // This property isn't known to the projection at all and thus should not be included
                            continue;
                        }
                    }

                    wildCardIncludes.Add(new PropertyReference { PropertyName = info.Name });
                }

                return wildCardIncludes;
            }

            if (includes.Any())
                return includes;

            if (destType == null)
            {
                // in the case of a blind object, default to source properties.  This is a bit dangerous!
                includes = sourceType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes<IncludeByDefault>().Any())
                    .Where(p => !p.GetCustomAttributes<InternalOnlyAttribute>().Any())
                    .Select(p => new PropertyReference() { PropertyName = p.Name });

                // If nothing is marked as include by default, get all properties.
                if (!includes.Any())
                {
                    includes = sourceType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => !p.GetCustomAttributes<InternalOnlyAttribute>().Any())
                        .Select(p => new PropertyReference() { PropertyName = p.Name });
                }
            }

            // if this doesn't have any includes specified, use the default
            if (!includes.Any() && Mappings.ContainsKey(sourceType))
            {
                includes = PropertyReference.Parse(Mappings[sourceType].DestinationForType(destType).DefaultIncludes);
            }

            // if this STILL doesn't have any includes, that means include everything
            if (!includes.Any() && destType != null)
            {
                includes = destType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new PropertyReference() { PropertyName = p.Name });
            }

            return includes;
        }

        /// <summary>
        /// Track each object we visit to make sure we don't end up in an infinite loop
        /// </summary>
        /// <param name="source"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        private static HashSet<int> UniqueVisit(object source, HashSet<int> visited)
        {
            var key = RuntimeHelpers.GetHashCode(source);
            if (visited.Contains(key))
            {
                throw new SelfReferencingLoopException();
            }
            visited = new HashSet<int>(visited);
            visited.Add(key);
            return visited;
        }

        /// <summary>
        /// Create an object, using any factories provided.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <param name="destType"></param>
        /// <returns></returns>
        private object CreateObjectInContext(ContextType context, Type sourceType, Type destType)
        {
            object destinationObject;
            if (Factories.ContainsKey(destType))
                destinationObject = Factories[destType](context);
            else
                destinationObject = destType.CreateDefaultObject();
            return destinationObject;
        }

        /// <summary>
        /// Map a collection of mapped types
        /// </summary>
        /// <param name="originalValue"></param>
        /// <param name="destinationType"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <param name="visited">todo: describe visited parameter on ExpandCollection</param>
        /// <returns></returns>
        protected IList ExpandCollection(object originalValue, Type destinationType, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            visited = UniqueVisit(originalValue, visited);

            var interfaceType = originalValue.GetType().GetTypeInfo().GetInterfaces()
                    .First(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            // Verify that the generic parameter is something we would expand
            var genericType = interfaceType.GenericTypeArguments[0];

            // try to figure out the destination object type
            var destInterfaceType = destinationType.GetTypeInfo().GetInterfaces()
                    .FirstOrDefault(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));


            Type expandedType;
            var blindAssignment = BlindHandlers.Where(kv => kv.Key.IsAssignableFrom(genericType)).Select(kv => kv.Value).FirstOrDefault();

            if (destInterfaceType != null)
                expandedType = destInterfaceType.GenericTypeArguments[0];
            else if (this.Mappings.ContainsKey(genericType))
                expandedType = this.Mappings[genericType].DefaultDestinationType;
            else if (this.ExpandBlindObjects && blindAssignment != null)
                expandedType = blindAssignment.Item1;
            else
                expandedType = typeof(Dictionary<string, object>);

            // Ok, now we need to try and instantiate the destination type (if it is concrete) or take a guess 
            // at a concrete type if it is an interface

            /// @TODO IList is an easy way of being able to add, but not all collections implement IList (like hashset)
            /// really we want to work with ICollection<T>, although for some reason ICollection (non-generic) doesn't have Add
            var instantiatedDestinationType = destinationType.CreateDefaultObject() as IList;
            if (instantiatedDestinationType == null)
            {
                var concreteType = typeof(List<>).MakeGenericType(expandedType);
                instantiatedDestinationType = concreteType.CreateDefaultObject() as IList;
            }

            // try to assign the data item by item
            foreach (var item in (IEnumerable)originalValue)
            {

                /// If authorization indicates this should not in fact be authorized, skip it
                if (!AuthorizeValue(originalValue, context,  item))
                {
                    continue;
                }

                instantiatedDestinationType.Add(this.Expand(item, context, includes, visited, expandedType));
            }

            return instantiatedDestinationType;
        }
        

        /// <summary>
        /// Given a value, attempt to set it to a property on a destination object.  This may involve changing the object,
        /// such as converting an int to a double, or an int to an int?, or expanding an object into its projection
        /// </summary>
        /// <param name="originalValue"></param>
        /// <param name="destinationProperty"></param>
        /// <param name="destinationObject"></param>
        /// <param name="context"></param>
        /// <param name="propertyReference"></param>
        /// <param name="visited">todo: describe visited parameter on SetValueToProperty</param>
        /// <returns></returns>
        private bool SetValueToProperty(object originalValue, PropertyInfo destinationProperty, object destinationObject, ContextType context, PropertyReference propertyReference, HashSet<int> visited)
        {

            // If it is null then just do a direct assignment
            if (originalValue == null)
            {
                destinationProperty.SetValue(destinationObject, null);
                return true;
            }

            // Try to do an assignment including any conversion needed
            if (destinationProperty.TrySetValueHandleConvert(destinationObject, originalValue))
            {
                return true;
            }

            if (WillExpand(originalValue))
            {
                IEnumerable<PropertyReference> propertySubReferences = CreatePropertyReferenceList(destinationProperty, propertyReference);
                var expandedValue = Expand(originalValue, context, propertySubReferences, visited, destinationProperty.PropertyType);
                if (destinationProperty.TrySetValueHandleConvert(destinationObject, expandedValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Create a list of property references based on the request or defaults as declared by a property attribute.
        /// </summary>
        /// <param name="destinationProperty"></param>
        /// <param name="propertyReference"></param>
        /// <returns></returns>
        private static IEnumerable<PropertyReference> CreatePropertyReferenceList(PropertyInfo destinationProperty, PropertyReference propertyReference)
        {
            // If the requestor didn't define any fields to include, check if the property has any default fields
            IEnumerable<PropertyReference> propertySubReferences = propertyReference.Children;
            if (!propertySubReferences.Any())
            {
                // check if there's a property attribute
                var includes = destinationProperty.GetCustomAttribute<SubPropertyIncludeByDefault>();
                if (includes != null)
                {
                    propertySubReferences = PropertyReference.Parse(includes.Includes);
                }
            }

            return propertySubReferences;
        }
    }*/
}
