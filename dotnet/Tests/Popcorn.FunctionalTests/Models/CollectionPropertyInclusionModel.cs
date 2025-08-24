using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class CollectionPropertyInclusionModel
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        // Collection with items having various property attributes
        public List<ItemWithAttributes> ItemsWithAttributes { get; set; } = new List<ItemWithAttributes>();
        
        // Collection with items having nested objects
        public List<ItemWithNestedObject> ItemsWithNestedObjects { get; set; } = new List<ItemWithNestedObject>();
        
        // Collection with items having nested collections
        public List<ItemWithNestedCollection> ItemsWithNestedCollections { get; set; } = new List<ItemWithNestedCollection>();
        
        // Collection with items having default properties
        public List<ItemWithDefaultProperties> ItemsWithDefaultProperties { get; set; } = new List<ItemWithDefaultProperties>();
    }

    public class ItemWithAttributes
    {
        public int Id { get; set; }
        
        [Always]
        public string AlwaysIncludedProperty { get; set; } = string.Empty;
        
        [Default]
        public string DefaultIncludedProperty { get; set; } = string.Empty;
        
        [Never]
        public string NeverIncludedProperty { get; set; } = string.Empty;
        
        public string RegularProperty { get; set; } = string.Empty;
    }

    public class ItemWithNestedObject
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public NestedObject NestedObject { get; set; } = new NestedObject();
    }

    public class NestedObject
    {
        public int NestedId { get; set; }
        
        [Always]
        public string AlwaysIncludedNestedProperty { get; set; } = string.Empty;
        
        [Default]
        public string DefaultIncludedNestedProperty { get; set; } = string.Empty;
        
        public string RegularNestedProperty { get; set; } = string.Empty;
    }

    public class ItemWithNestedCollection
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public List<NestedItem> NestedItems { get; set; } = new List<NestedItem>();
    }

    public class NestedItem
    {
        public int NestedItemId { get; set; }
        
        [Always]
        public string AlwaysIncludedNestedItemProperty { get; set; } = string.Empty;
        
        [Default]
        public string DefaultIncludedNestedItemProperty { get; set; } = string.Empty;
        
        public string RegularNestedItemProperty { get; set; } = string.Empty;
    }

    public class ItemWithDefaultProperties
    {
        public int Id { get; set; }
        
        [Default]
        public string DefaultProperty1 { get; set; } = string.Empty;
        
        [Default]
        public string DefaultProperty2 { get; set; } = string.Empty;
        
        public string NonDefaultProperty1 { get; set; } = string.Empty;
        
        public string NonDefaultProperty2 { get; set; } = string.Empty;
    }
}
