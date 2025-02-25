using System.Text.Json.Serialization;
using Popcorn.FunctionalTests.Models;
using Popcorn.Shared;

namespace Popcorn.FunctionalTests
{
    [JsonSerializable(typeof(ApiResponse<ItemModel>))]
    [JsonSerializable(typeof(ApiResponse<TestModel>))]
    [JsonSerializable(typeof(ApiResponse<AlwaysAttributeTestModel>))]
    [JsonSerializable(typeof(ApiResponse<NestedAlwaysAttributeTestModel>))]
    [JsonSerializable(typeof(ApiResponse<CollectionAlwaysAttributeTestModel>))]
    [JsonSerializable(typeof(ApiResponse<ConflictingAttributesTestModel>))]
    [JsonSerializable(typeof(ApiResponse<IncludeParameterTestModel>))]
    [JsonSerializable(typeof(ApiResponse<PrimitiveTypesTestModel>))]
    partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
