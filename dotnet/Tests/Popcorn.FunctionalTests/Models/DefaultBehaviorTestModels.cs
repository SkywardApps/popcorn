using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    // Model with no attributes - all properties should be included by default
    public class NoAttributesModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // Model with a single Default attribute - only that property should be included by default
    public class SingleDefaultModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        [Default]
        public int DefaultValue { get; set; }
        
        public string Description { get; set; } = string.Empty;
    }

    // Model with a single Always attribute - only that property should be included by default
    public class SingleAlwaysModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        [Always]
        public int AlwaysValue { get; set; }
        
        public string Description { get; set; } = string.Empty;
    }

    // Model with a single Never attribute - all properties except that one should be included by default
    public class SingleNeverModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        [Never]
        public int NeverValue { get; set; }
        
        public string Description { get; set; } = string.Empty;
    }

    // Model with mixed attributes - only Default and Always properties should be included by default
    public class MixedAttributesModel
    {
        public int Id { get; set; }
        
        [Default]
        public string DefaultName { get; set; } = string.Empty;
        
        [Always]
        public int AlwaysValue { get; set; }
        
        [Never]
        public string NeverDescription { get; set; } = string.Empty;
        
        public bool IsActive { get; set; }
    }
}
