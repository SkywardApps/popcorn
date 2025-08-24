using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class AdvancedCollectionTypesModel
    {
        // ConcurrentDictionary
        public ConcurrentDictionary<string, int> ConcurrentStringIntDictionary { get; set; } = 
            new ConcurrentDictionary<string, int>();
        
        // ImmutableArray
        public ImmutableArray<int> ImmutableIntArray { get; set; } = ImmutableArray<int>.Empty;
        
        public ImmutableArray<string> ImmutableStringArray { get; set; } = ImmutableArray<string>.Empty;
        
        // HashSet
        public HashSet<int> IntHashSet { get; set; } = new HashSet<int>();
        
        public HashSet<string> StringHashSet { get; set; } = new HashSet<string>();
        
        // ObservableCollection
        public ObservableCollection<int> ObservableIntCollection { get; set; } = new ObservableCollection<int>();
        
        public ObservableCollection<string> ObservableStringCollection { get; set; } = new ObservableCollection<string>();
        
        // ImmutableList
        public ImmutableList<int> ImmutableIntList { get; set; } = ImmutableList<int>.Empty;
        
        public ImmutableList<string> ImmutableStringList { get; set; } = ImmutableList<string>.Empty;
        
        // ImmutableDictionary
        public ImmutableDictionary<string, int> ImmutableStringIntDictionary { get; set; } = 
            ImmutableDictionary<string, int>.Empty;
        
        public ImmutableDictionary<string, string> ImmutableStringStringDictionary { get; set; } = 
            ImmutableDictionary<string, string>.Empty;
        
        // ImmutableHashSet
        public ImmutableHashSet<int> ImmutableIntHashSet { get; set; } = ImmutableHashSet<int>.Empty;
        
        public ImmutableHashSet<string> ImmutableStringHashSet { get; set; } = ImmutableHashSet<string>.Empty;
        
        // SortedList
        public SortedList<string, int> SortedStringIntList { get; set; } = new SortedList<string, int>();
        
        public SortedList<string, string> SortedStringStringList { get; set; } = new SortedList<string, string>();
        
        // SortedDictionary
        public SortedDictionary<string, int> SortedStringIntDictionary { get; set; } = new SortedDictionary<string, int>();
        
        public SortedDictionary<string, string> SortedStringStringDictionary { get; set; } = new SortedDictionary<string, string>();
        
        // SortedSet
        public SortedSet<int> SortedIntSet { get; set; } = new SortedSet<int>();
        
        public SortedSet<string> SortedStringSet { get; set; } = new SortedSet<string>();
    }
}
