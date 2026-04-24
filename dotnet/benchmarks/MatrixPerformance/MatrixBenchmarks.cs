using BenchmarkDotNet.Attributes;
using Popcorn.Shared;
using SerializationPerformance.Models;
using System.Text.Json;

namespace MatrixPerformance;

// The five benchmarks that back the ratio gate, minus the legacy PopcornNetStandard
// comparisons (not AOT-safe) and minus the redundant per-shape non-List variants
// (little signal beyond what the List shapes already show).
//
// This class is run across 6 jobs: { net8, net9, net10 } x { JIT, AOT } — see Program.cs.
[MemoryDiagnoser]
public class MatrixBenchmarks
{
    private List<SimpleModel> _simpleModelList = null!;
    private List<ComplexNestedModel> _complexModelList = null!;

    private readonly JsonSerializerOptions _stjOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = MatrixJsonContext.Default,
    };

    private readonly JsonSerializerOptions _popcornOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = MatrixJsonContext.Default,
    };

    private readonly List<PropertyReference> _emptyIncludes = new();
    private readonly List<PropertyReference> _allIncludes = new()
    {
        new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null }
    };

    private ApiResponse<List<SimpleModel>> _simpleListPopcornAll = null!;
    private ApiResponse<List<ComplexNestedModel>> _complexListPopcornAll = null!;
    private ApiResponse<List<ComplexNestedModel>> _complexListPopcornDefault = null!;

    [GlobalSetup]
    public void Setup()
    {
        _simpleModelList = TestDataGenerator.CreateSimpleModelList(100);
        _complexModelList = TestDataGenerator.CreateComplexNestedModelList(25);

        _popcornOptions.AddPopcornOptions();

        _simpleListPopcornAll = new ApiResponse<List<SimpleModel>>(
            new Pop<List<SimpleModel>> { PropertyReferences = _allIncludes, Data = _simpleModelList });

        _complexListPopcornAll = new ApiResponse<List<ComplexNestedModel>>(
            new Pop<List<ComplexNestedModel>> { PropertyReferences = _allIncludes, Data = _complexModelList });

        _complexListPopcornDefault = new ApiResponse<List<ComplexNestedModel>>(
            new Pop<List<ComplexNestedModel>> { PropertyReferences = _emptyIncludes, Data = _complexModelList });
    }

    [Benchmark(Baseline = true)]
    public string SimpleModelList_Stj_SourceGen() =>
        JsonSerializer.Serialize(_simpleModelList, _stjOptions);

    [Benchmark]
    public string SimpleModelList_PopcornAll() =>
        JsonSerializer.Serialize(_simpleListPopcornAll, _popcornOptions);

    [Benchmark]
    public string ComplexModelList_Stj_SourceGen() =>
        JsonSerializer.Serialize(_complexModelList, _stjOptions);

    [Benchmark]
    public string ComplexModelList_PopcornAll() =>
        JsonSerializer.Serialize(_complexListPopcornAll, _popcornOptions);

    [Benchmark]
    public string ComplexModelList_PopcornDefault() =>
        JsonSerializer.Serialize(_complexListPopcornDefault, _popcornOptions);
}
