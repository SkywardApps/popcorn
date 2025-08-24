using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class DictionaryTypesModel
    {
        // Basic dictionary types
        public Dictionary<string, int> StringIntDictionary { get; set; } = new Dictionary<string, int>();
        
        public Dictionary<string, string> StringStringDictionary { get; set; } = new Dictionary<string, string>();
        
        // Interface dictionary types
        public IDictionary<string, int> StringIntIDictionary { get; set; } = new Dictionary<string, int>();
        
        public IDictionary<string, string> StringStringIDictionary { get; set; } = new Dictionary<string, string>();
        
        // ReadOnly dictionary types
        public ReadOnlyDictionary<string, int> ReadOnlyStringIntDictionary { get; set; } = 
            new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());
        
        public ReadOnlyDictionary<string, string> ReadOnlyStringStringDictionary { get; set; } = 
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        
        // Dictionary with complex values
        public Dictionary<string, ComplexItem> StringComplexItemDictionary { get; set; } = 
            new Dictionary<string, ComplexItem>();
        
        // Dictionary with collection values
        public Dictionary<string, List<int>> StringIntListDictionary { get; set; } = 
            new Dictionary<string, List<int>>();
        
        public Dictionary<string, string[]> StringStringArrayDictionary { get; set; } = 
            new Dictionary<string, string[]>();
        
        // Nested dictionaries
        public Dictionary<string, Dictionary<string, int>> NestedStringIntDictionary { get; set; } = 
            new Dictionary<string, Dictionary<string, int>>();
    }
}
