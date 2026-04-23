#nullable enable
using System;
using System.Text.Json;

namespace Popcorn.Shared
{
    public class PopcornOptions
    {
        public Type EnvelopeType { get; set; } = typeof(ApiResponse<>);

        /// <summary>
        /// Naming policy applied by <c>UsePopcornExceptionHandler</c> when it writes the error envelope
        /// (both the default <c>ApiResponse&lt;T&gt;</c> shape and any custom envelope registered via a
        /// generator-emitted writer). Match this to the policy you set on your
        /// <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> so error responses look like success responses.
        /// When <c>null</c>, property names are written verbatim.
        /// </summary>
        public JsonNamingPolicy? DefaultNamingPolicy { get; set; }
    }
}
