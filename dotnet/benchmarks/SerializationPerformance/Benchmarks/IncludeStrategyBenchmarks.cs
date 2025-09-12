using BenchmarkDotNet.Attributes;
using SerializationPerformance.Models;
using System.Text.Json;
using Popcorn.Shared;

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

    private List<PropertyReference> _emptyIncludes = new();
    private List<PropertyReference> _defaultIncludes = new() { new PropertyReference { Name = "!default".AsMemory(), Negated = false, Children = null } };
    private List<PropertyReference> _allIncludes = new() { new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null } };
    
    // Simple custom includes [Id,Title,Timestamp]
    private List<PropertyReference> _simpleCustomIncludes = new()
    {
        new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Title".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Timestamp".AsMemory(), Negated = false, Children = null }
    };
    
    // Complex custom includes [Id,Title,Details[Id,Name],Items[Id,Name,IsActive]]
    private List<PropertyReference> _complexCustomIncludes = new()
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
                new PropertyReference { Name = "Name".AsMemory(), Negated = false, Children = null },
                new PropertyReference { Name = "IsActive".AsMemory(), Negated = false, Children = null }
            }
        }
    };
    
    // Negated includes [!all,-SecretData,-Priority]
    private List<PropertyReference> _negatedIncludes = new()
    {
        new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "SecretData".AsMemory(), Negated = true, Children = null },
        new PropertyReference { Name = "Priority".AsMemory(), Negated = true, Children = null }
    };
    
    // Always fields only for AttributeHeavyModel [Id,Name,IsActive]
    private List<PropertyReference> _alwaysFieldsOnly = new()
    {
        new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Name".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "IsActive".AsMemory(), Negated = false, Children = null }
    };
    
    // Exclude never fields [!all,-Password,-InternalNotes,-Metadata]
    private List<PropertyReference> _excludeNeverFields = new()
    {
        new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Password".AsMemory(), Negated = true, Children = null },
        new PropertyReference { Name = "InternalNotes".AsMemory(), Negated = true, Children = null },
        new PropertyReference { Name = "Metadata".AsMemory(), Negated = true, Children = null }
    };

    private ApiResponse<List<ComplexNestedModel>> _complexEmptyResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexDefaultResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexAllResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexSimpleCustomResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexComplexCustomResponse;
    private ApiResponse<List<ComplexNestedModel>> _complexNegatedResponse;
    
    private ApiResponse<List<AttributeHeavyModel>> _attributeEmptyResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeDefaultResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeAllResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeAlwaysOnlyResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeExcludeNeverResponse;

    [GlobalSetup]
    public void Setup()
    {
        _complexModels = TestDataGenerator.CreateComplexNestedModelList(50, maxDepth: 3);
        _attributeHeavyModels = TestDataGenerator.CreateAttributeHeavyModelList(100);

        _standardJsonOptions.AddPopcornOptions();

        // Initialize complex model responses
        _complexEmptyResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _complexModels
        });
        _complexDefaultResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _defaultIncludes,
            Data = _complexModels
        });
        _complexAllResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _complexModels
        });
        _complexSimpleCustomResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _simpleCustomIncludes,
            Data = _complexModels
        });
        _complexComplexCustomResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _complexCustomIncludes,
            Data = _complexModels
        });
        _complexNegatedResponse = new ApiResponse<List<ComplexNestedModel>>(new Pop<List<ComplexNestedModel>>
        {
            PropertyReferences = _negatedIncludes,
            Data = _complexModels
        });

        // Initialize attribute heavy model responses
        _attributeEmptyResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _attributeHeavyModels
        });
        _attributeDefaultResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _defaultIncludes,
            Data = _attributeHeavyModels
        });
        _attributeAllResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _attributeHeavyModels
        });
        _attributeAlwaysOnlyResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _alwaysFieldsOnly,
            Data = _attributeHeavyModels
        });
        _attributeExcludeNeverResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _excludeNeverFields,
            Data = _attributeHeavyModels
        });
    }

    [Benchmark(Baseline = true)]
    public string ComplexModel_EmptyIncludes()
    {
        return JsonSerializer.Serialize(_complexEmptyResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_DefaultIncludes()
    {
        return JsonSerializer.Serialize(_complexDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_AllIncludes()
    {
        return JsonSerializer.Serialize(_complexAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_SimpleCustomIncludes()
    {
        return JsonSerializer.Serialize(_complexSimpleCustomResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_ComplexCustomIncludes()
    {
        return JsonSerializer.Serialize(_complexComplexCustomResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string ComplexModel_NegatedIncludes()
    {
        return JsonSerializer.Serialize(_complexNegatedResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_EmptyIncludes()
    {
        return JsonSerializer.Serialize(_attributeEmptyResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_DefaultIncludes()
    {
        return JsonSerializer.Serialize(_attributeDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_AllIncludes()
    {
        return JsonSerializer.Serialize(_attributeAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_AlwaysFieldsOnly()
    {
        return JsonSerializer.Serialize(_attributeAlwaysOnlyResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavyModel_ExcludeNeverFields()
    {
        return JsonSerializer.Serialize(_attributeExcludeNeverResponse, _standardJsonOptions);
    }
}
