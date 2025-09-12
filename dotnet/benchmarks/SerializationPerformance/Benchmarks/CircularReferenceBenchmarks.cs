using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;
using Popcorn.Shared;

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

    private List<PropertyReference> _emptyIncludes = new();
    private List<PropertyReference> _allIncludes = new() { new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null } };

    private ApiResponse<CircularReferenceModel> _singleNoCircularDefaultResponse;
    private ApiResponse<CircularReferenceModel> _singleNoCircularAllResponse;
    private ApiResponse<CircularReferenceModel> _singleWithCircularDefaultResponse;
    private ApiResponse<CircularReferenceModel> _singleWithCircularAllResponse;
    
    private ApiResponse<List<CircularReferenceModel>> _listNoCircularDefaultResponse;
    private ApiResponse<List<CircularReferenceModel>> _listNoCircularAllResponse;
    private ApiResponse<List<CircularReferenceModel>> _listWithCircularDefaultResponse;
    private ApiResponse<List<CircularReferenceModel>> _listWithCircularAllResponse;
    
    private ApiResponse<List<CircularReferenceModel>> _overheadNoCircularResponse;
    private ApiResponse<List<CircularReferenceModel>> _overheadWithCircularResponse;

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

        _standardJsonOptions.AddPopcornOptions();

        // Initialize single model responses
        _singleNoCircularDefaultResponse = new ApiResponse<CircularReferenceModel>(new Pop<CircularReferenceModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _singleModelNoCircular
        });
        _singleNoCircularAllResponse = new ApiResponse<CircularReferenceModel>(new Pop<CircularReferenceModel>
        {
            PropertyReferences = _allIncludes,
            Data = _singleModelNoCircular
        });
        _singleWithCircularDefaultResponse = new ApiResponse<CircularReferenceModel>(new Pop<CircularReferenceModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _singleModelWithCircular
        });
        _singleWithCircularAllResponse = new ApiResponse<CircularReferenceModel>(new Pop<CircularReferenceModel>
        {
            PropertyReferences = _allIncludes,
            Data = _singleModelWithCircular
        });

        // Initialize list responses
        _listNoCircularDefaultResponse = new ApiResponse<List<CircularReferenceModel>>(new Pop<List<CircularReferenceModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _modelsNoCircular
        });
        _listNoCircularAllResponse = new ApiResponse<List<CircularReferenceModel>>(new Pop<List<CircularReferenceModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _modelsNoCircular
        });
        _listWithCircularDefaultResponse = new ApiResponse<List<CircularReferenceModel>>(new Pop<List<CircularReferenceModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _modelsWithCircular
        });
        _listWithCircularAllResponse = new ApiResponse<List<CircularReferenceModel>>(new Pop<List<CircularReferenceModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _modelsWithCircular
        });

        // Initialize overhead test responses
        _overheadNoCircularResponse = new ApiResponse<List<CircularReferenceModel>>(new Pop<List<CircularReferenceModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _modelsNoCircular
        });
        _overheadWithCircularResponse = new ApiResponse<List<CircularReferenceModel>>(new Pop<List<CircularReferenceModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _modelsWithCircular
        });
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
        return JsonSerializer.Serialize(_singleNoCircularDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SingleModel_PopcornAll_NoCircular()
    {
        return JsonSerializer.Serialize(_singleNoCircularAllResponse, _standardJsonOptions);
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
        return JsonSerializer.Serialize(_singleWithCircularDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SingleModel_PopcornAll_WithCircular()
    {
        return JsonSerializer.Serialize(_singleWithCircularAllResponse, _standardJsonOptions);
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
        return JsonSerializer.Serialize(_listNoCircularDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string List_PopcornAll_NoCircular()
    {
        return JsonSerializer.Serialize(_listNoCircularAllResponse, _standardJsonOptions);
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
        return JsonSerializer.Serialize(_listWithCircularDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string List_PopcornAll_WithCircular()
    {
        return JsonSerializer.Serialize(_listWithCircularAllResponse, _standardJsonOptions);
    }

    // Circular Reference Detection Overhead Tests
    [Benchmark]
    public string CircularDetectionOverhead_NoCircular()
    {
        return JsonSerializer.Serialize(_overheadNoCircularResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string CircularDetectionOverhead_WithCircular()
    {
        return JsonSerializer.Serialize(_overheadWithCircularResponse, _standardJsonOptions);
    }
}
