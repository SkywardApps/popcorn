﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Skyward.Popcorn
{
#nullable enable
    /// <summary>
    /// This describes a property to be referenced (by name) and an optional (recursive) list of properties to be referenced on the 
    /// property's value if it is a complex type.
    /// </summary>
    public class PropertyReference
    {
        public PropertyReference(string propertyName, bool optional)
        {
            PropertyName = propertyName;
            Optional = optional;
        }

        /// <summary>
        /// The name of the property
        /// </summary>
        public string PropertyName { get; set; }

        public bool Optional { get; set; } = false;

        /// <summary>
        /// Any properties to reference on this property's value
        /// </summary>
        public List<PropertyReference> Children { get; set;  } = new List<PropertyReference>();

        /// <summary>
        /// Helper method to take a string in the format "[name[subname1, subname2, ...],...]" and create a PropertyReference tree out of it.
        /// </summary>
        /// <param name="includes"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyReference> Parse(string includes)
        {
            var stack = new Stack<PropertyReference>();
            stack.Push(new PropertyReference("", false)); // the root

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
                        stack.Push(new PropertyReference("", false));
                        break;
                    // finished a reference, so add it as a child and start a peer
                    case ',':
                        child = stack.Pop();
                        if (!String.IsNullOrWhiteSpace(child.PropertyName))
                        {
                            var propName = child.PropertyName.TrimStart(' ').TrimEnd(' ');
                            if (propName.StartsWith("?"))
                            {
                                child.Optional = true;
                                propName = propName.Substring(1);
                            }
                            child.PropertyName = propName;
                            stack.Peek().Children.Add(child);
                        }
                        stack.Push(new PropertyReference("", false));
                        break;
                    // Completed a child list, so add the last item as a child and pop up the stack
                    case ']':
                        child = stack.Pop();
                        if (!String.IsNullOrWhiteSpace(child.PropertyName))
                        {
                            var propName = child.PropertyName.TrimStart(' ').TrimEnd(' ');
                            if (propName.StartsWith("?"))
                            {
                                child.Optional = true;
                                propName = propName.Substring(1);
                            }
                            child.PropertyName = propName;
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
    }
}
