using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Skyward.Popcorn
{
    /// <summary>
    /// This describes a property to be referenced (by name) and an optional (recursive) list of properties to be referenced on the 
    /// property's value if it is a complex type.
    /// </summary>
    public class PropertyReference
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Any properties to reference on this property's value
        /// </summary>
        public List<PropertyReference> Children { get; } = new List<PropertyReference>();

        /// <summary>
        /// Helper method to take a string in the format "[name[subname1, subname2, ...],...]" and create a PropertyReference tree out of it.
        /// </summary>
        /// <param name="includes"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyReference> Parse(string includes)
        {
            var stack = new Stack<PropertyReference>();
            stack.Push(new PropertyReference()); // the root

            // A very simple state machine for parsing the include string format.
            // It is not terribly error-tolerant at the moment, but does work if the format provided
            // is correct.
            foreach(char c in includes)
            {
                PropertyReference child;
                switch(c)
                {
                    // Starting a child list, so add a new item on the stack
                    case '[':
                        stack.Push(new PropertyReference());
                        break;
                    // finished a reference, so add it as a child and start a peer
                    case ',':
                        child = stack.Pop();
                        child.PropertyName = child.PropertyName.TrimStart(' ').TrimEnd(' ');
                        stack.Peek().Children.Add(child);
                        stack.Push(new PropertyReference());
                        break;
                    // Completed a child list, so add the last item as a child and pop up the stack
                    case ']':
                        child = stack.Pop();
                        if (!String.IsNullOrWhiteSpace(child.PropertyName))
                        {
                            child.PropertyName = child.PropertyName.TrimStart(' ').TrimEnd(' ');
                            if (child.PropertyName == "*" && stack.Peek().PropertyName == "*")
                            {
                                throw new InvalidCastException("A wildcard was cast on a wildcard as such [*[*]]");
                            }
                            stack.Peek().Children.Add(child);
                        }
                        break;
                    default:
                        stack.Peek().PropertyName += c;
                        break;
                }
            }
            return stack.Pop().Children;
        }

        /// <summary>
        /// A function to validate and apply the attribute level or mapping level defaults for a projected entity
        /// </summary>
        /// <param name="parsedDefaultIncludes"></param>
        /// <param name="destTypeInfo"></param>
        /// <returns></returns>
        public static string CompareAndConstructDefaultIncludes(List<PropertyReference> parsedDefaultIncludes, TypeInfo destTypeInfo)
        {
            // Create a variable to allow looping through adding all the defaultIncludes properties that are tagged
            var parsedDefaultIncludesHolder = new List<PropertyReference> { };
            var baseDefaultIncludesHolder = new List<PropertyReference> { };
            parsedDefaultIncludesHolder.AddRange(parsedDefaultIncludes);

            // Look to see if there is a base class with default includes
            // TODO: This does potentially introduce a slight issue in that technically the base class does not actually have to be mapped
            // and there could be arguments that maybe that is a good thing or a bad thing. 10/20/2017 AB
            baseDefaultIncludesHolder.AddRange(DeconstructDefaultIncludes(new List<PropertyReference>(), destTypeInfo.BaseType.GetTypeInfo()));

            // Deconstruct the main object in target
            parsedDefaultIncludesHolder = DeconstructDefaultIncludes(parsedDefaultIncludesHolder, destTypeInfo, baseDefaultIncludesHolder);

            // Combine the base and subclass results as necessary
            if (baseDefaultIncludesHolder.Count != 0)
            {
                parsedDefaultIncludesHolder.AddRange(baseDefaultIncludesHolder);
            }

            // Handle no defaults
            if (parsedDefaultIncludesHolder.Count == 0)
            {
                return "[]";
            }
            else
            {
                // construct the proper result
                string result = String.Join(",", parsedDefaultIncludesHolder.Select(m => m.PropertyName));
                result = result.Insert(0, "[").Insert(result.Length + 1, "]");

                return result;
            }
        }

        /// <summary>
        /// A function to specifically deconstruct the default includes off of a type info
        /// </summary>
        /// <param name="parsedDefaultIncludes"></param>
        /// <param name="destTypeInfo"></param>
        /// <returns></returns>
        public static List<PropertyReference> DeconstructDefaultIncludes(List<PropertyReference> parsedDefaultIncludes, TypeInfo destTypeInfo, List<PropertyReference> baseClassDefaultIncludes = null)
        {
            // Check to ensure that there are no passed in parsedDefaultIncludes should there be declarations on a base class
            if (parsedDefaultIncludes.Count != 0 && baseClassDefaultIncludes.Count != 0)
            {
                throw new MultipleDefaultsException($"Defaults are declared for {destTypeInfo.Name} in the configuration mapping and on the projection attributes of its base class.");
            }

            // Create a variable to allow looping through adding all the defaultIncludes properties that are tagged
            var parsedDefaultIncludesHolder = new List<PropertyReference> { };
            parsedDefaultIncludesHolder.AddRange(parsedDefaultIncludes);

            // Loop through each property on an entity to see if anything is declared to IncludeByDefault
            foreach (PropertyInfo propertyInfo in destTypeInfo.DeclaredProperties)
            {
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

                        // We don't want to allow a user to set defaults in both the mapping and at the attribute level
                        if (type == typeof(IncludeByDefault) && parsedDefaultIncludes.Count != 0)
                        {
                            throw new MultipleDefaultsException($"Defaults are declared for {destTypeInfo.Name} in the configuration mapping and on the projection attributes.");
                        }
                        if (type == typeof(IncludeByDefault))
                        {
                            parsedDefaultIncludesHolder.Add(new PropertyReference { PropertyName = propertyInfo.Name });
                        }
                    }
                }
            }

            return parsedDefaultIncludesHolder;
        }

    }
}
