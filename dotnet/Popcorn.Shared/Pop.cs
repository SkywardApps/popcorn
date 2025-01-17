using System.Collections.Immutable;

#nullable enable
namespace Popcorn.Shared
{
    public record struct Pop<T>
    {
        public IReadOnlyList<PropertyReference> PropertyReferences { get; init; }
        public T Data { get; init;  }
    }
}
