using Popcorn.Shared;
using SerializationPerformance.Models;
using System.Text.Json.Serialization;

namespace MatrixPerformance;

// Minimal JsonSerializerContext for the matrix pass. Only the five types that back the
// benchmarks below are registered — keeps the AOT binary small and the generator work
// focused.
[JsonSerializable(typeof(List<SimpleModel>))]
[JsonSerializable(typeof(List<ComplexNestedModel>))]

[JsonSerializable(typeof(ApiResponse<List<SimpleModel>>))]
[JsonSerializable(typeof(ApiResponse<List<ComplexNestedModel>>))]
public partial class MatrixJsonContext : JsonSerializerContext
{
}
