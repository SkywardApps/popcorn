using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;
using Popcorn.Shared; 

namespace SerializationPerformance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class SerializationComparisonBenchmarks
{
    private SimpleModel _simpleModel = null!;
    private List<SimpleModel> _simpleModelList = null!;
    private ComplexNestedModel _complexModel = null!;
    private List<ComplexNestedModel> _complexModelList = null!;

    private readonly JsonSerializerOptions _standardJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private List<PropertyReference> _emptyIncludes = new ();
    private List<PropertyReference> _allIncludes = new() { new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null } };
    
    // Custom includes for properties that are neither [Default] nor [Always]
    private List<PropertyReference> _simpleModelCustomIncludes = new()
    {
        new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Name".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "IsActive".AsMemory(), Negated = false, Children = null }
    };
    
    private List<PropertyReference> _complexModelCustomIncludes = new()
    {
        new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Title".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Details".AsMemory(), Negated = false, 
            Children = new List<PropertyReference>
            {
                new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
                new PropertyReference { Name = "Name".AsMemory(), Negated = false, Children = null }
            }
        },
        new PropertyReference { Name = "Items".AsMemory(), Negated = false, 
            Children = new List<PropertyReference>
            {
                new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
                new PropertyReference { Name = "Name".AsMemory(), Negated = false, Children = null }
            }
        },
        new PropertyReference { Name = "Lookup".AsMemory(), Negated = false, Children = null }
    };
    private ApiResponse<SimpleModel> _simpleModelDefaultResponse;
    private ApiResponse<SimpleModel> _simpleModelAllResponse;
    private ApiResponse<SimpleModel> _simpleModelCustomResponse;
    private ApiResponse<List<SimpleModel>> _simpleModelListDefaultResponse;
    private ApiResponse<List<SimpleModel>> _simpleModelListAllResponse;
    private ApiResponse<List<SimpleModel>> _simpleModelListCustomResponse;
    private ApiResponse<ComplexNestedModel> _complexModelDefaultResponse;
    private ApiResponse<ComplexNestedModel> _complexModelAllResponse;
    private ApiResponse<ComplexNestedModel> _complexModelCustomResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexModelListDefaultResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexModelListAllResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexModelListCustomResponse;

    [GlobalSetup]
    public void Setup()
    {
        _simpleModel = TestDataGenerator.CreateSimpleModel(1);
        _simpleModelList = TestDataGenerator.CreateSimpleModelList(100);
        _complexModel = TestDataGenerator.CreateComplexNestedModel(1);
        _complexModelList = TestDataGenerator.CreateComplexNestedModelList(25);

        _standardJsonOptions.AddPopcornOptions();

        _simpleModelDefaultResponse = new ApiResponse<SimpleModel>(new Pop<SimpleModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _simpleModel
        });
        _simpleModelAllResponse = new ApiResponse<SimpleModel>(new Pop<SimpleModel>
        {
            PropertyReferences = _allIncludes,
            Data = _simpleModel
        });
        _simpleModelCustomResponse = new ApiResponse<SimpleModel>(new Pop<SimpleModel>
        {
            PropertyReferences = _simpleModelCustomIncludes,
            Data = _simpleModel
        });
        _simpleModelListDefaultResponse = new ApiResponse<List<SimpleModel>>(new Pop<List<SimpleModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _simpleModelList
        });
        _simpleModelListAllResponse = new ApiResponse<List<SimpleModel>>(new Pop<List<SimpleModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _simpleModelList
        });
        _simpleModelListCustomResponse = new ApiResponse<List<SimpleModel>>(new Pop<List<SimpleModel>>
        {
            PropertyReferences = _simpleModelCustomIncludes,
            Data = _simpleModelList
        });
        _complexModelDefaultResponse = new ApiResponse<ComplexNestedModel>(new Pop<ComplexNestedModel>
        {
            PropertyReferences = _emptyIncludes,
            Data = _complexModel
        });
        _complexModelAllResponse = new ApiResponse<ComplexNestedModel>(new Pop<ComplexNestedModel>
        {
            PropertyReferences = _allIncludes,
            Data = _complexModel
        });
        _complexModelCustomResponse = new ApiResponse<ComplexNestedModel>(new Pop<ComplexNestedModel>
        {
            PropertyReferences = _complexModelCustomIncludes,
            Data = _complexModel
        });
        _complexModelListDefaultResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _complexModelList
        });
        _complexModelListAllResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _complexModelList
        });
        _complexModelListCustomResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _complexModelCustomIncludes,
            Data = _complexModelList
        });
    }

    // Simple Model Benchmarks
    [Benchmark(Baseline = true)]
    public string SimpleModel_StandardJson()
    {
        return JsonSerializer.Serialize(_simpleModel, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModel_PopcornDefault()
    {
        return JsonSerializer.Serialize(_simpleModelDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModel_PopcornAll()
    {
        return JsonSerializer.Serialize(_simpleModelAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModel_PopcornCustom()
    {
        return JsonSerializer.Serialize(_simpleModelCustomResponse, _standardJsonOptions);
    }

    // Simple Model List Benchmarks
    [Benchmark]
    public string SimpleModelList_StandardJson()
    {
        return JsonSerializer.Serialize(_simpleModelList, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModelList_PopcornDefault()
    {
        return JsonSerializer.Serialize(_simpleModelListDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModelList_PopcornAll()
    {
        return JsonSerializer.Serialize(_simpleModelListAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModelList_PopcornCustom()
    {
        return JsonSerializer.Serialize(_simpleModelListCustomResponse, _standardJsonOptions);
    }

    // Complex Model Benchmarks
    [Benchmark]
    public string ComplexModel_StandardJson()
    {
        return JsonSerializer.Serialize(_complexModel, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_PopcornDefault()
    {
        return JsonSerializer.Serialize(_complexModelDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_PopcornAll()
    {
        return JsonSerializer.Serialize(_complexModelAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_PopcornCustom()
    {
        return JsonSerializer.Serialize(_complexModelCustomResponse, _standardJsonOptions);
    }

    // Complex Model List Benchmarks
    [Benchmark]
    public string ComplexModelList_StandardJson()
    {
        return JsonSerializer.Serialize(_complexModelList, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModelList_PopcornDefault()
    {
        return JsonSerializer.Serialize(_complexModelListDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModelList_PopcornAll()
    {
        return JsonSerializer.Serialize(_complexModelListAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModelList_PopcornCustom()
    {
        return JsonSerializer.Serialize(_complexModelListCustomResponse, _standardJsonOptions);
    }
}
