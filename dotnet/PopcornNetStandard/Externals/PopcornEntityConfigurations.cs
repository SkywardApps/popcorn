using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Skyward.Popcorn
{
    /*
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// This is the definition of how to map one type to another, specified by the generic parameters.  This provides
    /// a fluent api to customize the mapping.
    /// It represents the overall configuration of a source type and modifiers on how to 'expand' it.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    public class PopcornEntityConfigurations<TSourceType> 
        : ProjectionDefinitionConfiguration<TSourceType, object>
    {
        /// <summary>
        /// Add a function-equivalent that is given the opportunity to 'prepare' a property on the source object before it is mapped to the destination.
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public PopcornEntityConfigurations<TSourceType> PrepareProperty(string propName, Action<object, PropertyInfo, object, ContextType> action)
        {
            InternalMappingDefinition.PrepareProperties.Add(propName, action);
            return this;
        }

        /// <summary>
        /// Add a function-equivalent that is given the opportunity to inspect / change the source object and context before it is mapped
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public PopcornEntityConfigurations<TSourceType> BeforeExpansion(Action<object, object, ContextType> action)
        {
            InternalMappingDefinition.BeforeExpansion.Add(action);
            return this;
        }
    }*/
}
