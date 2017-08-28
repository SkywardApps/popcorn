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
                if (this.WillExpandDirect(genericType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Expand a mapped type
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        protected object ExpandDirectObject(object source, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            var key = RuntimeHelpers.GetHashCode(source);
            if (visited.Contains(key))
            {
                throw new SelfReferencingLoopException();
            }
            visited = new HashSet<int>(visited);
            visited.Add(key);

            Type sourceType = source.GetType();

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

            // Attempt to create a projection object we'll map the data into
            object destinationObject = CreateObjectInContext(context, sourceType);

            // Allow any actions to run ahead of mapping
            foreach (var action in Mappings[sourceType]._BeforeExpansion)
                action(destinationObject, source, context);

            // Iterate over only the requested properties
            foreach (var propertyReference in includes)
            {
                // Attempt to assign the property
                if (AssignProperty(propertyReference, destinationObject, source, Mappings[sourceType], context, visited))
                    continue;

                // @todo Try to do funky stuff as far as camelcasing!

                // Couldn't map it, but it was explicitly requested, so throw an error
                throw new InvalidCastException(propertyReference.PropertyName);
            }

            // Allow any actions to run after the mapping
            /// @Todo should this be in reverse order so we have a nested stack style FILO?
            foreach (var action in Mappings[sourceType]._AfterExpansion)
                action(destinationObject, source, context);

            return destinationObject;
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
        /// <returns></returns>
        protected IList ExpandCollection(object originalValue, Type destinationType, ContextType context, IEnumerable<PropertyReference> includes, HashSet<int> visited)
        {
            var key = RuntimeHelpers.GetHashCode(originalValue);
            if (visited.Contains(key))
            {
                throw new SelfReferencingLoopException();
            }
            visited = new HashSet<int>(visited);
            visited.Add(key);

            var interfaceType = originalValue.GetType().GetTypeInfo().GetInterfaces()
                    .First(t => t.IsConstructedGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            // Verify that the generic parameter is something we would expand
            var genericType = interfaceType.GenericTypeArguments[0];
            var expandedType = this.Mappings[genericType].DestinationType;

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
                instantiatedDestinationType.Add(this.Expand(item, context, includes, visited));
            }

            return instantiatedDestinationType;
        }

        /// <summary>
        /// Attempt to find and assign a value for a property on the source object.  This may be a direct mapping, indirect, or from a provided translator.
        /// </summary>
        /// <param name="propertyReference"></param>
        /// <param name="destinationObject"></param>
        /// <param name="source"></param>
        /// <param name="mappingDefinition"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool AssignProperty(PropertyReference propertyReference, object destinationObject, object source, MappingDefinition mappingDefinition, ContextType context, HashSet<int> visited)
        {
            string propertyName = propertyReference.PropertyName;
            var translators = mappingDefinition.Translators;
            var preparers = mappingDefinition.PrepareProperties;
            Type destinationType = mappingDefinition.DestinationType;
            Type sourceType = source.GetType();

            // if this property doesn't even exist on the destination object, cry about it
            var destinationProperty = destinationType.GetTypeInfo().GetProperty(propertyName);
            if (destinationProperty == null)
                throw new ArgumentOutOfRangeException(propertyName);

            // See if there's a propertyPreparer
            if (preparers.ContainsKey(propertyName))
            {
                preparers[propertyName](destinationObject, destinationProperty, source, context);
            }

            // if there's a custom entry for this, it gets first crack
            if (translators.ContainsKey(propertyName)
                && SetValueToProperty(translators[propertyName](source, context), destinationProperty, destinationObject, context, propertyReference, visited))
            {
                return true;
            }

            // if there's an existing property on the source, try to blind-assign it
            var matchingProperty = sourceType.GetTypeInfo().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (matchingProperty != null
                && SetValueToProperty(matchingProperty.GetValue(source), destinationProperty, destinationObject, context, propertyReference, visited))
            {
                return true;
            }

            // If there's a simple method we can call, invoke it and assign the results
            var matchingMethod = sourceType.GetTypeInfo().GetMethod(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (matchingMethod != null
                && matchingMethod.ReturnType != null
                && matchingMethod.ReturnType != typeof(void)
                && !matchingMethod.GetParameters().Any()
                && SetValueToProperty(matchingMethod.Invoke(source, new object[] { }), destinationProperty, destinationObject, context, propertyReference, visited))
            {
                return true;
            }

            return false;
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

            // If we can expand the source object into the destination object, do that.
            if (this.WillExpand(originalValue))
            {
                // If the requestor didn't define any fields to include, check if the property has any default fields
                IEnumerable<PropertyReference> propertySubReferences = propertyReference.Children;
                if(!propertySubReferences.Any())
                {
                    // check if there's a property attribute
                    var includes = destinationProperty.GetCustomAttribute<DefaultIncludesAttribute>();
                    if(includes != null)
                    {
                        propertySubReferences = PropertyReference.Parse(includes.Includes);
                    }
                }

                var expandedValue = this.Expand(originalValue, context, propertySubReferences, visited);
                // Lets hope we can indeed assign this now

                if (destinationProperty.TrySetValueHandleConvert(destinationObject, expandedValue))
                {
                    return true;
                }
            }

            // Is this a list of items we need to project, we have to do a bit of extra magic to create a new collection
            if (this.WillExpandCollection(originalValue.GetType()))
            {
                IList instantiatedDestinationType = ExpandCollection(originalValue, destinationProperty.PropertyType, context, propertyReference.Children, visited);
                
                if (instantiatedDestinationType == null)
                {
                    return false;
                }

                // And actually assign it
                if (destinationProperty.TrySetValueHandleConvert(destinationObject, instantiatedDestinationType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
