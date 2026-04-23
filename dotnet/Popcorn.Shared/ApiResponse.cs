#nullable enable
using System.Text.Json.Serialization;

namespace Popcorn.Shared
{
    public record class ApiResponse<T>
    {
        public static implicit operator ApiResponse<T>(Pop<T> data) => new(data);

        /// <summary>
        /// Internal parameterless constructor reserved for <see cref="FromError"/> and record copy
        /// semantics. External callers must construct an <see cref="ApiResponse{T}"/> via the
        /// <see cref="Pop{T}"/> overload, the <see cref="PropertyReference"/> overload, or
        /// <see cref="FromError"/>, so the "success with no data" shape cannot be constructed accidentally.
        /// </summary>
        internal ApiResponse() { }

        public ApiResponse(Pop<T> data)
        {
            Data = data;
        }

        public ApiResponse(IReadOnlyList<PropertyReference> props, T data)
        {
            Data = new Pop<T> { Data = data, PropertyReferences = props };
        }

        public static ApiResponse<T> FromError(ApiError error) => new()
        {
            Success = false,
            Error = error,
        };

        public bool Success { get; init; } = true;

        public Pop<T> Data { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiError? Error { get; init; }
    }
}
