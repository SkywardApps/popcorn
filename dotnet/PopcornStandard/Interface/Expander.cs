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
    /// This is the public interface part for the 'Expander' class.
    /// The expander will allow you to project from one type to another, dynamically selecting which properties to include and
    /// which properties to descend into and retrieve (the expansion part).
    /// 
    /// Types will be mapped implicitly where possible, or you may provide a 'Translator' that handles providing data for a 
    /// particular property.
    /// 
    /// This is intended primarily for Api usage so a client can selectively include properties and nested data in their query.
    /// </summary>
    public partial class Expander
    {
        /// <summary>
        /// This is the core of the expander.  This registers incoming types (the source of the data) and specifies a 
        /// single outgoing type that it will be converted to.
        /// 
        /// It is possible that in the future we may want to provide multiple destination options, primarily for nested 
        /// entities.  Top-level entities will always need a 'default' outgoing type.
        /// </summary>
        internal Dictionary<Type, MappingDefinition> Mappings { get; } = new Dictionary<Type, MappingDefinition>();
        internal Dictionary<Type, Func<ContextType, object>> Factories { get; } = new Dictionary<Type, Func<ContextType, object>>();
        internal HashSet<Type> BlacklistExpansion = new HashSet<Type>
        {
            typeof(string),
        };
        internal bool ExpandBlindObjects { get; set; } = false;

        /// <summary>
        /// Query whether or not a particular object is either a Mapped type or a collection of a Mapped type.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public bool WillExpand(object source)
        {
            if (source == null) { return false; } // handling the null response
            Type sourceType = source.GetType();
            return WillExpandType(sourceType);

        }

        /// <summary>
        /// Query whether or not a particular type is either a Mapped type or a collection of a Mapped type.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public bool WillExpandType(Type sourceType)
        {
            if (BlacklistExpansion.Contains(sourceType))
                return false;

            if (WillExpandDirect(sourceType))
                return true;
            if (WillExpandCollection(sourceType))
                return true;
            return WillExpandBlind(sourceType);
        }

        /// <summary>
        /// The entry point method for converting a type into its projection and selectively including data.
        /// This will work on either a Mapped Type or a collection of a Mapped Type.
        /// This version using anonymous objects works well for the Api use case.  We may want a generic typed
        /// version if we ever think of a reason to use this elsewhere.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context">A context dictionary that will be passed around to all conversion routines.</param>
        /// <param name="includes"></param>
        /// <param name="visited"></param>
        /// <param name="destinationTypeHint">todo: describe destinationTypeHint parameter on Expand</param>
        /// <returns></returns>
        public object Expand(object source, ContextType context = null, IEnumerable<PropertyReference> includes = null, HashSet<int> visited = null, Type destinationTypeHint = null)
        {
            // Create a context if one wasn't provided
            if (context == null)
                context = new ContextType();

            // Create an empty include list if one wasn't provided
            if (includes == null)
                includes = new PropertyReference[] { };

            Type sourceType = source.GetType();

            if(visited == null)
                visited = new HashSet<int>();

            // See if this is a directly expandable type (Mapped Type)
            if (WillExpandDirect(sourceType))
            {
                return ExpandDirectObject(source, context, includes, visited);
            }

            // Otherwise, see if this is a collection of an expandable type
            if (WillExpandCollection(sourceType))
            {
                return ExpandCollection(source, destinationTypeHint??typeof(ArrayList), context, includes, visited);
            }

            if (WillExpandBlind(sourceType))
            {
                return ExpandBlindObject(source, context, includes, visited);
            }

            // Otherwise, the caller requested that we expand a type we have no knowledge of.
            throw new UnknownMappingException(sourceType.ToString());
        }

        public object Sort(object source, string sortTarget, string sortDirection)
        {
            var originalList = (IList)source;

            // Start by finding all of the properties on the entity in question
            var typeInfo = originalList[0].GetType().GetTypeInfo();

            if (typeInfo.DeclaredProperties.FirstOrDefault(values => values.Name.Contains(sortTarget)) == null)
            {
                // TODO: Consider making an "InvalidSortError"
                throw new InvalidCastException(sortTarget);
            }

            foreach (PropertyInfo info in typeInfo.DeclaredProperties)
            {
                // Make sure the property target passed in exists on the result and that there is more than 1 result
                if (info.Name == sortTarget && originalList.Count > 1)
                {
                    // Instantiate a list to hold the sorting results as we add them
                    var sortingList = new List<object> { };

                    // Loop through each result on the original list to place it in the holder list
                    for (int j = 0; j < originalList.Count; j++)
                    {
                        // This will throw an InvalidCastException for more complex types
                        IComparable targetObject = (IComparable)info.GetValue(originalList[j]);

                        if (sortDirection == "descending")
                        {
                            // Loop through the sorted list to put the object where it belongs
                            int originalCount = sortingList.Count;
                            for (int i = 0; i <= originalCount; i++)
                            {
                                // Add initial result
                                if (sortingList.Count == 0)
                                {
                                    sortingList.Insert((0), originalList[j]);
                                    break;
                                }

                                IComparable indexedObject = (IComparable)info.GetValue(sortingList[i]);
                                int compareResult = targetObject.CompareTo(indexedObject);

                                // Sort to see where the result should be added
                                if (compareResult < 0)
                                {
                                    sortingList.Insert((i), originalList[j]);
                                    break;
                                }
                                // Check forward to see if this is the end of the values to poll through and tack to the end
                                else if ((i + 1) == originalCount)
                                {
                                    sortingList.Insert((originalCount), originalList[j]);
                                    break;
                                }
                            }
                        }
                        else if (sortDirection == "ascending")
                        {
                            // Loop through the sorted list to put the object where it belongs
                            int originalCount = sortingList.Count;
                            for (int i = originalCount; i >= 0; i--)
                            {
                                // Add initial result
                                if (originalCount == 0)
                                {
                                    sortingList.Insert((0), originalList[j]);
                                    break;
                                }

                                IComparable indexedObject = (IComparable)info.GetValue(sortingList[i-1]);
                                int compareResult = targetObject.CompareTo(indexedObject);

                                // Sort to see where the result should be added
                                if (compareResult < 0)
                                {
                                    sortingList.Insert((i), originalList[j]);
                                    break;
                                }
                                // Check forward to see if this is the end of the values to poll through and tack to the end
                                else if ((i - 1) == 0)
                                {
                                    sortingList.Insert((0), originalList[j]);
                                    break;
                                }
                            }
                        }
                    }
                    
                    // Reset the orginal list to sortingList to return the correct results
                    originalList = sortingList;
                }
            }

            return originalList;
        }

    }
}
