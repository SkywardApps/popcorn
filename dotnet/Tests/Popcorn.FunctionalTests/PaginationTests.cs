using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 pagination (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - `?page=N&pageSize=M` query params (page is 1-based).
    //   - Middleware applies Skip/Take to the top-level collection.
    //   - Envelope gains optional PageInfo { Page, PageSize, TotalItems, TotalPages }.
    //   - Default pageSize = 50, max TBD.
    public class PaginationTests
    {
        private static SortableCollection LargeSample()
        {
            var coll = new SortableCollection();
            for (int i = 1; i <= 25; i++)
            {
                coll.Items.Add(new SortableItem { Id = i, Name = $"Item{i}", Year = 2000 + i });
            }
            return coll;
        }

        [Fact(Skip = "Pending: pagination not yet implemented. See apiDesign.md.")]
        public async Task Pagination_FirstPage_ReturnsFirstSlice()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?page=1&pageSize=10");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(10, items.GetArrayLength());
            Assert.Equal(1, items[0].GetProperty("Id").GetInt32());
            Assert.Equal(10, items[9].GetProperty("Id").GetInt32());
        }

        [Fact(Skip = "Pending: pagination.")]
        public async Task Pagination_SecondPage_ReturnsSecondSlice()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?page=2&pageSize=10");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(10, items.GetArrayLength());
            Assert.Equal(11, items[0].GetProperty("Id").GetInt32());
        }

        [Fact(Skip = "Pending: pagination. Last partial page returns remaining items only.")]
        public async Task Pagination_LastPage_ReturnsRemainingItems()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?page=3&pageSize=10");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(5, items.GetArrayLength());
        }

        [Fact(Skip = "Pending: pagination. PageInfo envelope property emitted on paginated responses.")]
        public async Task Pagination_EmitsPageInfoMetadata()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?page=2&pageSize=10");

            Assert.True(doc.RootElement.TryGetProperty("Data", out var data));
            Assert.True(data.TryGetProperty("Page", out var page));
            Assert.Equal(2, page.GetProperty("Page").GetInt32());
            Assert.Equal(10, page.GetProperty("PageSize").GetInt32());
            Assert.Equal(25, page.GetProperty("TotalItems").GetInt32());
            Assert.Equal(3, page.GetProperty("TotalPages").GetInt32());
        }

        [Fact(Skip = "Pending: pagination. Out-of-range page returns empty items with correct metadata.")]
        public async Task Pagination_OutOfRangePage_ReturnsEmptyItems()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?page=99&pageSize=10");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(0, items.GetArrayLength());
        }

        [Fact(Skip = "Pending: pagination. Zero or negative page defaults to page 1.")]
        public async Task Pagination_InvalidPageNumber_DefaultsToFirstPage()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?page=0&pageSize=10");
            var items = doc.GetData().GetProperty("Items");

            Assert.Equal(1, items[0].GetProperty("Id").GetInt32());
        }

        [Fact(Skip = "Pending: pagination. No page/pageSize params → no pagination applied, no PageInfo emitted.")]
        public async Task Pagination_AbsentParams_ReturnsFullCollection()
        {
            using var server = TestServerHelper.CreateServer(LargeSample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test");
            var data = doc.GetData();

            Assert.Equal(25, data.GetProperty("Items").GetArrayLength());
            Assert.False(data.HasProperty("Page"));
        }
    }
}
