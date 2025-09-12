using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;

namespace SerializationPerformance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class IncludeStrategyBenchmarks
{
    private List<ComplexNestedModel> _complexModels = null!;
    private List<AttributeHeavyModel> _attributeHeavyModels = null!;

    private readonly JsonSerializerOptions _standardJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [GlobalSetup]
    public void Setup()
    {
        _complexModels = TestDataGenerator.CreateComplexNestedModelList(50, maxDepth: 3);
        _attributeHeavyModels = TestDataGenerator.CreateAttributeHeavyModelList(100);
    }

    [Benchmark(Baseline = true)]
    public string ComplexModel_EmptyIncludes()
    {
        // TODO: Replace with Popcorn serialization with empty includes (default behavior)
        return JsonSerializer.Serialize(_complexModels, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_DefaultIncludes()
    {
        // TODO: Replace with Popcorn serialization with [!default]
        return JsonSerializer.Serialize(_complexModels, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_AllIncludes()
    {
        // TODO: Replace with Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_complexModels, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_SimpleCustomIncludes()
    {
        // TODO: Replace with Popcorn serialization with [Id,Title,Timestamp]
        return JsonSerializer.Serialize(_complexModels, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_ComplexCustomIncludes()
    {
        // TODO: Replace with Popcorn serialization with [Id,Title,Details[Id,Name],Items[Id,Name,IsActive]]
        return JsonSerializer.Serialize(_complexModels, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_NegatedIncludes()
    {
        // TODO: Replace with Popcorn serialization with [!all,-SecretData,-Priority]
        return JsonSerializer.Serialize(_complexModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_EmptyIncludes()
    {
        // TODO: Replace with Popcorn serialization with empty includes
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_DefaultIncludes()
    {
        // TODO: Replace with Popcorn serialization with [!default]
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_AllIncludes()
    {
        // TODO: Replace with Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_AlwaysFieldsOnly()
    {
        // TODO: Replace with Popcorn serialization that only includes [Always] fields
        // Should include: Id, Name, EmailAddress, IsActive
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_ExcludeNeverFields()
    {
        // TODO: Replace with Popcorn serialization with [!all,-Password,-InternalNotes,-Metadata]
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }
}
