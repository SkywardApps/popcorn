using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class ErrorHandlingTests
    {
        [Fact]
        public async Task UnknownIncludeProperty_IsSilentlyIgnored()
        {
            var model = new ErrorHandlingModel { Id = 1, Name = "thing" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[Id,Name,Nonexistent]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("Id"));
            Assert.True(data.HasProperty("Name"));
            Assert.False(data.HasProperty("Nonexistent"));
        }

        [Fact]
        public async Task NullNestedReference_EmitsNull_WhenIncluded()
        {
            var model = new ErrorHandlingModel { Id = 1, Name = "thing", Nested = null };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("Nested"));
            Assert.Equal(JsonValueKind.Null, data.GetProperty("Nested").ValueKind);
        }

        [Fact]
        public async Task NonNullNestedReference_EmitsObject_WhenIncluded()
        {
            var model = new ErrorHandlingModel { Id = 1, Name = "thing", Nested = new NestedReference { Label = "inner" } };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[Nested[Label]]");
            var nested = doc.GetData().GetProperty("Nested");

            Assert.Equal(JsonValueKind.Object, nested.ValueKind);
            Assert.Equal("inner", nested.GetProperty("Label").GetString());
        }

        [Fact]
        public async Task MalformedInclude_GracefullyFallsBackToDefault()
        {
            var model = new ErrorHandlingModel { Id = 1, Name = "thing" };
            using var server = TestServerHelper.CreateServer(model);
            // Unterminated bracket — parser should not throw; should fall back to default behavior.
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[Id,Name");
            var data = doc.GetData();

            Assert.True(data.HasProperty("Id"));
            Assert.True(data.HasProperty("Name"));
        }

        [Fact]
        public async Task MissingIncludeParameter_UsesDefaultBehavior()
        {
            var model = new ErrorHandlingModel { Id = 1, Name = "thing" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test");
            var data = doc.GetData();

            Assert.True(data.HasProperty("Id"));
            Assert.True(data.HasProperty("Name"));
        }

        [Fact]
        public async Task SerializationException_ProducesErrorEnvelope()
        {
            var model = new ExplodingModel { Id = 42 };
            using var server = TestServerHelper.CreateServerWithWritePathException(model);
            var response = await server.CreateClient().GetAsync("/test?include=[!all]");

            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("exploded during serialization",
                doc.RootElement.GetProperty("Error").GetProperty("Message").GetString());
        }
    }
}
