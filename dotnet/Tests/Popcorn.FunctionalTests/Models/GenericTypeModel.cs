using System.Collections.Generic;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class PageModel<T>
    {
        [Always]
        public int PageNumber { get; set; }

        [Always]
        public int PageSize { get; set; }

        [Default]
        public List<T> Items { get; set; } = new();

        [Default]
        public long TotalCount { get; set; }
    }

    public class ItemPayload
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string Name { get; set; } = string.Empty;

        public string Secret { get; set; } = string.Empty;
    }
}
