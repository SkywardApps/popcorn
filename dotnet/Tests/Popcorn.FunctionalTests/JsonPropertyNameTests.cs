using System;
using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // Contract: the wire name (from [JsonPropertyName] if present, else the C# name) IS the API.
    // Clients see the wire name in responses and request fields by wire name in ?include=.
    // The C# name is an implementation detail the client has no visibility into.
    public class JsonPropertyNameTests
    {
        private static JsonPropertyNameModel Sample() => new()
        {
            DisplayName = "Liz Lemon",
            CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ExternalId = "ext-42",
            InternalName = "liz.lemon",
        };

        [Fact]
        public async Task JsonPropertyName_EmittedUsingMappedName()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("display_name"));
            Assert.True(data.HasProperty("created_at"));
            Assert.True(data.HasProperty("external_id"));
        }

        [Fact]
        public async Task JsonPropertyName_CSharpNameNotEmitted_WhenMapped()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.False(data.HasProperty("DisplayName"));
            Assert.False(data.HasProperty("CreatedAt"));
            Assert.False(data.HasProperty("ExternalId"));
        }

        [Fact]
        public async Task JsonPropertyName_IncludeMatchesWireName()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[display_name]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("display_name"));
            Assert.Equal("Liz Lemon", data.GetProperty("display_name").GetString());
        }

        [Fact]
        public async Task JsonPropertyName_IncludeByCSharpName_DoesNotMatch()
        {
            // The C# name is not part of the API contract — clients only see the wire name.
            // Requesting by C# name must not emit the property (it's effectively an unknown include).
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[DisplayName]");
            var data = doc.GetData();

            Assert.False(data.HasProperty("display_name"));
            Assert.False(data.HasProperty("DisplayName"));
        }

        [Fact]
        public async Task JsonPropertyName_DefaultAttributeRespectsMappedOutput()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("display_name"));
            Assert.True(data.HasProperty("external_id"));
            Assert.False(data.HasProperty("created_at"));
        }

        [Fact]
        public async Task JsonPropertyName_AlwaysAttributeStillApplies()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[InternalName]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("external_id"));
        }

        [Fact]
        public async Task JsonPropertyName_ValueIsCorrect_WhenRequestedByWireName()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[display_name]");
            var value = doc.GetData().GetProperty("display_name");

            Assert.Equal(JsonValueKind.String, value.ValueKind);
            Assert.Equal("Liz Lemon", value.GetString());
        }

        [Fact]
        public async Task JsonPropertyName_NegationUsesWireName()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all,-display_name]");
            var data = doc.GetData();

            Assert.False(data.HasProperty("display_name"));
            Assert.True(data.HasProperty("created_at"));
            Assert.True(data.HasProperty("external_id"));
        }

        [Fact]
        public async Task JsonPropertyName_NestedIncludeUsesWireName()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            // created_at is not [Default], so it only appears when explicitly requested by wire name.
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[created_at,display_name]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("created_at"));
            Assert.True(data.HasProperty("display_name"));
        }
    }
}
