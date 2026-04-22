using System.Collections.Generic;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class SortableItem
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string Name { get; set; } = string.Empty;

        [Default]
        public int Year { get; set; }

        public string Category { get; set; } = string.Empty;
    }

    public class SortableCollection
    {
        [Default]
        public List<SortableItem> Items { get; set; } = new();
    }
}
