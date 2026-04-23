using System.Collections.Generic;
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
    [JsonSerializable(typeof(ApiResponse<BasicCollectionTypesModel>))]
    [JsonSerializable(typeof(ApiResponse<CollectionEdgeCasesModel>))]
    [JsonSerializable(typeof(ApiResponse<DictionaryTypesModel>))]
    [JsonSerializable(typeof(ApiResponse<CollectionPropertyInclusionModel>))]
    [JsonSerializable(typeof(ApiResponse<EnumTestModel>))]
    [JsonSerializable(typeof(ApiResponse<StringEnumTestModel>))]
    [JsonSerializable(typeof(ApiResponse<JsonPropertyNameModel>))]
    [JsonSerializable(typeof(ApiResponse<Vehicle>))]
    [JsonSerializable(typeof(ApiResponse<Car>))]
    [JsonSerializable(typeof(ApiResponse<Truck>))]
    [JsonSerializable(typeof(ApiResponse<VehicleCollection>))]
    [JsonSerializable(typeof(ApiResponse<List<Vehicle>>))]
    [JsonSerializable(typeof(ApiResponse<PageModel<ItemPayload>>))]
    [JsonSerializable(typeof(ApiResponse<ErrorHandlingModel>))]
    [JsonSerializable(typeof(ApiResponse<ExplodingModel>))]
    [JsonSerializable(typeof(ApiResponse<PersonWithComputed>))]
    [JsonSerializable(typeof(ApiResponse<CarSource>))]
    [JsonSerializable(typeof(ApiResponse<CarProjection>))]
    [JsonSerializable(typeof(ApiResponse<NullabilityCoverageModel>))]
    // Root-level nullability edge cases. Exercise:
    //   - Nullable<int> at root (distinct CLR type from int; needs its own Pop<int?> body).
    //   - Nullable<NullStruct> at root (user-defined struct wrapped in Nullable<T>).
    //   - ApiResponse<string?> / ApiResponse<string> at root — NRT-annotation variants collapse to one converter.
    //   - List<int?> / Dictionary<string,int?> at root — nullable value-type element/value top-level.
    //   - NullClass vs NullClass? — NRT annotation differs but CLR type identical; coexist on one converter.
    [JsonSerializable(typeof(ApiResponse<int?>))]
    [JsonSerializable(typeof(ApiResponse<NullStruct?>))]
    [JsonSerializable(typeof(ApiResponse<string?>))]
    [JsonSerializable(typeof(ApiResponse<List<int?>>))]
    [JsonSerializable(typeof(ApiResponse<Dictionary<string, int?>>))]
    [JsonSerializable(typeof(ApiResponse<NullClass>))]
    [JsonSerializable(typeof(ApiResponse<NullClass?>))]
    [JsonSerializable(typeof(ApiResponse<EnvelopePayload>))]
    [JsonSerializable(typeof(MyTestEnvelope<EnvelopePayload>))]
    [JsonSerializable(typeof(DerivedEnvelope<EnvelopePayload>))]
    [JsonSerializable(typeof(NestedEnvelopeContainer.NestedEnvelope<EnvelopePayload>))]
    [JsonSerializable(typeof(RecordEnvelope<EnvelopePayload>))]
    [JsonSerializable(typeof(RenamedMarkerEnvelope<EnvelopePayload>))]
    [JsonSerializable(typeof(AlternateEnvelope<EnvelopePayload>))]
    partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
