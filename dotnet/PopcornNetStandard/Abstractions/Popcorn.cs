using Skyward.Popcorn.Expanders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn.Abstractions
{
    public class TypeConfiguration
    {
        public TypeConfiguration(Type type)
        {
            Type = type;
            Authorizers = new List<Func<object, object, bool>>();
            DefaultInclude = new List<PropertyReference>();
            AlwaysInclude = new List<string>();
            NeverInclude = new List<string>();
            ConditionalInclude = new List<Func<Type, object, IReadOnlyList<PropertyReference>, List<PropertyReference>>>();
        }

        public Type Type { get; }

        public List<Func<object, object, bool>> Authorizers { get; set; }
        public List<PropertyReference> DefaultInclude { get; set; }
        public List<string> AlwaysInclude { get; set; }
        public List<string> NeverInclude { get; set; }
        public bool AssignDirect { get; set; }
        internal List<Func<Type, object, IReadOnlyList<PropertyReference>, List<PropertyReference>>> ConditionalInclude {get;set;}
    }


    public class PopcornFactory
    {
        readonly List<IPopcornExpander> _expanders = new List<IPopcornExpander>();
        readonly Dictionary<Type, TypeConfiguration> _typeConfigurations = new Dictionary<Type, TypeConfiguration>();

        public IPopcorn CreatePopcorn()
        {
            return new Popcorn(this);
        }


        public PopcornFactory UseExpander(IPopcornExpander expander)
        {
            if (expander is null)
            {
                throw new ArgumentNullException(nameof(expander));
            }

            _expanders.Add(expander);
            return this;
        }

        public PopcornFactory ConfigureType<Type>(Action<TypeConfiguration> configure)
        {
            var config = new TypeConfiguration(typeof(Type));
            configure(config);
            _typeConfigurations.Add(typeof(Type), config);
            return this;
        }

        public PopcornFactory AssignDirect<Type>()
        {
            _typeConfigurations.Add(typeof(Type), new TypeConfiguration(typeof(Type)) { 
                AssignDirect = true
            });
            return this;
        }

        public PopcornFactory UseDefaultConfiguration()
        {
            AssignDirect<string>();
            AssignDirect<bool>();
            AssignDirect<Byte>();
            AssignDirect<SByte>();
            AssignDirect<UInt16>();
            AssignDirect<UInt32>();
            AssignDirect<UInt64>();
            AssignDirect<Int16>();
            AssignDirect<Int32>();
            AssignDirect<Int64>();
            AssignDirect<Decimal>();
            AssignDirect<Double>();
            AssignDirect<Single>();
            AssignDirect<DateTime>();
            AssignDirect<DateTimeOffset>();
            UseExpander(new PopcornCollectionExpander());
            UseExpander(new PopcornDictionaryExpander());
            UseExpander(new PopcornBlindExpander());
            return this;
        }

        private class Popcorn : IPopcorn
        {
            readonly PopcornFactory _factory;
            readonly HashSet<int> _visited = new HashSet<int>();

            public Popcorn(PopcornFactory factory)
            {
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            }

            public object Expand<T>(T instance, IReadOnlyList<PropertyReference> includes = null)
            {
                return Expand(typeof(T), instance, includes);
            }

            public object Expand(Type sourceType, object instance, IReadOnlyList<PropertyReference> includes = null)
            {
                if (!_factory._typeConfigurations.ContainsKey(sourceType))
                {
                    // Figure out things like default includes, etc
                    // Build and assign this type config
                    _factory._typeConfigurations.Add(sourceType, new TypeConfiguration(sourceType));
                }

                var typeConfiguration = _factory._typeConfigurations[sourceType];
                if (typeConfiguration.AssignDirect == true)
                {
                    return instance;
                }

                // Figure out the include list
                Dictionary<string, PropertyReference> includeMap = (includes == null)
                    ? typeConfiguration.DefaultInclude.ToDictionary(kv => kv.PropertyName)
                    : includes.ToDictionary(kv => kv.PropertyName);

                // Add Always includes
                foreach (var include in typeConfiguration.AlwaysInclude)
                {
                    includeMap.Add(include, new PropertyReference
                    {
                        PropertyName = include
                    });
                }

                foreach (var test in typeConfiguration.ConditionalInclude)
                {
                    var revisedMap = test(sourceType, instance, new List<PropertyReference>(includeMap.Values));
                    if (revisedMap != null)
                    {
                        includeMap = revisedMap.ToDictionary(kv => kv.PropertyName);
                    }
                }

                // Remove Never includes
                foreach (var disinclude in typeConfiguration.NeverInclude)
                {
                    if (includeMap.ContainsKey(disinclude))
                    {
                        includeMap.Remove(disinclude);
                    }
                }

                // find the appropriate expander
                foreach (var expander in _factory._expanders)
                {
                    if (expander.WillHandle(sourceType, instance, this))
                    {
                        EnforceUniqueVisit(instance);
                        return expander.Expand(sourceType, instance, new List<PropertyReference>(includeMap.Values), this);
                    }
                }

                typeConfiguration.AssignDirect = true;
                return instance;
            }


            /// <summary>
            /// Track each object we visit to make sure we don't end up in an infinite loop
            /// </summary>
            /// <param name="source"></param>
            /// <param name="visited"></param>
            /// <returns></returns>
            private void EnforceUniqueVisit(object source)
            {
                var key = RuntimeHelpers.GetHashCode(source);
                if (_visited.Contains(key))
                {
                    throw new SelfReferencingLoopException();
                }
                _visited.Add(key);
            }

            /// <summary>
            /// Test if an object is authorized in a given context, from the given source.
            /// Source may be an object (if value was from a property) or the collection 
            /// (if value is contained within it).
            /// </summary>
            /// <param name="source"></param>
            /// <param name="context"></param>
            /// <param name="valueToAssign"></param>
            /// <returns>True if authorized, false if rejected</returns>
            public bool AuthorizeValue(object source, object valueToAssign)
            {
                if (valueToAssign == null)
                    return true;

                var sourceType = source.GetType();
                var assignType = valueToAssign.GetType();

                if (_factory._typeConfigurations.ContainsKey(assignType))
                {
                    foreach (var authorization in _factory._typeConfigurations[assignType].Authorizers)
                        if (!authorization(source, valueToAssign))
                            return false;
                }

                return true;
            }


            /// <summary>
            /// Retrieve a value to be assigned to a property on the projection.
            /// This may mean invoking a translator, retrieving a property, or executing a method.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="context"></param>
            /// <param name="propertyName"></param>
            /// <param name="translators"></param>
            /// <returns></returns>
            public object GetSourceValue(object source, string propertyName)
            {
                object valueToAssign = null;
                Type sourceType = source.GetType();
                var matchingProperty = sourceType.GetTypeInfo().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                var matchingMethod = sourceType.GetTypeInfo().GetMethod(propertyName, BindingFlags.Instance | BindingFlags.Public);

                // Check if the value is marked as InternalOnly
                InternalOnlyAttribute[] attributes = new InternalOnlyAttribute[2];
                attributes[0] = matchingProperty?.GetCustomAttribute<InternalOnlyAttribute>();
                attributes[1] = matchingMethod?.GetCustomAttribute<InternalOnlyAttribute>();

                string[] names = new string[2];
                names[0] = matchingProperty?.Name + " property";
                names[1] = matchingMethod?.Name + " method";

                for (int i = 0; i < attributes.Length; i++)
                {
                    var internalOnlyAttr = attributes[i];

                    if (internalOnlyAttr == null)
                    {
                        continue;
                    }

                    if (internalOnlyAttr.ThrowException)
                    {
                        throw new InternalOnlyViolationException(
                            string.Format("Expand: {0} inside {1} class is marked [InternalOnly]", names[i], sourceType.Name));
                    }

                    return null;
                }

                if (matchingProperty != null)
                {
                    valueToAssign = matchingProperty.GetValue(source);
                }

                // If there's a simple method we can call, invoke it and assign the results
                else if (matchingMethod != null
                    && matchingMethod.ReturnType != null
                    && matchingMethod.ReturnType != typeof(void))
                {
                    var parameterList = matchingMethod.GetParameters();
                    if (!parameterList.Any())
                        valueToAssign = matchingMethod.Invoke(source, new object[] { });
                    else
                    {
                        // Couldn't map it, but it was explicitly requested, so throw an error
                        throw new InvalidCastException(propertyName);
                    }
                }
                else
                {
                    // Couldn't map it, but it was explicitly requested, so throw an error
                    throw new InvalidCastException(propertyName);
                }

                return valueToAssign;
            }
        }
    }
}
