using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;
    /// <summary>
    /// This is the definition of how to map one type to another using the expander.  You can, but probably
    /// shouldn't create this directly.  There is a generic version that has a fluent api.
    /// </summary>
    internal class MappingDefinition
    {

        /// <summary>
        /// The type to map to
        /// </summary>
        public Type DestinationType { get; set; }
        /// <summary>
        /// Any functions to handle specific properties on the destination type
        /// </summary>
        public Dictionary<string, Func<object, ContextType, object>> Translators { get; } = new Dictionary<string, Func<object, ContextType, object>>();
        /// <summary>
        /// Any actions to handle specific properties on the source type
        /// </summary>
        public Dictionary<string, Action<object, PropertyInfo, object, ContextType>> PrepareProperties { get; } = new Dictionary<string, Action<object, PropertyInfo, object, ContextType>>();
        /// <summary>
        /// Any actions to handle source objects before they are expanded
        /// </summary>
        public List<Action<object, object, ContextType>> _BeforeExpansion { get; } = new List<Action<object, object, ContextType>>();
        /// <summary>
        /// Any actions to handle source objects after they are expanded
        /// </summary>
        public List<Action<object, object, ContextType>> _AfterExpansion { get; } = new List<Action<object, object, ContextType>>();
        /// <summary>
        /// Any actions to verify that an object is authorized in the current context
        /// </summary>
        public List<Func<object, ContextType, object,bool>> _Authorizers { get; } = new List<Func<object, ContextType, object, bool>>();
        /// <summary>
        /// The list of includes to use if none are explicitly requested
        /// </summary>
        public string DefaultIncludes { get; set; } = "";

    }
}
