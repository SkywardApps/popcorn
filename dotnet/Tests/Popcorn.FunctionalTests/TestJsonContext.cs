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
    [JsonSerializable(typeof(ApiResponse<ValueTypesTestModel>))]
    [JsonSerializable(typeof(ApiResponse<NoAttributesModel>))]
    [JsonSerializable(typeof(ApiResponse<SingleDefaultModel>))]
    [JsonSerializable(typeof(ApiResponse<SingleAlwaysModel>))]
    [JsonSerializable(typeof(ApiResponse<SingleNeverModel>))]
    [JsonSerializable(typeof(ApiResponse<MixedAttributesModel>))]
    partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
