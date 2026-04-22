using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class GenericTypeTests
    {
        private static PageModel<ItemPayload> Sample() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Items = new()
            {
                new() { Id = 1, Name = "first", Secret = "hidden1" },
                new() { Id = 2, Name = "second", Secret = "hidden2" },
            },
            TotalCount = 42,
        };

        [Fact]
        public async Task GenericWrapper_DefaultInclude_EmitsItemsAndMetadata()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("PageNumber"));
            Assert.True(data.HasProperty("PageSize"));
            Assert.True(data.HasProperty("Items"));
            Assert.True(data.HasProperty("TotalCount"));
        }

        [Fact]
        public async Task GenericWrapper_NestedInclude_AppliesToInnerType()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[Items[Id]]");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(2, items.GetArrayLength());
            Assert.True(items[0].HasProperty("Id"));
            Assert.False(items[0].HasProperty("Name"));
            Assert.False(items[0].HasProperty("Secret"));
        }

        [Fact]
        public async Task GenericWrapper_NestedAllInclude_EmitsAllInnerProperties()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[Items[!all]]");
            var items = doc.GetData().GetProperty("Items");

            Assert.True(items[0].HasProperty("Id"));
            Assert.True(items[0].HasProperty("Name"));
            Assert.True(items[0].HasProperty("Secret"));
        }

        [Fact]
        public async Task GenericWrapper_AlwaysAttributeAppliedOnOuterType()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[TotalCount]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("PageNumber"));
            Assert.True(data.HasProperty("PageSize"));
            Assert.True(data.HasProperty("TotalCount"));
            Assert.False(data.HasProperty("Items"));
        }
    }
}
