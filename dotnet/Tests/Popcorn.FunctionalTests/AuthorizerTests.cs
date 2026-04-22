using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 authorization (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - IPopcornAuthorizer<T> { bool AuthorizeInclude(T, string, object?); bool AuthorizeItem(T); }
    //   - services.AddPopcornAuthorizer<T, TAuthorizer>();
    //   - Generator-emitted converter resolves the authorizer once per request via IServiceProvider and
    //     gates item emission (AuthorizeItem) and property emission (AuthorizeInclude).
    public class AuthorizerTests
    {
        private static OwnedResourceList Sample() => new()
        {
            Resources = new()
            {
                new() { Id = 1, OwnerId = "alice", Title = "Alice's doc" },
                new() { Id = 2, OwnerId = "bob", Title = "Bob's doc" },
                new() { Id = 3, OwnerId = "alice", Title = "Alice's other doc" },
            },
        };

        [Fact(Skip = "Pending: authorizers not yet implemented. See apiDesign.md.")]
        public async Task Authorizer_ItemGate_FiltersOutUnauthorizedItems()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            // Given a registered IPopcornAuthorizer<OwnedResource> that only permits OwnerId="alice",
            // only Alice's resources should appear.
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var items = doc.GetData().GetProperty("Resources");

            Assert.Equal(2, items.GetArrayLength());
            foreach (var i in items.EnumerateArray())
                Assert.Equal("alice", i.GetProperty("OwnerId").GetString());
        }

        [Fact(Skip = "Pending: authorizers. Per-property gate.")]
        public async Task Authorizer_PropertyGate_FiltersOutUnauthorizedProperties()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            // Given a registered authorizer that disallows Title when the caller isn't the owner,
            // the Title property should be absent for non-matching items (or the item filtered entirely).
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var items = doc.GetData().GetProperty("Resources");

            foreach (var i in items.EnumerateArray())
            {
                if (i.GetProperty("OwnerId").GetString() != "alice")
                {
                    Assert.False(i.HasProperty("Title"));
                }
            }
        }

        [Fact(Skip = "Pending: authorizers. No authorizer registered → no filtering.")]
        public async Task Authorizer_NotRegistered_NoFiltering()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var items = doc.GetData().GetProperty("Resources");

            Assert.Equal(3, items.GetArrayLength());
        }

        [Fact(Skip = "Pending: authorizers. Authorizer receives DI services.")]
        public async Task Authorizer_ReceivesDIServices()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: authorizer implementations should be able to take DI services (e.g. ICurrentUserService) via constructor injection.");
        }

        [Fact(Skip = "Pending: authorizers. Authorizer resolved once per request, not per item.")]
        public async Task Authorizer_ResolvedOncePerRequest()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: generator-emitted converter should cache the authorizer instance for the request duration to avoid N DI resolutions per collection.");
        }
    }
}
