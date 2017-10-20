using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyward.Popcorn
{
    using ContextType = Dictionary<string, object>;
    /// <summary>
    /// This is the definition of how to map one type to another using the expander.  You can, but probably
    /// shouldn't create this directly.  Instead, use PopcornConfiguration.
    /// </summary>
    internal class MappingDefinition
    {

        /// <summary>
        /// The type to project to when nothing is specified
        /// </summary>
        public Type DefaultDestinationType;

        /// <summary>
        /// Any actions to handle specific properties on the source type
        /// </summary>
        public Dictionary<string, Action<object, PropertyInfo, object, ContextType>> PrepareProperties { get; } = new Dictionary<string, Action<object, PropertyInfo, object, ContextType>>();

        /// <summary>
        /// Any actions to handle source objects before they are expanded
        /// </summary>
        public List<Action<object, object, ContextType>> BeforeExpansion { get; } = new List<Action<object, object, ContextType>>();

        /// <summary>
        /// Any actions to verify that an object is authorized in the current context
        /// </summary>
        public List<Func<object, ContextType, object, bool>> Authorizers { get; } = new List<Func<object, ContextType, object, bool>>();

        /// <summary>
        /// Available destination types
        /// </summary>
        public Dictionary<Type, ProjectionDefinition> Destinations = new Dictionary<Type, ProjectionDefinition>();

        /// <summary>
        /// The type to project to if a specific one isn't requested
        /// </summary>
        /// <returns></returns>
        public ProjectionDefinition DefaultDestination()
        {
            return Destinations[DefaultDestinationType];
        }

        /// <summary>
        /// Get the projection definition for a specific destination type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public ProjectionDefinition DestinationForType(Type t)
        {
            if (Destinations.ContainsKey(t))
                return Destinations[t];
            return null;
        }

        /// <summary>
        /// Get the projection definition for a specific destination type (generic)
        /// </summary>
        /// <typeparam name="TDest"></typeparam>
        /// <returns></returns>
        public ProjectionDefinition DestinationForType<TDest>()
        {
            return Destinations.ContainsKey(typeof(TDest)) ? Destinations[typeof(TDest)] : null;
        }
    }
}
