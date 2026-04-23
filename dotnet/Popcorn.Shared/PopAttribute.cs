
namespace Popcorn
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AlwaysAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NeverAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DefaultAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class SubPropertyDefaultAttribute : Attribute
    {
        public SubPropertyDefaultAttribute(string includes) { Includes = includes; }
        public string Includes { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class PopcornEnvelopeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PopcornPayloadAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PopcornErrorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PopcornSuccessAttribute : Attribute { }
}