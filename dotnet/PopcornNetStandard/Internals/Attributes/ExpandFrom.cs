﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Skyward.Popcorn
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExpandFromAttribute : Attribute
    {
        Type source;
        string includes;

        public ExpandFromAttribute(Type source, string includes)
        {
            this.source = source;
            this.includes = includes;
        }

        public Type SourceType { get { return source; } }
        public string Includes { get { return includes; } }
    }
}
