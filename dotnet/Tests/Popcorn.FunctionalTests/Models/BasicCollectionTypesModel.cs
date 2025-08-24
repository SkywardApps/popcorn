using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class BasicCollectionTypesModel
    {
        // Array types
        public int[] IntArray { get; set; } = Array.Empty<int>();
        
        public string[] StringArray { get; set; } = Array.Empty<string>();
        
        // List types
        public List<int> IntList { get; set; } = new List<int>();
        
        public List<string> StringList { get; set; } = new List<string>();
        
        // IEnumerable types
        public IEnumerable<int> IntEnumerable { get; set; } = new List<int>();
        
        public IEnumerable<string> StringEnumerable { get; set; } = new List<string>();
        
        // ICollection types
        public ICollection<int> IntCollection { get; set; } = new List<int>();
        
        public ICollection<string> StringCollection { get; set; } = new List<string>();
        
        // ReadOnlyCollection types
        public ReadOnlyCollection<int> ReadOnlyIntCollection { get; set; } = new List<int>().AsReadOnly();
        
        public ReadOnlyCollection<string> ReadOnlyStringCollection { get; set; } = new List<string>().AsReadOnly();
        
        // Collection with complex items
        public List<ComplexItem> ComplexItemsList { get; set; } = new List<ComplexItem>();
        
        // Nested collections
        public List<List<int>> NestedIntLists { get; set; } = new List<List<int>>();
        
        public List<string[]> ListOfStringArrays { get; set; } = new List<string[]>();
    }

    public class ComplexItem
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        [Default]
        public string Description { get; set; } = string.Empty;
        
        [Always]
        public DateTime CreatedDate { get; set; }
    }
}
