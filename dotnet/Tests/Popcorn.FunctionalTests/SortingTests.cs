using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 sorting (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - `[Sortable]` attribute opts properties into sort-by-name.
    //   - `?sort=Field&sortDirection=Ascending|Descending` query params.
    //   - Generator emits switch(sortField) → typed Comparer<T>.Default.Compare dispatch.
    //   - Applied to top-level collection responses only (matching legacy scope).
    public class SortingTests
    {
        private static SortableCollection Sample() => new()
        {
            Items = new()
            {
                new() { Id = 3, Name = "Cayman", Year = 2005, Category = "Coupe" },
                new() { Id = 1, Name = "Firebird", Year = 1981, Category = "Muscle" },
                new() { Id = 2, Name = "250 GTO", Year = 1962, Category = "Classic" },
            },
        };

        [Fact(Skip = "Pending: sorting not yet implemented. See apiDesign.md — [Sortable] attribute + ?sort= query param + generator-emitted typed comparator dispatch.")]
        public async Task Sort_ByStringProperty_AscendingByDefault()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?sort=Name");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal("250 GTO", items[0].GetProperty("Name").GetString());
            Assert.Equal("Cayman", items[1].GetProperty("Name").GetString());
            Assert.Equal("Firebird", items[2].GetProperty("Name").GetString());
        }

        [Fact(Skip = "Pending: sorting.")]
        public async Task Sort_ByStringProperty_Descending()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?sort=Name&sortDirection=Descending");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal("Firebird", items[0].GetProperty("Name").GetString());
        }

        [Fact(Skip = "Pending: sorting.")]
        public async Task Sort_ByNumericProperty_AscendingByDefault()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?sort=Year");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(1962, items[0].GetProperty("Year").GetInt32());
            Assert.Equal(1981, items[1].GetProperty("Year").GetInt32());
            Assert.Equal(2005, items[2].GetProperty("Year").GetInt32());
        }

        [Fact(Skip = "Pending: sorting. Non-[Sortable] property must be rejected (HTTP 400) or ignored with diagnostic in envelope.")]
        public async Task Sort_ByNonSortableProperty_RejectedOrIgnored()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var response = await server.CreateClient().GetAsync("/test?sort=Category");
            // Category is not [Sortable] — expected to either return 400 or silently ignore.
            Assert.True((int)response.StatusCode == 400 || response.IsSuccessStatusCode);
        }

        [Fact(Skip = "Pending: sorting. Unknown sort key must not throw.")]
        public async Task Sort_ByUnknownProperty_DoesNotThrow()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var response = await server.CreateClient().GetAsync("/test?sort=DoesNotExist");
            // Should not crash; either 400 or 200 with unsorted list.
            Assert.True((int)response.StatusCode == 400 || response.IsSuccessStatusCode);
        }

        [Fact(Skip = "Pending: sorting. No sort param → items in original order.")]
        public async Task Sort_NoSortParam_PreservesOrder()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(3, items[0].GetProperty("Id").GetInt32());
            Assert.Equal(1, items[1].GetProperty("Id").GetInt32());
            Assert.Equal(2, items[2].GetProperty("Id").GetInt32());
        }

        [Fact(Skip = "Pending: sorting. Invalid sortDirection treated as Ascending.")]
        public async Task Sort_InvalidDirection_FallsBackToAscending()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?sort=Year&sortDirection=Garbage");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(1962, items[0].GetProperty("Year").GetInt32());
        }
    }
}
