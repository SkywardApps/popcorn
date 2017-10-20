using System;
using System.Collections.Generic;

namespace Skyward.Popcorn
{
    using ContextType = Dictionary<string, object>;
    internal class ProjectionDefinition
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
        /// Any actions to handle source objects after they are expanded
        /// </summary>
        public List<Action<object, object, ContextType>> AfterExpansion { get; } = new List<Action<object, object, ContextType>>();

        /// <summary>
        /// The list of includes to use if none are explicitly requested
        /// </summary>
        public string DefaultIncludes { get; set; } = "";

    }
}
