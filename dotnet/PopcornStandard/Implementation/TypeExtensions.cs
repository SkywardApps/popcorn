using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// Provides extensions to 'Type' for the expander to use
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Is this type a Nullable instance?
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullableType(this Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        /// <summary>
        /// Do our best to construct an object of the given type.
        /// This will look for public constructors:
        ///     That take an source type and Context Type
        ///     That take a Context Type
        ///     That take an source type Type
        ///     That take no parameters
        /// </summary>
        /// <param name="destinationType"></param>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static object CreateObjectFromSource(this Type destinationType, object source, ContextType context)
        {
            var sourceType = source.GetType();

            ConstructorInfo constructor = null;

            // We want to check for four constructors in descending order:
            if (context != null)
            {
                var contextType = context.GetType();

                // One that takes a source and context object
                constructor = destinationType.GetTypeInfo().GetConstructor(new Type[] { sourceType, contextType });
                if (constructor != null)
                    return constructor.Invoke(new object[] { source, context });

                // One that takes a context
                constructor = destinationType.GetTypeInfo().GetConstructor(new Type[] { contextType });
                if (constructor != null)
                    return constructor.Invoke(new object[] { context });
            }

            // One that takes a source
            constructor = destinationType.GetTypeInfo().GetConstructor(new Type[] { sourceType });
            if (constructor != null)
                return constructor.Invoke(new object[] { source });

            // One with no parameters
            constructor = destinationType.GetTypeInfo().GetConstructor(Type.EmptyTypes);
            if (constructor != null)
                return constructor.Invoke(new object[] { });

            return null;
        }
    }
}
