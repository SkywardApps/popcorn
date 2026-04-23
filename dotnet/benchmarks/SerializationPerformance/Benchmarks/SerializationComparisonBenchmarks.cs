using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;
using Popcorn.Shared;
using Skyward.Popcorn.Abstractions;
using LegacyRef = Skyward.Popcorn.PropertyReference;

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

    // Legacy PopcornNetStandard (reflection engine). Factory configured in Setup() to mirror
    // the [Always]/[Default]/[Never] attributes on the current models, then the per-request
    // IPopcorn instance is fresh per benchmark call (matches legacy middleware pattern).
    private PopcornFactory _legacyFactory = null!;

    private static readonly List<LegacyRef> _legacyEmptyIncludes = new();

    // Legacy's `!all` path has a latent collision with AlwaysInclude (CacheEnumeratedProperties
    // adds every property including Always ones; the Always loop then Add()s without a
    // ContainsKey check). We explicitly enumerate all non-Never properties instead — same output,
    // no collision.
    private static readonly List<LegacyRef> _legacySimpleAllIncludes = new()
    {
        new LegacyRef("Id", false),
        new LegacyRef("Name", false),
        new LegacyRef("CreatedAt", false),
        new LegacyRef("Description", false),
        new LegacyRef("IsActive", false),
    };

    private static readonly List<LegacyRef> _legacyComplexAllIncludes = new()
    {
        new LegacyRef("Id", false),
        new LegacyRef("Title", false),
        new LegacyRef("Timestamp", false),
        new LegacyRef("Details", false),
        new LegacyRef("Items", false),
        new LegacyRef("Child", false),
        new LegacyRef("Lookup", false),
        new LegacyRef("Priority", false),
    };

    private static readonly List<LegacyRef> _legacySimpleCustomIncludes = new()
    {
        new LegacyRef("Id", false),
        new LegacyRef("Name", false),
        new LegacyRef("IsActive", false),
    };

    private static readonly List<LegacyRef> _legacyComplexCustomIncludes = new()
    {
        new LegacyRef("Id", false),
        new LegacyRef("Title", false),
        new LegacyRef("Details", false)
        {
            Children = new List<LegacyRef>
            {
                new LegacyRef("Id", false),
                new LegacyRef("Name", false),
            },
        },
        new LegacyRef("Items", false)
        {
            Children = new List<LegacyRef>
            {
                new LegacyRef("Id", false),
                new LegacyRef("Name", false),
            },
        },
    };

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

        _legacyFactory = new PopcornFactory().UseDefaultConfiguration();

        // AlwaysInclude is deliberately unused — legacy's DeterminePropertyReferences
        // adds AlwaysInclude without a ContainsKey check, which collides whenever the
        // user's include list (or !all expansion) already names that field. We mirror
        // [Default] + [Always] intent by putting both into DefaultInclude instead.
        _legacyFactory.ConfigureType<SimpleModel>(cfg =>
        {
            cfg.DefaultInclude.Add(new LegacyRef(nameof(SimpleModel.CreatedAt), false));
            cfg.DefaultInclude.Add(new LegacyRef(nameof(SimpleModel.Description), false));
        });

        _legacyFactory.ConfigureType<ComplexNestedModel>(cfg =>
        {
            cfg.NeverInclude.Add(nameof(ComplexNestedModel.SecretData));
            cfg.DefaultInclude.Add(new LegacyRef(nameof(ComplexNestedModel.Timestamp), false));
            cfg.DefaultInclude.Add(new LegacyRef(nameof(ComplexNestedModel.Priority), false));
        });
    }

    private string LegacyExpandAndSerialize(object? instance, Type sourceType, IReadOnlyList<LegacyRef> includes)
    {
        var popcorn = _legacyFactory.CreatePopcorn();
        var expanded = popcorn.Expand(sourceType, instance, includes);
        return JsonSerializer.Serialize(expanded, _reflectionJsonOptions);
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

    [Benchmark]
    public string SimpleModel_LegacyDefault() =>
        LegacyExpandAndSerialize(_simpleModel, typeof(SimpleModel), _legacyEmptyIncludes);

    [Benchmark]
    public string SimpleModel_LegacyAll() =>
        LegacyExpandAndSerialize(_simpleModel, typeof(SimpleModel), _legacySimpleAllIncludes);

    [Benchmark]
    public string SimpleModel_LegacyCustom() =>
        LegacyExpandAndSerialize(_simpleModel, typeof(SimpleModel), _legacySimpleCustomIncludes);

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

    [Benchmark]
    public string SimpleModelList_LegacyDefault() =>
        LegacyExpandAndSerialize(_simpleModelList, typeof(List<SimpleModel>), _legacyEmptyIncludes);

    [Benchmark]
    public string SimpleModelList_LegacyAll() =>
        LegacyExpandAndSerialize(_simpleModelList, typeof(List<SimpleModel>), _legacySimpleAllIncludes);

    [Benchmark]
    public string SimpleModelList_LegacyCustom() =>
        LegacyExpandAndSerialize(_simpleModelList, typeof(List<SimpleModel>), _legacySimpleCustomIncludes);

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

    [Benchmark]
    public string ComplexModel_LegacyDefault() =>
        LegacyExpandAndSerialize(_complexModel, typeof(ComplexNestedModel), _legacyEmptyIncludes);

    [Benchmark]
    public string ComplexModel_LegacyAll() =>
        LegacyExpandAndSerialize(_complexModel, typeof(ComplexNestedModel), _legacyComplexAllIncludes);

    [Benchmark]
    public string ComplexModel_LegacyCustom() =>
        LegacyExpandAndSerialize(_complexModel, typeof(ComplexNestedModel), _legacyComplexCustomIncludes);

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

    [Benchmark]
    public string ComplexModelList_LegacyDefault() =>
        LegacyExpandAndSerialize(_complexModelList, typeof(List<ComplexNestedModel>), _legacyEmptyIncludes);

    [Benchmark]
    public string ComplexModelList_LegacyAll() =>
        LegacyExpandAndSerialize(_complexModelList, typeof(List<ComplexNestedModel>), _legacyComplexAllIncludes);

    [Benchmark]
    public string ComplexModelList_LegacyCustom() =>
        LegacyExpandAndSerialize(_complexModelList, typeof(List<ComplexNestedModel>), _legacyComplexCustomIncludes);
}
