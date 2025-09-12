using Popcorn;
using System.Text.Json.Serialization;

namespace SerializationPerformance.Models;

// Simple model for basic tests
public class SimpleModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
   
    [Always]
    public DateTime CreatedAt { get; set; }
    
    [Default]
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

// Complex nested model for depth testing
public class ComplexNestedModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    [Always]
    public DateTime Timestamp { get; set; }
    
    // Single nested object
    public SimpleModel? Details { get; set; }
    
    // Collection of simple objects
    public List<SimpleModel> Items { get; set; } = new();
    
    // Nested complex object
    public ComplexNestedModel? Child { get; set; }
    
    // Dictionary with complex values
    public Dictionary<string, SimpleModel> Lookup { get; set; } = new();
    
    // Optional fields for attribute testing
    [Never]
    public string SecretData { get; set; } = "hidden";
    
    [Default]
    public int Priority { get; set; }
}

// Model with circular references for loop detection testing
public class CircularReferenceModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    
    // Creates potential circular reference
    public CircularReferenceModel? Parent { get; set; }
    public List<CircularReferenceModel> Children { get; set; } = new();
    
    // Self-reference
    public CircularReferenceModel? Self { get; set; }
}

// Model for scalability testing - simple structure for large collections
public class ScalableModel
{
    public int Id { get; set; }
    public string Data { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    
    [Always]
    public bool IsImportant { get; set; }
    
    [Default]
    public string? Category { get; set; }
}

// Deep nesting model for depth performance testing
public class DeepNestingModel
{
    public int Level { get; set; }
    public string Name { get; set; } = "";
    public DeepNestingModel? Next { get; set; }
    
    [Always]
    public DateTime ProcessedAt { get; set; }
    
    [Default]
    public string? Description { get; set; }
}

// Model with heavy attribute usage for attribute processing benchmarks
public class AttributeHeavyModel
{
    [Always]
    public int Id { get; set; }

    [Always]
    public string Name { get; set; } = "";
    
    [Never]
    public string Password { get; set; } = "";

    [Never]
    public string InternalNotes { get; set; } = "";
    
    [Default]
    public string Email { get; set; } = "";

    [Default]
    public DateTime LastLogin { get; set; }

    [Always]
    public bool IsActive { get; set; }
    
    public string PublicData { get; set; } = "";
    public int Score { get; set; }
    public double Rating { get; set; }

    [Default]
    public List<string> Tags { get; set; } = new();

    [Never]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Model with JsonPropertyName mapping for property name mapping benchmarks
public class PropertyMappingModel
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";
    
    [JsonPropertyName("email_address")]
    [Always]
    public string EmailAddress { get; set; } = "";
    
    [JsonPropertyName("created_timestamp")]
    public DateTime CreatedTimestamp { get; set; }
    
    [JsonPropertyName("is_verified")]
    [Default]
    public bool IsVerified { get; set; }
    
    [JsonPropertyName("user_preferences")]
    public Dictionary<string, string> UserPreferences { get; set; } = new();
}
