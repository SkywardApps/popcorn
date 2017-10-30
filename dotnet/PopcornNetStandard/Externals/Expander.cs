using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static System.Reflection.TypeExtensions;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    public enum SortDirection { Unknown, Ascending, Descending }

    public interface IExpanderInternalConfiguration
    {
        Dictionary<Type, MappingDefinition> Mappings { get; }
        Dictionary<Type, Func<ContextType, object>> Factories { get; }
        bool ExpandBlindObjects { get; set; }

    }

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
    public partial class Expander : IExpanderInternalConfiguration
    {
        /// <summary>
        /// This is the core of the expander.  This registers incoming types (the source of the data) and specifies a 
        /// single outgoing type that it will be converted to.
        /// 
        /// It is possible that in the future we may want to provide multiple destination options, primarily for nested 
        /// entities.  Top-level entities will always need a 'default' outgoing type.
        /// </summary>
        Dictionary<Type, MappingDefinition> IExpanderInternalConfiguration.Mappings { get; } = new Dictionary<Type, MappingDefinition>();
        internal Dictionary<Type, MappingDefinition> Mappings => ((IExpanderInternalConfiguration)this).Mappings;
        Dictionary<Type, Func<ContextType, object>> IExpanderInternalConfiguration.Factories { get; } = new Dictionary<Type, Func<ContextType, object>>();
        internal Dictionary<Type, Func<ContextType, object>> Factories => ((IExpanderInternalConfiguration)this).Factories;
        bool IExpanderInternalConfiguration.ExpandBlindObjects { get; set; } = false;
        internal bool ExpandBlindObjects {
            get => ((IExpanderInternalConfiguration)this).ExpandBlindObjects;
            set => ((IExpanderInternalConfiguration)this).ExpandBlindObjects = value;
        }

        internal HashSet<Type> BlacklistExpansion = new HashSet<Type>
        {
            typeof(string),
        };

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
        /// This version allows specification of the includes in string format
        /// </summary>
        public object Expand(object source, ContextType context, string includes, HashSet<int> visited = null, Type destinationTypeHint = null)
        {
            return Expand(source, context, PropertyReference.Parse(includes), visited, destinationTypeHint);
        }

        /// <summary>
        /// The entry point method for converting a type into its projection and selectively including data.
        /// This will work on either a Mapped Type or a collection of a Mapped Type.
        /// This version allows specification of the includes as an IEnumerable of PropertyReferences.
        /// 
        /// Using anonymous objects works well for the Api use case.  
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

            if (visited == null)
                visited = new HashSet<int>();

            // Check if the source class is marked as InternalOnly
            var customAttr = sourceType.GetTypeInfo().GetCustomAttribute<InternalOnlyAttribute>();
            if (customAttr != null)
            {

                if (customAttr.ThrowExcepton)
                    throw new InternalOnlyViolationException();

                return null;
            }

            // See if this is a directly expandable type (Mapped Type)
            if (WillExpandDirect(sourceType))
            {
                return ExpandDirectObject(source, context, includes, visited, destinationTypeHint);
            }

            // Otherwise, see if this is a collection of an expandable type
            if (WillExpandCollection(sourceType))
            {
                return ExpandCollection(source, destinationTypeHint ?? typeof(ArrayList), context, includes, visited);
            }

            if (WillExpandBlind(sourceType))
            {
                return ExpandBlindObject(source, context, includes, visited);
            }

            // Otherwise, the caller requested that we expand a type we have no knowledge of.
            throw new UnknownMappingException(sourceType.ToString());
        }

        /// <summary>
        /// A generic overload that automatically provides the type hint.
        /// This accepts a string include list of the form "[Prop1,Prop2[SubProp1]]"
        /// </summary>
        /// <typeparam name="TDestType"></typeparam>
        /// <param name="source"></param>
        /// <param name="includes"></param>
        /// <param name="context"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        public TDestType Expand<TDestType>(object source, string includes, ContextType context = null, HashSet<int> visited = null)
        {
            return (TDestType)Expand(source, context, PropertyReference.Parse(includes), visited, typeof(TDestType));
        }

        /// <summary>
        /// A generic overload that automatically provides the type hint.
        /// This optionally accepts a list of PropertyReferences
        /// </summary>
        /// <typeparam name="TDestType"></typeparam>
        /// <param name="source"></param>
        /// <param name="includes"></param>
        /// <param name="context"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        public TDestType Expand<TDestType>(object source, IEnumerable<PropertyReference> includes = null, ContextType context = null, HashSet<int> visited = null)
        {
            return (TDestType)Expand(source, context, includes, visited, typeof(TDestType));
        }


        /// <summary>
        /// The entry point method for sorting an unknown object.
        /// This will work on either a Mapped Simple Type only.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sortTarget">The parameter desired to be sorted on.</param>
        /// <param name="sortDirection">An enumeration of possible options</param>
        /// <returns></returns>
        public object Sort(object source, string sortTarget, SortDirection sortDirection)
        {
            if (!(source is IEnumerable))
                throw new ArgumentException("'source' is not of a type that can be converted to an IEnumerable");
            IEnumerable<object> originalList = (source as IEnumerable).Cast<object>();

            // Make sure that there is more than 1 result so we actually hav something to sort
            if (originalList.Count() <= 1)
                return source;

            // Start by finding all of the properties on the entity in question
            TypeInfo typeInfo = originalList.First().GetType().GetTypeInfo();
            if (typeInfo.DeclaredProperties.FirstOrDefault(values => values.Name.Equals(sortTarget)) == null)
            {
                // TODO: Consider making an "InvalidSortError"
                throw new InvalidCastException(sortTarget);
            }

            // Get the property we actually want to target for sorting
            var sortProperty = typeInfo.GetProperty(sortTarget);

            // Instantiate a list that allows for easier sorting
            var sortingList = new List<object> { };
            foreach (object holder in originalList)
            {
                sortingList.Add(holder);
            }

            switch (sortDirection)
            {
                case SortDirection.Unknown:
                    throw new ArgumentException("Unknown sortDirection");
                case SortDirection.Ascending:
                    sortingList = sortingList.OrderBy(i => sortProperty.GetValue(i)).ToList();
                    break;
                case SortDirection.Descending:
                    sortingList = sortingList.OrderByDescending(i => sortProperty.GetValue(i)).ToList();
                    break;
            }

            // Reset the original object
            originalList = sortingList;

            return originalList;
        }
    }
}
