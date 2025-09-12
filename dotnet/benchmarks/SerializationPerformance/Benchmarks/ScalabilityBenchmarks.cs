using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;

namespace SerializationPerformance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ScalabilityBenchmarks
{
    private List<ScalableModel> _models10 = null!;
    private List<ScalableModel> _models100 = null!;
    private List<ScalableModel> _models1K = null!;
    private List<ScalableModel> _models10K = null!;
    private List<ScalableModel> _models100K = null!;

    private DeepNestingModel _depth1 = null!;
    private DeepNestingModel _depth2 = null!;
    private DeepNestingModel _depth5 = null!;
    private DeepNestingModel _depth10 = null!;
    private DeepNestingModel _depth20 = null!;

    private readonly JsonSerializerOptions _standardJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [GlobalSetup]
    public void Setup()
    {
        // Flat list scalability test data
        _models10 = TestDataGenerator.CreateScalableModelList(10);
        _models100 = TestDataGenerator.CreateScalableModelList(100);
        _models1K = TestDataGenerator.CreateScalableModelList(1_000);
        _models10K = TestDataGenerator.CreateScalableModelList(10_000);
        _models100K = TestDataGenerator.CreateScalableModelList(100_000);

        // Deep nesting test data
        _depth1 = TestDataGenerator.CreateDeepNestingModel(1);
        _depth2 = TestDataGenerator.CreateDeepNestingModel(2);
        _depth5 = TestDataGenerator.CreateDeepNestingModel(5);
        _depth10 = TestDataGenerator.CreateDeepNestingModel(10);
        _depth20 = TestDataGenerator.CreateDeepNestingModel(20);
    }

    // Flat List Scaling Tests - Standard JSON
    [Benchmark(Baseline = true)]
    public string FlatList_StandardJson_10()
    {
        return JsonSerializer.Serialize(_models10, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_StandardJson_100()
    {
        return JsonSerializer.Serialize(_models100, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_StandardJson_1K()
    {
        return JsonSerializer.Serialize(_models1K, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_StandardJson_10K()
    {
        return JsonSerializer.Serialize(_models10K, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_StandardJson_100K()
    {
        return JsonSerializer.Serialize(_models100K, _standardJsonOptions);
    }

    // Flat List Scaling Tests - Popcorn Default
    [Benchmark]
    public string FlatList_PopcornDefault_10()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_models10, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_100()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_models100, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_1K()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_models1K, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_10K()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_models10K, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_100K()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_models100K, _standardJsonOptions);
    }

    // Flat List Scaling Tests - Popcorn All
    [Benchmark]
    public string FlatList_PopcornAll_10()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_models10, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_100()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_models100, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_1K()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_models1K, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_10K()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_models10K, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_100K()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_models100K, _standardJsonOptions);
    }

    // Deep Nesting Tests - Standard JSON
    [Benchmark]
    public string DeepNesting_StandardJson_Depth1()
    {
        return JsonSerializer.Serialize(_depth1, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_StandardJson_Depth2()
    {
        return JsonSerializer.Serialize(_depth2, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_StandardJson_Depth5()
    {
        return JsonSerializer.Serialize(_depth5, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_StandardJson_Depth10()
    {
        return JsonSerializer.Serialize(_depth10, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_StandardJson_Depth20()
    {
        return JsonSerializer.Serialize(_depth20, _standardJsonOptions);
    }

    // Deep Nesting Tests - Popcorn Default
    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth1()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_depth1, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth2()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_depth2, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth5()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_depth5, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth10()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_depth10, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth20()
    {
        // TODO: Replace with actual Popcorn serialization
        return JsonSerializer.Serialize(_depth20, _standardJsonOptions);
    }

    // Deep Nesting Tests - Popcorn All
    [Benchmark]
    public string DeepNesting_PopcornAll_Depth1()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_depth1, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth2()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_depth2, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth5()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_depth5, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth10()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_depth10, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth20()
    {
        // TODO: Replace with actual Popcorn serialization with [!all]
        return JsonSerializer.Serialize(_depth20, _standardJsonOptions);
    }
}
