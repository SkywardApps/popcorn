using Skyward.Popcorn.Expanders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Skyward.Popcorn.Abstractions
{
    #nullable enable
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
            EnumeratedProperties = new List<string>();
        }

        public Type Type { get; }

        public List<Func<object, object, bool>> Authorizers { get; set; }
        public List<PropertyReference> DefaultInclude { get; set; }
        public List<string> AlwaysInclude { get; set; }
        public List<string> NeverInclude { get; set; }
        public bool AssignDirect { get; set; }
        public List<string> EnumeratedProperties { get; set; }
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
            readonly HashSet<object> _visited = new HashSet<object>();

            public Popcorn(PopcornFactory factory)
            {
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            }

            public object? Expand<T>(T? instance, IReadOnlyList<PropertyReference>? includes = null) where T : class
            {
                return Expand(typeof(T), instance, includes);
            }

            public object? Expand(Type sourceType, object? instance, IReadOnlyList<PropertyReference>? includes = null)
            {
                if (instance == null)
                {
                    return null;
                }

                TypeConfiguration typeConfiguration = GetOrBuildDefaultTypeConfig(sourceType);
                if (typeConfiguration.AssignDirect == true)
                {
                    return instance;
                }

                // find the appropriate expander
                foreach (var expander in _factory._expanders)
                {
                    if (expander.WillHandle(sourceType, instance, this))
                    {
                        var finalIncludes = includes;
                        if (expander.ShouldApplyIncludes)
                        {
                            Dictionary<string, PropertyReference> includeMap = DeterminePropertyReferences(sourceType, instance, includes, typeConfiguration);
                            finalIncludes = new List<PropertyReference>(includeMap.Values);
                        }

                        EnforceUniqueVisit(instance);
                        return expander.Expand(sourceType, instance, new List<PropertyReference>(finalIncludes), this);
                    }
                }

                typeConfiguration.AssignDirect = true;
                return instance;
            }

            private TypeConfiguration GetOrBuildDefaultTypeConfig(Type sourceType)
            {
                if (!_factory._typeConfigurations.ContainsKey(sourceType))
                {
                    // Build and assign this type config
                    TypeConfiguration config = BuildTypeDefaults(sourceType);
                    _factory._typeConfigurations.Add(sourceType, config);
                }

                // Check if we can bail out early
                var typeConfiguration = _factory._typeConfigurations[sourceType];
                return typeConfiguration;
            }

            public Dictionary<string, PropertyReference> DeterminePropertyReferences<T>(IReadOnlyList<PropertyReference> includes)
            {
                TypeConfiguration typeConfiguration = GetOrBuildDefaultTypeConfig(typeof(T));
                return DeterminePropertyReferences(typeof(T), null, includes, typeConfiguration);
            }

            private Dictionary<string, PropertyReference> DeterminePropertyReferences(Type sourceType, object? instance, IReadOnlyList<PropertyReference>? includes, TypeConfiguration typeConfiguration)
            {
                // Figure out the include list now we know we need it
                Dictionary<string, PropertyReference> includeMap = (includes == null || !includes.Any())
                    ? typeConfiguration.DefaultInclude.ToDictionary(kv => kv.PropertyName)
                    : includes.ToDictionary(kv => kv.PropertyName);

                // Handle wildcard expansion
                if (includes != null && includes.Any(i => i.PropertyName == "!default"))
                {
                    foreach (var defaultInclude in typeConfiguration.DefaultInclude)
                    {
                        if (!includeMap.ContainsKey(defaultInclude.PropertyName))
                        {
                            includeMap.Add(defaultInclude.PropertyName, defaultInclude);
                        }
                    }
                }

                if (includes != null && includes.Any(i => i.PropertyName == "!all"))
                {
                    if (!typeConfiguration.EnumeratedProperties.Any())
                    {
                        CacheEnumeratedProperties(typeConfiguration);
                    }

                    foreach (var property in typeConfiguration.EnumeratedProperties)
                    {
                        if (!includeMap.ContainsKey(property))
                        {
                            includeMap.Add(property, new PropertyReference(property));
                        }
                    }
                }

                // Add Always includes
                foreach (var include in typeConfiguration.AlwaysInclude)
                {
                    includeMap.Add(include, new PropertyReference(include));
                }

                if (instance != null)
                {
                    foreach (var test in typeConfiguration.ConditionalInclude)
                    {
                        var revisedMap = test(sourceType, instance, new List<PropertyReference>(includeMap.Values));
                        if (revisedMap != null)
                        {
                            includeMap = revisedMap.ToDictionary(kv => kv.PropertyName);
                        }
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

                return includeMap;
            }

            private void CacheEnumeratedProperties(TypeConfiguration config)
            {
                var sourceType = config.Type;
                config.EnumeratedProperties.Clear();
                // Loop through each property on an entity to see if anything is declared to IncludeByDefault
                foreach (PropertyInfo propertyInfo in sourceType.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    config.EnumeratedProperties.Add(propertyInfo.Name);
                }
            }

            private static TypeConfiguration BuildTypeDefaults(Type sourceType)
            {
                // Figure out things like default includes, etc
                var config = new TypeConfiguration(sourceType);

                // Loop through each property on an entity to see if anything is declared to IncludeByDefault
                foreach (PropertyInfo propertyInfo in sourceType.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    config.EnumeratedProperties.Add(propertyInfo.Name);
                    var customAttributesOriginal = (Array)propertyInfo.GetCustomAttributes();
                    if (customAttributesOriginal.Length == 0)
                    {
                        // No custom attributes means the next steps can be ignored
                        continue;
                    }
                    else
                    {
                        // Circle through the attributes to see if our IncludeByDefault is one of them
                        foreach (Attribute customAttribute in customAttributesOriginal)
                        {
                            var type = customAttribute.GetType();
                            if (type == typeof(IncludeByDefault))
                            {
                                config.DefaultInclude.Add(new PropertyReference(propertyInfo.Name));
                            }
                            else if (type == typeof(IncludeAlways))
                            {
                                config.AlwaysInclude.Add(propertyInfo.Name);
                            }
                            else if (type == typeof(InternalOnly))
                            {
                                config.NeverInclude.Add(propertyInfo.Name);
                            }
                        }
                    }
                }

                // If there were no explicit defaults, everything is default
                if (!config.DefaultInclude.Any())
                {
                    config.DefaultInclude = config.EnumeratedProperties.Select(val => new PropertyReference(val)).ToList();
                }

                return config;
            }


            /// <summary>
            /// Track each object we visit to make sure we don't end up in an infinite loop
            /// </summary>
            /// <param name="source"></param>
            /// <param name="visited"></param>
            /// <returns></returns>
            private void EnforceUniqueVisit(object source)
            {
                if (_visited.Contains(source))
                {
                    throw new SelfReferencingLoopException();
                }
                _visited.Add(source);
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
            public bool AuthorizeValue(object source, string propertyName, object? valueToAssign)
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
            public object? GetSourceValue(object source, string propertyName)
            {
                Type sourceType = source.GetType();
                var matchingProperty = sourceType.GetTypeInfo().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                var matchingMethod = sourceType.GetTypeInfo().GetMethod(propertyName, BindingFlags.Instance | BindingFlags.Public);

                // Check if the value is marked as InternalOnly
                InternalOnly?[] attributes = new InternalOnly?[2];
                attributes[0] = matchingProperty?.GetCustomAttribute<InternalOnly>();
                attributes[1] = matchingMethod?.GetCustomAttribute<InternalOnly>();

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

                object? valueToAssign = null;

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
