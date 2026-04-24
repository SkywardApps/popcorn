using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // v8 translators: plain C# computed properties. The DI-injecting [Translator] variant
    // was considered and dropped (2026-04-23) — see docs/MigrationV7toV8.md §5 for rationale
    // and the recommended endpoint-side resolution pattern.
    public class TranslatorTests
    {
        [Fact]
        public async Task ComputedProperty_EmittedAsNormalProperty()
        {
            var model = new PersonWithComputed { FirstName = "Liz", LastName = "Lemon" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FullName]");
            var fullName = doc.GetData().GetProperty("FullName");

            Assert.Equal("Liz Lemon", fullName.GetString());
        }

        [Fact]
        public async Task ComputedProperty_HonorsDefaultAttribute()
        {
            var model = new PersonWithComputed { FirstName = "Liz", LastName = "Lemon" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("FullName"));
            Assert.Equal("Liz Lemon", data.GetProperty("FullName").GetString());
        }

        [Fact]
        public async Task ComputedProperty_CanBeExcluded()
        {
            var model = new PersonWithComputed { FirstName = "Liz", LastName = "Lemon" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FirstName]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("FirstName"));
            Assert.False(data.HasProperty("FullName"));
        }

    }
}
