using BenchmarkDotNet.Attributes;
using Popcorn.Shared;
using SerializationPerformance.Models;
using System.Text.Json;

namespace SerializationPerformance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class AttributeProcessingBenchmarks
{
    private List<SimpleModel> _simpleModels = null!;
    private List<AttributeHeavyModel> _attributeHeavyModels = null!;
    private List<PropertyMappingModel> _propertyMappingModels = null!;

    private readonly JsonSerializerOptions _standardJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    [GlobalSetup]
    public void Setup()
    {
        _simpleModels = TestDataGenerator.CreateSimpleModelList(200);
        _attributeHeavyModels = TestDataGenerator.CreateAttributeHeavyModelList(200);
        _propertyMappingModels = TestDataGenerator.CreatePropertyMappingModelList(200);
    }

    // Baseline tests with minimal attributes
    [Benchmark(Baseline = true)]
    public string SimpleModels_StandardJson()
    {
        return JsonSerializer.Serialize(_simpleModels, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModels_PopcornDefault()
    {
        // TODO: Replace with actual Popcorn serialization
        // SimpleModel has minimal attributes: [Always] CreatedAt, [Default] Description
        return JsonSerializer.Serialize(_simpleModels, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModels_PopcornAll()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_simpleModels, _standardJsonOptions);
    }

    // Heavy attribute processing tests
    [Benchmark]
    public string AttributeHeavy_StandardJson()
    {
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornDefault()
    {
        // TODO: Replace with actual Popcorn serialization
        // AttributeHeavyModel has many attributes: [Always], [Never], [Default]
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornAll()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        // Should respect [Never] attributes even with [!all]
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornAlwaysOnly()
    {
        // TODO: Replace with actual Popcorn serialization that only processes [Always] fields
        // Should include: Id, Name, EmailAddress, IsActive
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornDefaultOnly()
    {
        // TODO: Replace with actual Popcorn serialization with [!default]
        // Should include: Email, LastLogin, IsVerified, Tags + [Always] fields
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornExcludeNever()
    {
        // TODO: Replace with actual Popcorn serialization with [!all] excluding [Never] fields
        // Should exclude: Password, InternalNotes, Metadata
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    // Property name mapping tests
    [Benchmark]
    public string PropertyMapping_StandardJson()
    {
        return JsonSerializer.Serialize(_propertyMappingModels, _standardJsonOptions);
    }

    [Benchmark]
    public string PropertyMapping_PopcornDefault()
    {
        // TODO: Replace with actual Popcorn serialization
        // PropertyMappingModel has JsonPropertyName mappings: user_id, full_name, etc.
        return JsonSerializer.Serialize(_propertyMappingModels, _standardJsonOptions);
    }

    [Benchmark]
    public string PropertyMapping_PopcornAll()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_propertyMappingModels, _standardJsonOptions);
    }

    [Benchmark]
    public string PropertyMapping_PopcornCustomIncludes()
    {
        // TODO: Replace with actual Popcorn serialization with specific includes
        // Test property name mapping with custom field selection
        return JsonSerializer.Serialize(_propertyMappingModels, _standardJsonOptions);
    }

    // Attribute processing overhead comparison
    [Benchmark]
    public string MinimalAttributes_Popcorn()
    {
        // TODO: Simple models with few attributes
        return JsonSerializer.Serialize(_simpleModels, _standardJsonOptions);
    }

    [Benchmark]
    public string HeavyAttributes_Popcorn()
    {
        // TODO: Models with many attributes to measure processing overhead
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string NoAttributes_StandardJson()
    {
        // Create models without any Popcorn attributes for comparison
        var plainModels = _simpleModels.Select(m => new 
        {
            m.Id,
            m.Name,
            m.CreatedAt,
            m.Description,
            m.IsActive
        }).ToList();
        
        return JsonSerializer.Serialize(plainModels, _standardJsonOptions);
    }
}
