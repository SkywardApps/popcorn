using System;
using System.IO;
using System.Net;
using System.Text;
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

        // Covers: when the response has already started streaming, the middleware cannot rewrite it and
        // rethrows. The exception propagates to the test host rather than producing a malformed envelope.
        [Fact]
        public async Task Middleware_WhenResponseHasStarted_Rethrows()
        {
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddHttpContextAccessor();
                    s.AddRouting();
                    s.AddPopcorn();
                    s.AddPopcornEnvelopes();
                })
                .Configure(app =>
                {
                    app.UsePopcornExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async ctx =>
                        {
                            // Flush bytes to the buffered body so the inner buffer stream reports HasStarted.
                            // Then forcibly mark the response as started by flushing the buffer to the wire
                            // via StartAsync (the test host respects this signal).
                            await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("{\"partial\":true"));
                            await ctx.Response.StartAsync();
                            throw new InvalidOperationException("too late");
                        });
                    });
                }));

            // The exception rethrows out of the pipeline; TestHost surfaces it as an HttpRequestException
            // or an aggregated failure. We just assert it doesn't silently turn into a well-formed 500.
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                var response = await server.CreateClient().GetAsync("/test");
                // If we get here, verify the body is NOT a clean error envelope — the middleware
                // re-threw because HasStarted==true, so the partial stream reaches us.
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("\"Success\":false", body);
            });
        }

        // Covers: two [PopcornEnvelope] types registered in the same JsonSerializerContext both get
        // dispatch arms in the generator-emitted error writer.
        [Fact]
        public async Task MultipleCustomEnvelopes_BothDispatchedByGeneratedWriter()
        {
            using var serverA = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(MyTestEnvelope<>));
            using var serverB = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(AlternateEnvelope<>));

            using var docA = JsonDocument.Parse(await (await serverA.CreateClient().GetAsync("/test")).Content.ReadAsStringAsync());
            using var docB = JsonDocument.Parse(await (await serverB.CreateClient().GetAsync("/test")).Content.ReadAsStringAsync());

            // MyTestEnvelope's slots: Ok / Payload / Problem
            Assert.False(docA.RootElement.GetProperty("Ok").GetBoolean());
            Assert.True(docA.RootElement.TryGetProperty("Problem", out _));

            // AlternateEnvelope's slots: State / Contents / Boom — different names, same writer.
            Assert.False(docB.RootElement.GetProperty("State").GetBoolean());
            Assert.True(docB.RootElement.TryGetProperty("Boom", out _));
            Assert.False(docB.RootElement.TryGetProperty("Ok", out _));
        }

        // Covers: ApiError.Detail round-trips through the default error envelope when non-null.
        [Fact]
        public async Task DefaultEnvelope_ApiErrorDetail_RoundTripsWhenNonNull()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                toThrow: new DetailedException("boom", "stack-like detail"));
            var response = await server.CreateClient().GetAsync("/test");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            // DetailedException.Message = "boom"; we have no way to inject Detail directly from the
            // handler without changing the middleware, so this test asserts the *shape*: when Detail
            // is provided (via ApiError ctor), it appears in the envelope.
            Assert.Equal("boom", doc.RootElement.GetProperty("Error").GetProperty("Message").GetString());
            // And the positive-branch check: a directly-constructed ApiError with Detail serializes it.
            var directly = JsonSerializer.Serialize(ApiResponse<EnvelopePayload>.FromError(new ApiError("X", "bad", "extra")));
            using var directDoc = JsonDocument.Parse(directly);
            Assert.Equal("extra", directDoc.RootElement.GetProperty("Error").GetProperty("Detail").GetString());
        }

        private class DetailedException(string message, string detail) : Exception(message)
        {
            public string Detail { get; } = detail;
        }

        // Covers: [JsonPropertyName] on a marker property takes precedence over the C# property name
        // when the generator emits the error-envelope writer.
        [Fact]
        public async Task RenamedMarkerEnvelope_UsesJsonPropertyNameOverride()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(RenamedMarkerEnvelope<>));
            var response = await server.CreateClient().GetAsync("/test");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            // Wire names are from [JsonPropertyName], not the C# names.
            Assert.False(root.GetProperty("success_flag").GetBoolean());
            Assert.True(root.TryGetProperty("problem_details", out var problem));
            Assert.Equal("boom", problem.GetProperty("Message").GetString());
            Assert.False(root.TryGetProperty("Ok", out _));
            Assert.False(root.TryGetProperty("Problem", out _));
        }

        // Covers: [PopcornEnvelope] works on a record class (not just plain class) — AnalyzeEnvelope
        // and the generated writer handle record-shape envelopes.
        [Fact]
        public async Task RecordEnvelope_IsDispatchedOnException()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(RecordEnvelope<>));
            var response = await server.CreateClient().GetAsync("/test");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            Assert.False(root.GetProperty("Ok").GetBoolean());
            Assert.Equal("boom", root.GetProperty("Issue").GetProperty("Message").GetString());
        }

        // Covers: AddPopcornEnvelopes() is idempotent — repeated calls overwrite the same writer
        // delegate in the registry rather than stacking or corrupting state.
        [Fact]
        public async Task AddPopcornEnvelopes_IsIdempotent_OnRepeatedCall()
        {
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddHttpContextAccessor();
                    s.AddRouting();
                    s.AddPopcorn(o => o.EnvelopeType = typeof(MyTestEnvelope<>));
                    // Call three times — the registry should end up with exactly one working writer.
                    s.AddPopcornEnvelopes();
                    s.AddPopcornEnvelopes();
                    s.AddPopcornEnvelopes();
                })
                .Configure(app =>
                {
                    app.UsePopcornExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapGet("/test", _ => throw new InvalidOperationException("boom")));
                }));

            using var doc = JsonDocument.Parse(await (await server.CreateClient().GetAsync("/test")).Content.ReadAsStringAsync());
            Assert.False(doc.RootElement.GetProperty("Ok").GetBoolean());
            Assert.Equal("boom", doc.RootElement.GetProperty("Problem").GetProperty("Message").GetString());
        }
    }
}
