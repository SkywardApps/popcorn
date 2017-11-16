using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// This is the definition of how to map one type to another, specified by the generic parameters.  This provides
    /// a fluent api to customize the mapping.
    /// It represents the overall configuration of a source tyoe, as well as the 'default destination' projection configuration.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    /// <typeparam name="TDestType"></typeparam>
    public class MappingDefinitionConfiguration<TSourceType, TDestType> 
        : ProjectionDefinitionConfiguration<TSourceType, TDestType>
    {


        /// <summary>
        /// Add a function-equivalent that is given the opportunity to 'prepare' a property on the source object before it is mapped to the destination.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="memberExpression"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> PrepareProperty<TProperty>(
            Expression<Func<TDestType, TProperty>> memberExpression,
            Action<TDestType, PropertyInfo, TSourceType, ContextType> action)
        {
            var member = (memberExpression.Body as MemberExpression);
            string propertyName = member.Member.Name;
            InternalMappingDefinition.PrepareProperties.Add(propertyName, (destObject, destProp, sourceObject, context) => action((TDestType)destObject, destProp, (TSourceType)sourceObject, context));
            return this;
        }

        /// <summary>
        /// Add a function-equivalent that is given the opportunity to 'prepare' a property on the source object before it is mapped to the destination.
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> PrepareProperty(string propName, Action<object, PropertyInfo, object, ContextType> action)
        {
            InternalMappingDefinition.PrepareProperties.Add(propName, action);
            return this;
        }

        /// <summary>
        /// Add a function-equivalent that is given the opportunity to inspect / change the source object and context before it is mapped
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> BeforeExpansion(Action<object, object, ContextType> action)
        {
            InternalMappingDefinition.BeforeExpansion.Add(action);
            return this;
        }
    }
}
