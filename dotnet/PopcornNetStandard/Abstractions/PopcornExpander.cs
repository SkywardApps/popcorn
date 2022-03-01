using System;
using System.Collections.Generic;

namespace Skyward.Popcorn.Abstractions
{
    #nullable enable
    public class PopcornExpander : IPopcornExpander
    {
        private readonly Func<Type, object, IPopcorn, bool> _test;
        private readonly Func<Type, object, IReadOnlyList<PropertyReference>, IPopcorn, object> _expand;

        public PopcornExpander(Func<Type, object, IPopcorn, bool> test, Func<Type, object, IReadOnlyList<PropertyReference>, IPopcorn, object> expand)
        {
            _test = test ?? throw new ArgumentNullException(nameof(test));
            _expand = expand ?? throw new ArgumentNullException(nameof(expand));
        }

        public PopcornExpander(Func<Type, bool> test, Func<Type, object, IReadOnlyList<PropertyReference>, IPopcorn, object> expand)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }
            _test = (type, source, popcorn) => test(type);
            _expand = expand ?? throw new ArgumentNullException(nameof(expand));
        }

        public static PopcornExpander ExpanderForType<T>(Func<Type, object, IReadOnlyList<PropertyReference>, IPopcorn, object> expand)
        {
            return new PopcornExpander((type) => type == typeof(T), expand);
        }

        public static PopcornExpander ExpanderForType<T>(Func<T, bool> test,  Func<Type, object, IReadOnlyList<PropertyReference>, IPopcorn, object> expand) where T : class
        {
            return new PopcornExpander((type, source, popcorn) => type == typeof(T) && test(source as T), expand);
        }

        public bool WillHandle(Type sourceType, object instance, IPopcorn popcorn)
        {
            return _test(sourceType, instance, popcorn);
        }

        public object Expand(Type sourceType, object instance, IReadOnlyList<PropertyReference> properties, IPopcorn popcorn)
        {
            return _expand(sourceType, instance, properties, popcorn);
        }
    }
}
