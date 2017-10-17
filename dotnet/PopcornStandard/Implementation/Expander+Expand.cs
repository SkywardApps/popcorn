using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// The expansions algorithms
    /// </summary>
    public partial class Expander
    {
        /// <summary>
        /// Query if this is a mapped type
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        protected bool WillExpandDirect(Type sourceType)
        {
            return (Mappings.ContainsKey(sourceType) && Mappings[sourceType] != null);
        }

        /// <summary>
        /// Query if this is a collection of a mapped type
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        protected bool WillExpandCollection(Type sourceType)
        {
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
            if (!this.ExpandBlindObjects)
                return false;
            return (!WillExpandDirect(sourceType)
                && !WillExpandCollection(sourceType)
                && sourceType.GetTypeInfo().IsClass
                && sourceType != typeof(string) ); // Do we really have to blacklist classes here? 
            // We'll also need to exclude basically anything that is already trivially serializable 
            // Dictionaries and Lists should be excluded (at least for now...)
            // So how do we know where the list of items we should serialize ends, and what we should ignore starts?
        }

        /// <summary>
        /// Expand a mapped type
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <param name="visited">todo: describe visited parameter on ExpandDirectObject</param>
        /// <returns></returns>
        protected object ExpandDirectObject(object source, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            visited = UniqueVisit(source, visited);

            Type sourceType = source.GetType();
            includes = ValidateIncludes(includes, sourceType);

            // Attempt to create a projection object we'll map the data into
            object destinationObject = CreateObjectInContext(context, sourceType);

            // Allow any actions to run ahead of mapping
            foreach (var action in Mappings[sourceType]._BeforeExpansion)
                action(destinationObject, source, context);

            // Iterate over only the requested properties
            foreach (var propertyReference in includes)
            {
                string propertyName = propertyReference.PropertyName;
                var translators = Mappings[sourceType].Translators;
                var preparers = Mappings[sourceType].PrepareProperties;

                // if this property doesn't even exist on the destination object, cry about it
                var destinationProperty = Mappings[sourceType].DestinationType.GetTypeInfo().GetProperty(propertyName);
                if (destinationProperty == null)
                    throw new ArgumentOutOfRangeException(propertyName);

                // See if there's a propertyPreparer
                if (preparers.ContainsKey(propertyName))
                {
                    preparers[propertyName](destinationObject, destinationProperty, source, context);
                }

                // Transform the input value as needed
                var valueToAssign = GetSourceValue(source, context, propertyReference.PropertyName, Mappings[sourceType].Translators);


                /// If authorization indicates this should not in face be authorized, skip it
                if(!AuthorizeValue(source, context, valueToAssign))
                {
                    continue;
                }

                
                // Attempt to assign the property - this will expand the item if needed
                if (!SetValueToProperty(valueToAssign, destinationProperty, destinationObject, context, propertyReference, visited))
                {
                    // Couldn't map it, but it was explicitly requested, so throw an error
                    throw new InvalidCastException(propertyReference.PropertyName);
                }
            }

            // Allow any actions to run after the mapping
            /// @Todo should this be in reverse order so we have a nested stack style FILO?
            foreach (var action in Mappings[sourceType]._AfterExpansion)
                action(destinationObject, source, context);

            return destinationObject;
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
                foreach (var authorization in Mappings[assignType]._Authorizers)
                    if (!authorization(source, context, valueToAssign))
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Take a complex object, and transfer properties requested into a dictionary
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        protected Dictionary<string, object> ExpandBlindObject(object source, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            visited = UniqueVisit(source, visited);

            Type sourceType = source.GetType();
            includes = ValidateIncludes(includes, sourceType);

            // Attempt to create a projection object we'll map the data into
            var destinationObject = new Dictionary<string, object>();

            MappingDefinition mappingDefinition = null;

            // Allow any actions to run ahead of mapping
            if (Mappings.ContainsKey(sourceType))
            {
                foreach (var action in Mappings[sourceType]._BeforeExpansion)
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
                object valueToAssign = GetSourceValue(source, context, propertyName, mappingDefinition?.Translators);
                
                /// If authorization indicates this should not in face be authorized, skip it
                if (!AuthorizeValue(source, context, valueToAssign))
                {
                    continue;
                }

                if (WillExpand(valueToAssign))
                {
                    valueToAssign = Expand(valueToAssign, context, propertyReference.Children, visited);
                }

                destinationObject[propertyName] = valueToAssign;
            }

            // Allow any actions to run after the mapping
            /// @Todo should this be in reverse order so we have a nested stack style FILO?
            if (Mappings.ContainsKey(sourceType))
                foreach (var action in Mappings[sourceType]._AfterExpansion)
                    action(destinationObject, source, context);

            return destinationObject;
        }

        private static object GetSourceValue(object source, ContextType context, string propertyName, Dictionary<string, Func<object, ContextType, object>> translators = null)
        {
            object valueToAssign = null;
            Type sourceType = source.GetType();
            var matchingProperty = sourceType.GetTypeInfo().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var matchingMethod = sourceType.GetTypeInfo().GetMethod(propertyName, BindingFlags.Instance | BindingFlags.Public);

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
                && matchingMethod.ReturnType != typeof(void)
                && !matchingMethod.GetParameters().Any())
            {
                valueToAssign = matchingMethod.Invoke(source, new object[] { });
            }
            else
            {
                // Couldn't map it, but it was explicitly requested, so throw an error
                throw new InvalidCastException(propertyName);
            }

            return valueToAssign;
        }

        private IEnumerable<PropertyReference> ValidateIncludes(IEnumerable<PropertyReference> includes, Type sourceType)
        {
            if (includes.Any())
                return includes;

            if (!Mappings.ContainsKey(sourceType))
            {
                // in the case of a blind object, default to source properties.  This is a bit dangerous!
                includes = sourceType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new PropertyReference() { PropertyName = p.Name });
            }

            // if this doesn't have any includes specified, use the default
            if (!includes.Any())
            {
                includes = PropertyReference.Parse(Mappings[sourceType].DefaultIncludes);
            }

            // if this STILL doesn't have any includes, that means include everything
            if (!includes.Any())
            {
                includes = Mappings[sourceType].DestinationType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new PropertyReference() { PropertyName = p.Name });
            }

            return includes;
        }

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

        private object CreateObjectInContext(ContextType context, Type sourceType)
        {
            object destinationObject;
            if (Factories.ContainsKey(Mappings[sourceType].DestinationType))
                destinationObject = Factories[Mappings[sourceType].DestinationType](context);
            else
                destinationObject = Mappings[sourceType].DestinationType.CreateDefaultObject();
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
            var expandedType = this.Mappings.ContainsKey(genericType) ? this.Mappings[genericType].DestinationType : typeof(Dictionary<string, object>);

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

                /// If authorization indicates this should not in face be authorized, skip it
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
    }
}
