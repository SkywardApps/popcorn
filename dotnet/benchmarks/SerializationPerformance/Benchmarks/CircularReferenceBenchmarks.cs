using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;

namespace SerializationPerformance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class CircularReferenceBenchmarks
{
    private List<CircularReferenceModel> _modelsNoCircular = null!;
    private List<CircularReferenceModel> _modelsWithCircular = null!;
    private CircularReferenceModel _singleModelNoCircular = null!;
    private CircularReferenceModel _singleModelWithCircular = null!;

    private readonly JsonSerializerOptions _standardJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    [GlobalSetup]
    public void Setup()
    {
        // Models without circular references (baseline)
        _modelsNoCircular = TestDataGenerator.CreateCircularReferenceModelList(100, includeCircular: false);
        
        // Models with circular references (test circular detection overhead)
        _modelsWithCircular = TestDataGenerator.CreateCircularReferenceModelList(100, includeCircular: true);
        
        // Single models for focused testing
        _singleModelNoCircular = TestDataGenerator.CreateCircularReferenceModel(1, createCircular: false);
        _singleModelWithCircular = TestDataGenerator.CreateCircularReferenceModel(1, createCircular: true);
    }

    // Single Model Tests - Baseline (No Circular References)
    [Benchmark(Baseline = true)]
    public string SingleModel_StandardJson_NoCircular()
    {
        return JsonSerializer.Serialize(_singleModelNoCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string SingleModel_PopcornDefault_NoCircular()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_singleModelNoCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string SingleModel_PopcornAll_NoCircular()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_singleModelNoCircular, _standardJsonOptions);
    }

    // Single Model Tests - With Circular References
    [Benchmark]
    public string SingleModel_StandardJson_WithCircular()
    {
        return JsonSerializer.Serialize(_singleModelWithCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string SingleModel_PopcornDefault_WithCircular()
    {
        // TODO: Replace with actual Popcorn serialization - should detect circular references
        return JsonSerializer.Serialize(_singleModelWithCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string SingleModel_PopcornAll_WithCircular()
    {
        // TODO: Replace with actual Popcorn serialization with [!all] - should detect circular references
        return JsonSerializer.Serialize(_singleModelWithCircular, _standardJsonOptions);
    }

    // List Tests - No Circular References
    [Benchmark]
    public string List_StandardJson_NoCircular()
    {
        return JsonSerializer.Serialize(_modelsNoCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string List_PopcornDefault_NoCircular()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_modelsNoCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string List_PopcornAll_NoCircular()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_modelsNoCircular, _standardJsonOptions);
    }

    // List Tests - With Circular References
    [Benchmark]
    public string List_StandardJson_WithCircular()
    {
        return JsonSerializer.Serialize(_modelsWithCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string List_PopcornDefault_WithCircular()
    {
        // TODO: Replace with actual Popcorn serialization - should detect circular references
        return JsonSerializer.Serialize(_modelsWithCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string List_PopcornAll_WithCircular()
    {
        // TODO: Replace with actual Popcorn serialization with [!all] - should detect circular references
        return JsonSerializer.Serialize(_modelsWithCircular, _standardJsonOptions);
    }

    // Circular Reference Detection Overhead Tests
    [Benchmark]
    public string CircularDetectionOverhead_NoCircular()
    {
        // TODO: This should use Popcorn serialization with circular detection enabled
        // but applied to data without actual circular references to measure overhead
        return JsonSerializer.Serialize(_modelsNoCircular, _standardJsonOptions);
    }

    [Benchmark]
    public string CircularDetectionOverhead_WithCircular()
    {
        // TODO: This should use Popcorn serialization with circular detection enabled
        // and applied to data with circular references to measure detection cost
        return JsonSerializer.Serialize(_modelsWithCircular, _standardJsonOptions);
    }
}
