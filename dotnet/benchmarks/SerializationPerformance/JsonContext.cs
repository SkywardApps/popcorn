using Popcorn.Shared;
using SerializationPerformance.Models;
using System.Text.Json.Serialization;

namespace SerializationPerformance;

[JsonSerializable(typeof(SimpleModel))]
[JsonSerializable(typeof(List<SimpleModel>))]
[JsonSerializable(typeof(ComplexNestedModel))]
[JsonSerializable(typeof(List<ComplexNestedModel>))]
[JsonSerializable(typeof(CircularReferenceModel))]
[JsonSerializable(typeof(List<CircularReferenceModel>))]
[JsonSerializable(typeof(ScalableModel))]
[JsonSerializable(typeof(List<ScalableModel>))]
[JsonSerializable(typeof(DeepNestingModel))]
[JsonSerializable(typeof(AttributeHeavyModel))]
[JsonSerializable(typeof(List<AttributeHeavyModel>))]
[JsonSerializable(typeof(PropertyMappingModel))]
[JsonSerializable(typeof(List<PropertyMappingModel>))]

[JsonSerializable(typeof(ApiResponse<SimpleModel>))]
[JsonSerializable(typeof(ApiResponse<List<SimpleModel>>))]
[JsonSerializable(typeof(ApiResponse<ComplexNestedModel>))]
[JsonSerializable(typeof(ApiResponse<List<ComplexNestedModel>>))]
[JsonSerializable(typeof(ApiResponse<CircularReferenceModel>))]
[JsonSerializable(typeof(ApiResponse<List<CircularReferenceModel>>))]
[JsonSerializable(typeof(ApiResponse<ScalableModel>))]
[JsonSerializable(typeof(ApiResponse<List<ScalableModel>>))]
[JsonSerializable(typeof(ApiResponse<DeepNestingModel>))]
[JsonSerializable(typeof(ApiResponse<AttributeHeavyModel>))]
[JsonSerializable(typeof(ApiResponse<List<AttributeHeavyModel>>))]
[JsonSerializable(typeof(ApiResponse<PropertyMappingModel>))]
[JsonSerializable(typeof(ApiResponse<List<PropertyMappingModel>>))]
public partial class BenchmarkJsonContext : JsonSerializerContext
{
}
