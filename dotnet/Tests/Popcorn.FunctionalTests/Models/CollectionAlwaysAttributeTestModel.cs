using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class CollectionAlwaysAttributeTestModel
    {
        public int Id { get; set; }
        
        public List<ItemModel> Items { get; set; } = new List<ItemModel>();
    }

    public class ItemModel
    {
        [Always]
        public int AlwaysIncludedId { get; set; }
        
        public string RegularProperty { get; set; } = string.Empty;
    }
}
