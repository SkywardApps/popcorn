#nullable enable
using System.Net.Http;
using System.Text.Json;

namespace Popcorn.Shared
{
    public record class ApiResponse<T>
    {
        public static implicit operator ApiResponse<T>(Pop<T> data) => new(data);

        public ApiResponse(Pop<T> data)
        {
            Data = data;
        }

        public ApiResponse(IReadOnlyList<PropertyReference> props, T data)
        {
            Data = new Pop<T> { Data = data, PropertyReferences = props };
        }

        public Pop<T> Data { get; init; }
    }
}
