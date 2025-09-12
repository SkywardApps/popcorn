using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;
using Popcorn.Shared;

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

    private List<PropertyReference> _emptyIncludes = new();
    private List<PropertyReference> _allIncludes = new() { new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null } };
    
    private ApiResponse<List<ScalableModel>> _models10DefaultResponse;
    private ApiResponse<List<ScalableModel>> _models100DefaultResponse;
    private ApiResponse<List<ScalableModel>> _models1KDefaultResponse;
    private ApiResponse<List<ScalableModel>> _models10KDefaultResponse;
    private ApiResponse<List<ScalableModel>> _models100KDefaultResponse;
    
    private ApiResponse<List<ScalableModel>> _models10AllResponse;
    private ApiResponse<List<ScalableModel>> _models100AllResponse;
    private ApiResponse<List<ScalableModel>> _models1KAllResponse;
    private ApiResponse<List<ScalableModel>> _models10KAllResponse;
    private ApiResponse<List<ScalableModel>> _models100KAllResponse;
    
    private ApiResponse<DeepNestingModel> _depth1DefaultResponse;
    private ApiResponse<DeepNestingModel> _depth2DefaultResponse;
    private ApiResponse<DeepNestingModel> _depth5DefaultResponse;
    private ApiResponse<DeepNestingModel> _depth10DefaultResponse;
    private ApiResponse<DeepNestingModel> _depth20DefaultResponse;
    
    private ApiResponse<DeepNestingModel> _depth1AllResponse;
    private ApiResponse<DeepNestingModel> _depth2AllResponse;
    private ApiResponse<DeepNestingModel> _depth5AllResponse;
    private ApiResponse<DeepNestingModel> _depth10AllResponse;
    private ApiResponse<DeepNestingModel> _depth20AllResponse;

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

        _standardJsonOptions.AddPopcornOptions();

        // Initialize flat list responses
        _models10DefaultResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _models10
        });
        _models100DefaultResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _models100
        });
        _models1KDefaultResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _models1K
        });
        _models10KDefaultResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _models10K
        });
        _models100KDefaultResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _models100K
        });

        _models10AllResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _models10
        });
        _models100AllResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _models100
        });
        _models1KAllResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _models1K
        });
        _models10KAllResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _models10K
        });
        _models100KAllResponse = new ApiResponse<List<ScalableModel>>(new Pop<List<ScalableModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _models100K
        });

        // Initialize deep nesting responses
        _depth1DefaultResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _depth1
        });
        _depth2DefaultResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _depth2
        });
        _depth5DefaultResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _depth5
        });
        _depth10DefaultResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _depth10
        });
        _depth20DefaultResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _depth20
        });

        _depth1AllResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _allIncludes,
            Data = _depth1
        });
        _depth2AllResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _allIncludes,
            Data = _depth2
        });
        _depth5AllResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _allIncludes,
            Data = _depth5
        });
        _depth10AllResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _allIncludes,
            Data = _depth10
        });
        _depth20AllResponse = new ApiResponse<DeepNestingModel>(new Pop<DeepNestingModel>
        {
            PropertyReferences = _allIncludes,
            Data = _depth20
        });
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
        return JsonSerializer.Serialize(_models10DefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_100()
    {
        return JsonSerializer.Serialize(_models100DefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_1K()
    {
        return JsonSerializer.Serialize(_models1KDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_10K()
    {
        return JsonSerializer.Serialize(_models10KDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornDefault_100K()
    {
        return JsonSerializer.Serialize(_models100KDefaultResponse, _standardJsonOptions);
    }

    // Flat List Scaling Tests - Popcorn All
    [Benchmark]
    public string FlatList_PopcornAll_10()
    {
        return JsonSerializer.Serialize(_models10AllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_100()
    {
        return JsonSerializer.Serialize(_models100AllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_1K()
    {
        return JsonSerializer.Serialize(_models1KAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_10K()
    {
        return JsonSerializer.Serialize(_models10KAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string FlatList_PopcornAll_100K()
    {
        return JsonSerializer.Serialize(_models100KAllResponse, _standardJsonOptions);
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
        return JsonSerializer.Serialize(_depth1DefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth2()
    {
        return JsonSerializer.Serialize(_depth2DefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth5()
    {
        return JsonSerializer.Serialize(_depth5DefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth10()
    {
        return JsonSerializer.Serialize(_depth10DefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornDefault_Depth20()
    {
        return JsonSerializer.Serialize(_depth20DefaultResponse, _standardJsonOptions);
    }

    // Deep Nesting Tests - Popcorn All
    [Benchmark]
    public string DeepNesting_PopcornAll_Depth1()
    {
        return JsonSerializer.Serialize(_depth1AllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth2()
    {
        return JsonSerializer.Serialize(_depth2AllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth5()
    {
        return JsonSerializer.Serialize(_depth5AllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth10()
    {
        return JsonSerializer.Serialize(_depth10AllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string DeepNesting_PopcornAll_Depth20()
    {
        return JsonSerializer.Serialize(_depth20AllResponse, _standardJsonOptions);
    }
}
