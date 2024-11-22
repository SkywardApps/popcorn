
namespace Popcorn
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PopAttribute : Attribute { 
        public PopAttribute(Type targetType) { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AlwaysAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NeverAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DefaultAttribute : Attribute { }
}