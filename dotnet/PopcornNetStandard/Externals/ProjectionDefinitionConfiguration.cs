using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Skyward.Popcorn
{/*
    using ContextType = Dictionary<string, object>;

    /// <summary>
    /// This is the definition of how to map one type to another, specified by the generic parameters.  This provides
    /// a fluent api to customize the mapping.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    /// <typeparam name="TDestType"></typeparam>
    public class ProjectionDefinitionConfiguration<TSourceType>
    {
        internal MappingDefinition InternalMappingDefinition { get; set; }
        internal ProjectionDefinition InternalProjectionDefinition { get; set; }

        /// <summary>
        /// Add a translation for a specific property on the destination type.
        /// Provide a function-equivalent that takes the source object and the context and returns data of the property type.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="memberExpression"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public ProjectionDefinitionConfiguration<TSourceType> Translate<TProperty>(
            Expression<Func<TSourceType, TProperty>> memberExpression,
            Func<TSourceType, ContextType, TProperty> func)
        {
            var member = (memberExpression.Body as MemberExpression);
            string propertyName = member.Member.Name;
            return TranslateByName(propertyName, (source, context) => func((TSourceType)source, context));
        }

        public ProjectionDefinitionConfiguration<TSourceType> Translate<TProperty>(
            Expression<Func<TSourceType, TProperty>> memberExpression,
            Func<TSourceType, ContextType, object> func)
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
        public ProjectionDefinitionConfiguration<TSourceType> Translate<TProperty>(
            Expression<Func<TSourceType, TProperty>> memberExpression,
            Func<ContextType, object> func)
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
        public ProjectionDefinitionConfiguration<TSourceType> Translate<TProperty>(
            Expression<Func<TSourceType, TProperty>> memberExpression,
            Func<TSourceType, object> func)
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
        public ProjectionDefinitionConfiguration<TSourceType> Translate<TProperty>(
            Expression<Func<TSourceType, TProperty>> memberExpression,
            Func<object> func)
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
        public ProjectionDefinitionConfiguration<TSourceType, TDestType> TranslateByName(
            string propName,
            Func<object, ContextType, object> func)
        {
            InternalProjectionDefinition.Translators.Add(propName, func);
            return this;
        }

        /// <summary>
        /// Add a function-equivalent that is given the opportunity to inspect / change the source object and context after it is mapped
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ProjectionDefinitionConfiguration<TSourceType> AfterExpansion(Action<object, object, ContextType> action)
        {
            InternalProjectionDefinition.AfterExpansion.Add(action);
            return this;
        }

        /// <summary>
        /// Assign a function-equivalent that handles the entire projection
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public ProjectionDefinitionConfiguration<TSourceType> Handle(Func<object, IEnumerable<PropertyReference>, ContextType, MappingDefinition, ProjectionDefinition, object> handler)
        {
            InternalProjectionDefinition.Handler = handler;
            return this;
        }
    }*/
}
