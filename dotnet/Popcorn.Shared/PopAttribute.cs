
namespace Popcorn
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AlwaysAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NeverAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DefaultAttribute : Attribute { }
}