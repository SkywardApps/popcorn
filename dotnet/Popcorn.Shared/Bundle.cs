using System.Collections.Immutable;

#nullable enable
namespace Popcorn.Shared
{
    public record struct Bundle<T>
    {
        public ImmutableArray<PropertyReference> PropertyReferences { get; init; }
        public T Data { get; init;  }
    }
}
