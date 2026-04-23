using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Popcorn.Shared;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 custom response envelope (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - Default envelope: ApiResponse<T> { Success, Data: Pop<T>, Error? }
    //   - Custom envelope via [PopcornEnvelope] on a user type + services.AddPopcorn(o => o.EnvelopeType = typeof(...))
    //   - Slots bound via marker attributes: [PopcornPayload], [PopcornError], [PopcornSuccess].
    //   - Exception → Envelope.Success=false + Error populated, via UsePopcornExceptionHandler() middleware.
    //   - One envelope per application (multi-envelope out of scope).
    public class CustomEnvelopeTests
    {
        [Fact]
        public async Task DefaultEnvelope_Error_EmittedWhenExceptionThrown()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler();
            var response = await server.CreateClient().GetAsync("/test");

            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
            var error = doc.RootElement.GetProperty("Error");
            Assert.Equal("InvalidOperationException", error.GetProperty("Code").GetString());
            Assert.Equal("boom", error.GetProperty("Message").GetString());
        }

        [Fact]
        public async Task CustomEnvelope_ReplacesDefaultShape()
        {
            var payload = new EnvelopePayload { Id = 7, Name = "seven" };
            using var server = TestServerHelper.CreateServerWithCustomEnvelope(
                payload,
                pop => new MyTestEnvelope<EnvelopePayload> { Ok = true, Payload = pop });

            var response = await server.CreateClient().GetAsync("/test?include=[Id,Name]");
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Ok", out _));
            Assert.True(root.TryGetProperty("Payload", out _));
            Assert.False(root.TryGetProperty("Success", out _));
            Assert.False(root.TryGetProperty("Data", out _));
        }

        [Fact]
        public async Task CustomEnvelope_WrapsPayloadWithUserFields()
        {
            var payload = new EnvelopePayload { Id = 11, Name = "eleven" };
            using var server = TestServerHelper.CreateServerWithCustomEnvelope(
                payload,
                pop => new MyTestEnvelope<EnvelopePayload>
                {
                    Ok = true,
                    Payload = pop,
                    Messages = new() { "ready" },
                });

            var response = await server.CreateClient().GetAsync("/test?include=[Id,Name]");
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.True(root.GetProperty("Ok").GetBoolean());
            var inner = root.GetProperty("Payload");
            Assert.Equal(11, inner.GetProperty("Id").GetInt32());
            Assert.Equal("eleven", inner.GetProperty("Name").GetString());
            Assert.Equal("ready", root.GetProperty("Messages")[0].GetString());
        }

        [Fact]
        public async Task CustomEnvelope_ExceptionHandler_UsesCustomShape()
        {
            using var server = TestServerHelper.CreateServerWithThrowingHandler(
                configurePopcorn: o => o.EnvelopeType = typeof(MyTestEnvelope<>));
            var response = await server.CreateClient().GetAsync("/test");

            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.False(root.GetProperty("Ok").GetBoolean());
            Assert.Equal("InvalidOperationException", root.GetProperty("Problem").GetProperty("Code").GetString());
            Assert.Equal("boom", root.GetProperty("Problem").GetProperty("Message").GetString());
            Assert.False(root.TryGetProperty("Success", out _));
            Assert.False(root.TryGetProperty("Error", out _));
        }
    }
}
