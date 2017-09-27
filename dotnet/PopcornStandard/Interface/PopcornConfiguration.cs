using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyward.Popcorn
{
    using ContextType = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// A fluent-api style configuration object for the ApiExpander
    /// </summary>
    public class PopcornConfiguration
    {
        Expander _expander;
        public PopcornConfiguration(Expander expander) { _expander = expander; }

        public ContextType Context { get; private set; }
        public Func<object, object, object> Inspector { get; private set; }

        public bool ApplyToAllEndpoints { get; private set; } = true;

        /// <summary>
        /// Designate the context for this target
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public PopcornConfiguration SetContext(ContextType context)
        {
            if (Context != null)
                throw new InvalidOperationException("Context has already been assigned");

            Context = context;
            return this;
        }

        /// <summary>
        /// Designate an inspector to run on expanded objects
        /// </summary>
        /// <param name="inspector"></param>
        /// <returns></returns>
        public PopcornConfiguration SetInspector(Func<object, object, object> inspector)
        {
            if (Inspector != null)
                throw new InvalidOperationException("Inspector has already been assigned");
            Inspector = inspector;
            return this;
        }

        /// <summary>
        /// Set this configuration to only expand endpoints that have the ExpandResult attribute set
        /// </summary>
        /// <returns></returns>
        public PopcornConfiguration SetOptIn()
        {
            ApplyToAllEndpoints = false;
            return this;
        }

        /// <summary>
        /// Add a mapping of a data type to a projection type
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <typeparam name="TDestType"></typeparam>
        /// <param name="defaultIncludes"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public PopcornConfiguration Map<TSourceType, TDestType>(
            string defaultIncludes = null, 
            Action<MappingDefinitionConfiguration<TSourceType, TDestType>> config = null)
        {
            var sourceType = typeof(TSourceType);
            var destType = typeof(TDestType);

            var destTypeInfo = typeof(TDestType).GetTypeInfo();
            var parsedDefaultIncludes = (defaultIncludes == null) ? new List<PropertyReference> { } : (List<PropertyReference>)PropertyReference.Parse(defaultIncludes);
            defaultIncludes = CompareAndConstructDefaultIncludes(parsedDefaultIncludes, destTypeInfo);

            var definition = new MappingDefinitionConfiguration<TSourceType, TDestType>
            {
                InternalDefinition = new MappingDefinition
                {
                    DestinationType = destType,
                    DefaultIncludes = defaultIncludes
                }
            };

            // We will allow a client to reference the same mapping multiple times to add more translations etc,
            // but ONLY if the types remain consistent!
            if (_expander.Mappings.ContainsKey(sourceType))
            {
                if (_expander.Mappings[sourceType].DestinationType != destType)
                    throw new InvalidOperationException(
                        $"Expander was mapped multiple times but types do not match."
                        + " {sourceType} was previously mapped to {this.Mappings[sourceType].DestinationType} and attempted to remap to {destType}."
                        + "  Only one destination type can be specified.");
                if (defaultIncludes != null)
                    _expander.Mappings[sourceType].DefaultIncludes = defaultIncludes;
                definition = new MappingDefinitionConfiguration<TSourceType, TDestType>
                {
                    InternalDefinition = _expander.Mappings[sourceType]
                };
            }
            else
            {
                _expander.Mappings.Add(typeof(TSourceType), definition.InternalDefinition);
            }

            if (config != null)
                config(definition);

            return this;
        }

        /// <summary>
        /// Assign a factory function to create a specific type
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <param name="factory"></param>
        /// <returns></returns>
        public PopcornConfiguration AssignFactory<TSourceType>(Func<TSourceType> factory)
        {
            if (factory != null)
            {
                _expander.Factories.Add(typeof(TSourceType), (context) => factory());
            }
            else if(_expander.Factories.ContainsKey(typeof(TSourceType)))
            {
                _expander.Factories.Remove(typeof(TSourceType));
            }
            return this;
        }

        /// <summary>
        /// Assign a factory function to create a specific type from a context object
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <param name="factory"></param>
        /// <returns></returns>
        public PopcornConfiguration AssignFactory<TSourceType>(Func<ContextType, TSourceType> factory)
        {
            if (factory != null)
            {
                _expander.Factories.Add(typeof(TSourceType), (context) => factory(context));
            }
            else if (_expander.Factories.ContainsKey(typeof(TSourceType)))
            {
                _expander.Factories.Remove(typeof(TSourceType));
            }
            return this;
        }

        /// <summary>
        /// Assign a factory function to create a specific type from a context object
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <param name="factory"></param>
        /// <returns></returns>
        public string CompareAndConstructDefaultIncludes(List<PropertyReference> parsedDefaultIncludes, TypeInfo destTypeInfo)
        {
            foreach (PropertyInfo propertyInfo in destTypeInfo.DeclaredProperties)
            {
                var customAttributesOriginal = (Array)propertyInfo.GetCustomAttributes();
                if (customAttributesOriginal.Length == 0)
                {
                    continue;
                }
                else
                {
                    foreach (Attribute customAttribute in customAttributesOriginal)
                    {
                        var type = customAttribute.GetType();
                        if (type.Namespace == "Skyward.Popcorn" && type.Name == "IncludeByDefault")
                        {
                            foreach (PropertyReference reference in parsedDefaultIncludes)
                            {
                                if (reference.PropertyName == propertyInfo.Name)
                                {
                                    throw new Exception($"Property {propertyInfo.Name} is marked as [IncludeByDefault] and declared in the config.map function.");
                                }
                            }

                            parsedDefaultIncludes.Add(new PropertyReference { PropertyName = propertyInfo.Name });
                        }
                    }
                }
            }

            if (parsedDefaultIncludes.Count == 0)
            {
                return "[]";
            } else
            {
                string includesStringConstructor = "[";
                for (int i = 0; i < parsedDefaultIncludes.Count; i++)
                {
                    int adjustedCount = parsedDefaultIncludes.Count - 1;
                    if (i < adjustedCount)
                    {
                        includesStringConstructor = includesStringConstructor + parsedDefaultIncludes[i].PropertyName + ",";
                    } else if (i == adjustedCount)
                    {
                        includesStringConstructor = includesStringConstructor + parsedDefaultIncludes[i].PropertyName + "]";
                    }
                }

                return includesStringConstructor;
            }
        }

        public PopcornConfiguration BlacklistExpansion<TSourceType>()
        {
            _expander.BlacklistExpansion.Add(typeof(TSourceType));
            return this;
        }

        public PopcornConfiguration BlacklistExpansion(Type type)
        {
            _expander.BlacklistExpansion.Add(type);
            return this;
        }

        public PopcornConfiguration EnableBlindExpansion(bool v)
        {
            _expander.ExpandBlindObjects = v;
            return this;
        }
    }
}