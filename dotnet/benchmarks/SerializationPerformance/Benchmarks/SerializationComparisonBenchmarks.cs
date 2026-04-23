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

    // Reflection-based System.Text.Json options. Establishes the "pre-source-gen" baseline.
    private readonly JsonSerializerOptions _reflectionJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Stock System.Text.Json + generated metadata context (BenchmarkJsonContext). Matches what
    // an AOT-targeting app would use without Popcorn. Fair-fight comparison point for Popcorn.
    private readonly JsonSerializerOptions _stjSourceGenOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = BenchmarkJsonContext.Default,
    };

    // Popcorn options — BenchmarkJsonContext plus AddPopcornOptions (registers generated converters).
    private readonly JsonSerializerOptions _popcornJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = BenchmarkJsonContext.Default,
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

        _popcornJsonOptions.AddPopcornOptions();

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

    // Simple Model
    [Benchmark(Baseline = true)]
    public string SimpleModel_Stj_Reflection() =>
        JsonSerializer.Serialize(_simpleModel, _reflectionJsonOptions);

    [Benchmark]
    public string SimpleModel_Stj_SourceGen() =>
        JsonSerializer.Serialize(_simpleModel, _stjSourceGenOptions);

    [Benchmark]
    public string SimpleModel_PopcornDefault() =>
        JsonSerializer.Serialize(_simpleModelDefaultResponse, _popcornJsonOptions);

    [Benchmark]
    public string SimpleModel_PopcornAll() =>
        JsonSerializer.Serialize(_simpleModelAllResponse, _popcornJsonOptions);

    [Benchmark]
    public string SimpleModel_PopcornCustom() =>
        JsonSerializer.Serialize(_simpleModelCustomResponse, _popcornJsonOptions);

    // Simple Model List
    [Benchmark]
    public string SimpleModelList_Stj_Reflection() =>
        JsonSerializer.Serialize(_simpleModelList, _reflectionJsonOptions);

    [Benchmark]
    public string SimpleModelList_Stj_SourceGen() =>
        JsonSerializer.Serialize(_simpleModelList, _stjSourceGenOptions);

    [Benchmark]
    public string SimpleModelList_PopcornDefault() =>
        JsonSerializer.Serialize(_simpleModelListDefaultResponse, _popcornJsonOptions);

    [Benchmark]
    public string SimpleModelList_PopcornAll() =>
        JsonSerializer.Serialize(_simpleModelListAllResponse, _popcornJsonOptions);

    [Benchmark]
    public string SimpleModelList_PopcornCustom() =>
        JsonSerializer.Serialize(_simpleModelListCustomResponse, _popcornJsonOptions);

    // Complex Model
    [Benchmark]
    public string ComplexModel_Stj_Reflection() =>
        JsonSerializer.Serialize(_complexModel, _reflectionJsonOptions);

    [Benchmark]
    public string ComplexModel_Stj_SourceGen() =>
        JsonSerializer.Serialize(_complexModel, _stjSourceGenOptions);

    [Benchmark]
    public string ComplexModel_PopcornDefault() =>
        JsonSerializer.Serialize(_complexModelDefaultResponse, _popcornJsonOptions);

    [Benchmark]
    public string ComplexModel_PopcornAll() =>
        JsonSerializer.Serialize(_complexModelAllResponse, _popcornJsonOptions);

    [Benchmark]
    public string ComplexModel_PopcornCustom() =>
        JsonSerializer.Serialize(_complexModelCustomResponse, _popcornJsonOptions);

    // Complex Model List
    [Benchmark]
    public string ComplexModelList_Stj_Reflection() =>
        JsonSerializer.Serialize(_complexModelList, _reflectionJsonOptions);

    [Benchmark]
    public string ComplexModelList_Stj_SourceGen() =>
        JsonSerializer.Serialize(_complexModelList, _stjSourceGenOptions);

    [Benchmark]
    public string ComplexModelList_PopcornDefault() =>
        JsonSerializer.Serialize(_complexModelListDefaultResponse, _popcornJsonOptions);

    [Benchmark]
    public string ComplexModelList_PopcornAll() =>
        JsonSerializer.Serialize(_complexModelListAllResponse, _popcornJsonOptions);

    [Benchmark]
    public string ComplexModelList_PopcornCustom() =>
        JsonSerializer.Serialize(_complexModelListCustomResponse, _popcornJsonOptions);
}
