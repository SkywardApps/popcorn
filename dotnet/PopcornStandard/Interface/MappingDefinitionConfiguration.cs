using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// This is the definition of how to map one type to another, specified by the generic parameters.  This provides
    /// a fluent api to customize the mapping.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    /// <typeparam name="TDestType"></typeparam>
    public class MappingDefinitionConfiguration<TSourceType, TDestType>
    {
        internal MappingDefinition InternalDefinition { get; set; }
        /// <summary>
        /// Add a translation for a specific property on the destination type.
        /// Provide a function-equivalent that takes the source object and the context and returns data of the property type.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="memberExpression"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> Translate<TProperty>(
            Expression<Func<TDestType, TProperty>> memberExpression,
            Func<TSourceType, ContextType, TProperty> func)
        {
            var member = (memberExpression.Body as MemberExpression);
            string propertyName = member.Member.Name;
            return TranslateByName(propertyName, (source, context) => func((TSourceType)source, context));
        }

        /// <summary>
        /// Add a translation for a specific property on the destination type.
        /// Provide a function-equivalent that takes the context and returns data of the property type.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="memberExpression"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> Translate<TProperty>(
            Expression<Func<TDestType, TProperty>> memberExpression,
            Func<ContextType, TProperty> func)
        {
            var member = (memberExpression.Body as MemberExpression);
            string propertyName = member.Member.Name;
            return TranslateByName(propertyName, (source, context) => func(context));
        }

        /// <summary>
        /// Add a translation for a specific property on the destination type.
        /// Provide a function-equivalent that takes the source object and returns data of the property type.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="memberExpression"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> Translate<TProperty>(
            Expression<Func<TDestType, TProperty>> memberExpression,
            Func<TSourceType, TProperty> func)
        {
            var member = (memberExpression.Body as MemberExpression);
            string propertyName = member.Member.Name;
            return TranslateByName(propertyName, (source, context) => func((TSourceType)source));
        }

        /// <summary>
        /// Add a translation for a specific property on the destination type.
        /// Provide a function-equivalent that returns data of the property type.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="memberExpression"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> Translate<TProperty>(
            Expression<Func<TDestType, TProperty>> memberExpression,
            Func<TProperty> func)
        {
            var member = (memberExpression.Body as MemberExpression);
            string propertyName = member.Member.Name;
            return TranslateByName(propertyName, (source, context) => func());
        }

        /// <summary>
        /// Add a translation for a specific property by name on the destination type.
        /// Provide a function-equivalent that returns data of the property type or an assignable equivalent. 
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> TranslateByName(
            string propName,
            Func<object, ContextType, object> func)
        {
            InternalDefinition.Translators.Add(propName, func);
            return this;
        }

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
            InternalDefinition.PrepareProperties.Add(propertyName, (destObject, destProp, sourceObject, context) => action((TDestType)destObject, destProp, (TSourceType)sourceObject, context));
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
            InternalDefinition.PrepareProperties.Add(propName, action);
            return this;
        }

        /// <summary>
        /// Add a function-equivalent that is given the opportunity to inspect / change the source object and context before it is mapped
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> BeforeExpansion(Action<object, object, ContextType> action)
        {
            InternalDefinition._BeforeExpansion.Add(action);
            return this;
        }

        /// <summary>
        /// Add a function-equivalent that is given the opportunity to inspect / change the source object and context after it is mapped
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MappingDefinitionConfiguration<TSourceType, TDestType> AfterExpansion(Action<object, object, ContextType> action)
        {
            InternalDefinition._AfterExpansion.Add(action);
            return this;
        }
    }
}
