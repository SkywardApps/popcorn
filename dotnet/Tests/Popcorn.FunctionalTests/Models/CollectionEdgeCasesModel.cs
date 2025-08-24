using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class CollectionEdgeCasesModel
    {
        // Null collections
        public List<int>? NullIntList { get; set; }
        
        public string[]? NullStringArray { get; set; }
        
        public Dictionary<string, int>? NullDictionary { get; set; }
        
        // Empty collections
        public List<int> EmptyIntList { get; set; } = new List<int>();
        
        public string[] EmptyStringArray { get; set; } = Array.Empty<string>();
        
        public Dictionary<string, int> EmptyDictionary { get; set; } = new Dictionary<string, int>();
        
        // Collections with null items
        public List<string?> ListWithNullItems { get; set; } = new List<string?>();
        
        public List<ComplexItem?> ListWithNullComplexItems { get; set; } = new List<ComplexItem?>();
        
        // Collections with circular references
        public List<CircularReferenceItem> CircularReferenceList { get; set; } = new List<CircularReferenceItem>();
        
        // Very large collections (to be populated in tests)
        public List<int> VeryLargeIntList { get; set; } = new List<int>();
        
        public List<string> VeryLargeStringList { get; set; } = new List<string>();
        
        // Collection with items having very large properties
        public List<ItemWithLargeProperties> ItemsWithLargeProperties { get; set; } = new List<ItemWithLargeProperties>();
    }

    public class CircularReferenceItem
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        // This property can create a circular reference when items reference each other
        public CircularReferenceItem? Parent { get; set; }
        
        // This collection can create circular references when items are added to their own children
        public List<CircularReferenceItem> Children { get; set; } = new List<CircularReferenceItem>();
    }

    public class ItemWithLargeProperties
    {
        public int Id { get; set; }
        
        // Very large string property
        public string VeryLargeString { get; set; } = string.Empty;
        
        // Very large array
        public int[] VeryLargeArray { get; set; } = Array.Empty<int>();
    }
}
