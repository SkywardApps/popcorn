using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 filtering (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - `[Filterable(FilterOps)]` attribute opts properties in and declares supported operators.
    //   - `?filter=[Field:op:value,Field:op:value]` bracket grammar (mirrors ?include=).
    //   - Operators: eq, ne, gt, gte, lt, lte, contains, startsWith, endsWith, in.
    //   - Generator emits switch(filterField) → typed predicate dispatch. No expression evaluation (AOT-safe).
    public class FilteringTests
    {
        private static SortableCollection Sample() => new()
        {
            Items = new()
            {
                new() { Id = 1, Name = "Firebird", Year = 1981, Category = "Muscle" },
                new() { Id = 2, Name = "250 GTO", Year = 1962, Category = "Classic" },
                new() { Id = 3, Name = "Cayman", Year = 2005, Category = "Coupe" },
                new() { Id = 4, Name = "Camry", Year = 2024, Category = "Sedan" },
            },
        };

        [Fact(Skip = "Pending: filtering not yet implemented. See apiDesign.md.")]
        public async Task Filter_EqualsOperator_ReturnsMatchingItems()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?filter=[Year:eq:1981]");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal("Firebird", items[0].GetProperty("Name").GetString());
        }

        [Fact(Skip = "Pending: filtering.")]
        public async Task Filter_GreaterThanOperator_ReturnsMatchingItems()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?filter=[Year:gt:2000]");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(2, items.GetArrayLength());
        }

        [Fact(Skip = "Pending: filtering.")]
        public async Task Filter_LessThanOrEqualOperator_ReturnsMatchingItems()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?filter=[Year:lte:1981]");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(2, items.GetArrayLength());
        }

        [Fact(Skip = "Pending: filtering. String contains operator (case-sensitive).")]
        public async Task Filter_ContainsOperator_MatchesSubstring()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?filter=[Name:contains:Cam]");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal("Camry", items[0].GetProperty("Name").GetString());
        }

        [Fact(Skip = "Pending: filtering. Multiple filters combine via AND.")]
        public async Task Filter_MultipleFilters_ConjunctiveMatch()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?filter=[Year:gt:1970,Year:lt:2000]");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal("Firebird", items[0].GetProperty("Name").GetString());
        }

        [Fact(Skip = "Pending: filtering. Non-[Filterable] property rejected (HTTP 400) or ignored.")]
        public async Task Filter_NonFilterableProperty_RejectedOrIgnored()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var response = await server.CreateClient().GetAsync("/test?filter=[Category:eq:Sedan]");
            Assert.True((int)response.StatusCode == 400 || response.IsSuccessStatusCode);
        }

        [Fact(Skip = "Pending: filtering. Unsupported operator rejected with diagnostic.")]
        public async Task Filter_UnsupportedOperator_RejectedOrIgnored()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var response = await server.CreateClient().GetAsync("/test?filter=[Name:regex:.*]");
            Assert.True((int)response.StatusCode == 400 || response.IsSuccessStatusCode);
        }

        [Fact(Skip = "Pending: filtering. Filter combined with sort + pagination.")]
        public async Task Filter_WithSortAndPagination_AppliesInOrder()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?filter=[Year:gt:1970]&sort=Year&pageSize=2&page=1");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(2, items.GetArrayLength());
            Assert.Equal("Firebird", items[0].GetProperty("Name").GetString());
            Assert.Equal("Cayman", items[1].GetProperty("Name").GetString());
        }
    }
}
