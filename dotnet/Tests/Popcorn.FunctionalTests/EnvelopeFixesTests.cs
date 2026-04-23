using System;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Popcorn.FunctionalTests.Models;
using Popcorn.Shared;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // Coverage for post-review envelope fixes. Each test corresponds to a specific item in the review.
    public class EnvelopeFixesTests
    {
        // Covers: ApiResponse<T>.FromError factory produces a Success=false, Error-populated envelope
        // that serializes to the default error shape.
        [Fact]
        public void FromError_ProducesErrorShape_WhenSerialized()
        {
            var response = ApiResponse<EnvelopePayload>.FromError(new ApiError("X", "bad", "detail"));
            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
            options.AddPopcornOptions();

            var json = JsonSerializer.Serialize(response, options);
            using var doc = JsonDocument.Parse(json);

            Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("X", doc.RootElement.GetProperty("Error").GetProperty("Code").GetString());
            Assert.Equal("bad", doc.RootElement.GetProperty("Error").GetProperty("Message").GetString());
            Assert.Equal("detail", doc.RootElement.GetProperty("Error").GetProperty("Detail").GetString());
        }

        // Covers: AddPopcorn is idempotent — repeated calls produce a single PopcornOptions singleton.
        [Fact]
        public void AddPopcorn_IsIdempotent_SingleOptionsSingleton()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddPopcorn();
            services.AddPopcorn(o => o.EnvelopeType = typeof(MyTestEnvelope<>));
            services.AddPopcorn(o => o.DefaultNamingPolicy = JsonNamingPolicy.CamelCase);

            using var provider = services.BuildServiceProvider();
            var allOptions = provider.GetServices<PopcornOptions>();
            Assert.Single(allOptions);

            var opts = provider.GetRequiredService<PopcornOptions>();
            Assert.Equal(typeof(MyTestEnvelope<>), opts.EnvelopeType);
            Assert.Same(JsonNamingPolicy.CamelCase, opts.DefaultNamingPolicy);
        }

        // Covers: JSON naming policy is applied to the default error envelope shape.
        [Fact]
        public async Task DefaultEnvelope_Error_AppliesNamingPolicy()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.DefaultNamingPolicy = JsonNamingPolicy.CamelCase);
            var response = await server.CreateClient().GetAsync("/test");

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.False(root.GetProperty("success").GetBoolean());
            Assert.Equal("InvalidOperationException", root.GetProperty("error").GetProperty("code").GetString());
            Assert.False(root.TryGetProperty("Success", out _));
            Assert.False(root.TryGetProperty("Error", out _));
        }

        // Covers: JSON naming policy is applied to the custom envelope shape emitted by the generator.
        // Envelope's declared field names ("Ok", "Problem") are policy-converted at write time.
        [Fact]
        public async Task CustomEnvelope_Error_AppliesNamingPolicy()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o =>
                {
                    o.EnvelopeType = typeof(MyTestEnvelope<>);
                    o.DefaultNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            var response = await server.CreateClient().GetAsync("/test");

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.False(root.GetProperty("ok").GetBoolean());
            var problem = root.GetProperty("problem");
            Assert.Equal("InvalidOperationException", problem.GetProperty("code").GetString());
            Assert.Equal("boom", problem.GetProperty("message").GetString());
            Assert.False(root.TryGetProperty("Ok", out _));
            Assert.False(root.TryGetProperty("Problem", out _));
        }

        // Covers: middleware strips Content-Length set by an aborted inner handler so the replacement
        // body is not truncated or padded.
        [Fact]
        public async Task Middleware_OnException_StripsContentLengthFromAbortedHandler()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                beforeThrow: ctx => ctx.Response.Headers["Content-Length"] = "9999");
            var response = await server.CreateClient().GetAsync("/test");

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            // The body parses — if Content-Length was stale the TestServer would have truncated.
            using var doc = JsonDocument.Parse(body);
            Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
            if (response.Content.Headers.ContentLength.HasValue)
            {
                Assert.Equal(body.Length, response.Content.Headers.ContentLength.Value);
            }
        }

        // Covers: middleware preserves custom response headers set before the exception (not Content-Type,
        // which the middleware must overwrite; and not Content-Length, which we explicitly strip).
        [Fact]
        public async Task Middleware_OnException_PreservesCustomHeaders()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                beforeThrow: ctx => ctx.Response.Headers["X-Request-Id"] = "abc-123");
            var response = await server.CreateClient().GetAsync("/test");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("X-Request-Id", out var values));
            Assert.Equal("abc-123", Assert.Single(values));
        }

        // Covers: AddPopcornEnvelopes() registers the custom writer at DI time — the exception path
        // does not require AddPopcornOptions to have been called previously.
        [Fact]
        public async Task AddPopcornEnvelopes_AloneIsEnough_ForCustomErrorShape()
        {
            // CreateServerWithThrowingHandler uses AddPopcornEnvelopes() internally and never calls
            // AddPopcornOptions on any JsonSerializerOptions (the endpoint throws before serialization).
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(MyTestEnvelope<>));
            var response = await server.CreateClient().GetAsync("/test");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(doc.RootElement.GetProperty("Ok").GetBoolean());
            Assert.Equal("boom", doc.RootElement.GetProperty("Problem").GetProperty("Message").GetString());
        }

        // Covers: envelope markers declared on a base class are honored by the generator (inheritance walker).
        [Fact]
        public async Task DerivedEnvelope_InheritsBaseMarkers_ForErrorWriter()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(DerivedEnvelope<>));
            var response = await server.CreateClient().GetAsync("/test");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            // Success marker comes from the base class; payload marker from the derived class.
            Assert.False(root.GetProperty("Okay").GetBoolean());
            Assert.Equal("InvalidOperationException", root.GetProperty("Mishap").GetProperty("Code").GetString());
        }

        // Covers: an envelope nested inside a non-generic outer type is supported — the generator emits
        // the correct Outer.Inner<> typeof syntax.
        [Fact]
        public async Task NestedEnvelope_InsideNonGenericOuter_IsDispatched()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(NestedEnvelopeContainer.NestedEnvelope<>));
            var response = await server.CreateClient().GetAsync("/test");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            Assert.False(root.GetProperty("Ok").GetBoolean());
            Assert.Equal("boom", root.GetProperty("Fault").GetProperty("Message").GetString());
        }
    }
}
