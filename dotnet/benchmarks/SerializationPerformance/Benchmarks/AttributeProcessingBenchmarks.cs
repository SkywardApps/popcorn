using BenchmarkDotNet.Attributes;
using Popcorn.Shared;
using SerializationPerformance.Models;
using System.Text.Json;

namespace SerializationPerformance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class AttributeProcessingBenchmarks
{
    private List<SimpleModel> _simpleModels = null!;
    private List<AttributeHeavyModel> _attributeHeavyModels = null!;
    private List<PropertyMappingModel> _propertyMappingModels = null!;

    private readonly JsonSerializerOptions _standardJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private List<PropertyReference> _emptyIncludes = new();
    private List<PropertyReference> _allIncludes = new() { new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null } };
    private List<PropertyReference> _defaultIncludes = new() { new PropertyReference { Name = "!default".AsMemory(), Negated = false, Children = null } };
    
    // Always fields only for AttributeHeavyModel [Id,Name,IsActive]  
    private List<PropertyReference> _alwaysFieldsOnly = new()
    {
        new PropertyReference { Name = "Id".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Name".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "IsActive".AsMemory(), Negated = false, Children = null }
    };
    
    // Exclude Never fields [!all,-Password,-InternalNotes,-Metadata]
    private List<PropertyReference> _excludeNeverFields = new()
    {
        new PropertyReference { Name = "!all".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "Password".AsMemory(), Negated = true, Children = null },
        new PropertyReference { Name = "InternalNotes".AsMemory(), Negated = true, Children = null },
        new PropertyReference { Name = "Metadata".AsMemory(), Negated = true, Children = null }
    };
    
    // Custom includes for PropertyMappingModel [UserId,FullName,EmailAddress,IsVerified]
    private List<PropertyReference> _propertyMappingCustomIncludes = new()
    {
        new PropertyReference { Name = "UserId".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "FullName".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "EmailAddress".AsMemory(), Negated = false, Children = null },
        new PropertyReference { Name = "IsVerified".AsMemory(), Negated = false, Children = null }
    };

    private ApiResponse<List<SimpleModel>> _simpleDefaultResponse;
    private ApiResponse<List<SimpleModel>> _simpleAllResponse;
    
    private ApiResponse<List<AttributeHeavyModel>> _attributeDefaultResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeAllResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeAlwaysOnlyResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeDefaultOnlyResponse;
    private ApiResponse<List<AttributeHeavyModel>> _attributeExcludeNeverResponse;
    
    private ApiResponse<List<PropertyMappingModel>> _propertyMappingDefaultResponse;
    private ApiResponse<List<PropertyMappingModel>> _propertyMappingAllResponse;
    private ApiResponse<List<PropertyMappingModel>> _propertyMappingCustomResponse;
    
    private ApiResponse<List<SimpleModel>> _minimalAttributesResponse;
    private ApiResponse<List<AttributeHeavyModel>> _heavyAttributesResponse;


    [GlobalSetup]
    public void Setup()
    {
        _simpleModels = TestDataGenerator.CreateSimpleModelList(200);
        _attributeHeavyModels = TestDataGenerator.CreateAttributeHeavyModelList(200);
        _propertyMappingModels = TestDataGenerator.CreatePropertyMappingModelList(200);

        _standardJsonOptions.AddPopcornOptions();

        // Initialize simple model responses
        _simpleDefaultResponse = new ApiResponse<List<SimpleModel>>(new Pop<List<SimpleModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _simpleModels
        });
        _simpleAllResponse = new ApiResponse<List<SimpleModel>>(new Pop<List<SimpleModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _simpleModels
        });

        // Initialize attribute heavy model responses
        _attributeDefaultResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _emptyIncludes,
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
        _attributeDefaultOnlyResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _defaultIncludes,
            Data = _attributeHeavyModels
        });
        _attributeExcludeNeverResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _excludeNeverFields,
            Data = _attributeHeavyModels
        });

        // Initialize property mapping model responses
        _propertyMappingDefaultResponse = new ApiResponse<List<PropertyMappingModel>>(new Pop<List<PropertyMappingModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _propertyMappingModels
        });
        _propertyMappingAllResponse = new ApiResponse<List<PropertyMappingModel>>(new Pop<List<PropertyMappingModel>>
        {
            PropertyReferences = _allIncludes,
            Data = _propertyMappingModels
        });
        _propertyMappingCustomResponse = new ApiResponse<List<PropertyMappingModel>>(new Pop<List<PropertyMappingModel>>
        {
            PropertyReferences = _propertyMappingCustomIncludes,
            Data = _propertyMappingModels
        });

        // Initialize attribute overhead comparison responses
        _minimalAttributesResponse = new ApiResponse<List<SimpleModel>>(new Pop<List<SimpleModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _simpleModels
        });
        _heavyAttributesResponse = new ApiResponse<List<AttributeHeavyModel>>(new Pop<List<AttributeHeavyModel>>
        {
            PropertyReferences = _emptyIncludes,
            Data = _attributeHeavyModels
        });
    }

    // Baseline tests with minimal attributes
    [Benchmark(Baseline = true)]
    public string SimpleModels_StandardJson()
    {
        return JsonSerializer.Serialize(_simpleModels, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModels_PopcornDefault()
    {
        return JsonSerializer.Serialize(_simpleDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string SimpleModels_PopcornAll()
    {
        return JsonSerializer.Serialize(_simpleAllResponse, _standardJsonOptions);
    }

    // Heavy attribute processing tests
    [Benchmark]
    public string AttributeHeavy_StandardJson()
    {
        return JsonSerializer.Serialize(_attributeHeavyModels, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornDefault()
    {
        return JsonSerializer.Serialize(_attributeDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornAll()
    {
        return JsonSerializer.Serialize(_attributeAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornAlwaysOnly()
    {
        return JsonSerializer.Serialize(_attributeAlwaysOnlyResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornDefaultOnly()
    {
        return JsonSerializer.Serialize(_attributeDefaultOnlyResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string AttributeHeavy_PopcornExcludeNever()
    {
        return JsonSerializer.Serialize(_attributeExcludeNeverResponse, _standardJsonOptions);
    }

    // Property name mapping tests
    [Benchmark]
    public string PropertyMapping_StandardJson()
    {
        return JsonSerializer.Serialize(_propertyMappingModels, _standardJsonOptions);
    }

    [Benchmark]
    public string PropertyMapping_PopcornDefault()
    {
        return JsonSerializer.Serialize(_propertyMappingDefaultResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string PropertyMapping_PopcornAll()
    {
        return JsonSerializer.Serialize(_propertyMappingAllResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string PropertyMapping_PopcornCustomIncludes()
    {
        return JsonSerializer.Serialize(_propertyMappingCustomResponse, _standardJsonOptions);
    }

    // Attribute processing overhead comparison
    [Benchmark]
    public string MinimalAttributes_Popcorn()
    {
        return JsonSerializer.Serialize(_minimalAttributesResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string HeavyAttributes_Popcorn()
    {
        return JsonSerializer.Serialize(_heavyAttributesResponse, _standardJsonOptions);
    }

    [Benchmark]
    public string NoAttributes_StandardJson()
    {
        // Create models without any Popcorn attributes for comparison
        var plainModels = _simpleModels.Select(m => new 
        {
            m.Id,
            m.Name,
            m.CreatedAt,
            m.Description,
            m.IsActive
        }).ToList();
        
        return JsonSerializer.Serialize(plainModels, _standardJsonOptions);
    }
}
