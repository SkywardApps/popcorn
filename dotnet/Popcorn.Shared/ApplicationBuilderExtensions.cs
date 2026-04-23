#nullable enable
using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Popcorn.Shared
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Catches unhandled exceptions in the pipeline and rewrites the response as a structured Popcorn
        /// error envelope (the default <see cref="ApiResponse{T}"/> shape, or the custom shape registered
        /// via <c>[PopcornEnvelope]</c> + <see cref="PopcornOptions.EnvelopeType"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The middleware buffers the response body so it can replace it on exception. This means:
        /// (1) responses will not stream to the client as the handler writes them — they are flushed once
        ///     the handler returns successfully; and
        /// (2) every response through the middleware incurs memory proportional to its size.
        /// </para>
        /// <para>
        /// Scope this middleware to Popcorn endpoints (bounded JSON envelopes). Do not place it in the
        /// pipeline upstream of streaming endpoints (SSE, chunked downloads, long-polling).
        /// </para>
        /// <para>
        /// Headers set by an inner middleware or the endpoint before the exception are preserved on the
        /// error response (except <c>Content-Length</c>, which is reset by the replacement body, and
        /// <c>Content-Type</c>, which this middleware overwrites to <c>application/json</c>). Application
        /// concerns like auth cookies or request-id headers therefore survive. If you need to strip
        /// additional headers on error paths, place a <see cref="IHttpResponseBodyFeature"/>-aware
        /// middleware upstream, or call <c>ctx.Response.Headers.Remove(...)</c> from a custom handler.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UsePopcornExceptionHandler(this IApplicationBuilder app)
        {
            return app.Use(async (ctx, next) =>
            {
                var originalBody = ctx.Response.Body;
                using var buffer = new MemoryStream();
                ctx.Response.Body = buffer;

                try
                {
                    await next();
                    ctx.Response.Body = originalBody;
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody);
                }
                catch (Exception ex)
                {
                    ctx.Response.Body = originalBody;

                    if (ctx.Response.HasStarted)
                    {
                        throw;
                    }

                    var popcornOptions = ctx.RequestServices.GetService<PopcornOptions>();
                    var envelopeType = popcornOptions?.EnvelopeType ?? typeof(ApiResponse<>);
                    var namingPolicy = popcornOptions?.DefaultNamingPolicy;

                    // Reset response framing: remove Content-Length (replacement body will recompute it)
                    // and explicitly set the status + content type for the error envelope.
                    ctx.Response.Headers.Remove("Content-Length");
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    ctx.Response.ContentType = "application/json; charset=utf-8";

                    var error = new ApiError(ex.GetType().Name, ex.Message ?? string.Empty);

                    using var payload = new MemoryStream();
                    using (var writer = new Utf8JsonWriter(payload))
                    {
                        var wroteCustom = envelopeType != typeof(ApiResponse<>)
                            && PopcornErrorWriterRegistry.TryWrite(writer, envelopeType, error, namingPolicy);
                        if (!wroteCustom)
                        {
                            WriteDefaultErrorEnvelope(writer, error, namingPolicy);
                        }
                    }
                    payload.Position = 0;
                    await payload.CopyToAsync(ctx.Response.Body);
                }
            });
        }

        private static void WriteDefaultErrorEnvelope(Utf8JsonWriter writer, ApiError error, JsonNamingPolicy? namingPolicy)
        {
            string Convert(string n) => namingPolicy?.ConvertName(n) ?? n;

            writer.WriteStartObject();
            writer.WriteBoolean(Convert("Success"), false);
            writer.WriteStartObject(Convert("Error"));
            writer.WriteString(Convert("Code"), error.Code);
            writer.WriteString(Convert("Message"), error.Message);
            if (error.Detail is not null)
            {
                writer.WriteString(Convert("Detail"), error.Detail);
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}
