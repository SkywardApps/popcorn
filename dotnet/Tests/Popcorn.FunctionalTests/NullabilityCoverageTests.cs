using System.Collections.Generic;
using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // Exercises every (type-kind × position × container-nullability × element-nullability) cell
    // from NullabilityCoverageModel. For each permutation we check:
    //   A. A populated model round-trips to the expected JSON shape.
    //   B. Every nullable slot accepts a real null without crashing and renders as JSON null.
    //
    // Compile-time nullability warnings on the generator output are asserted separately in
    // Popcorn.SourceGenerator.Tests (NullabilityDiagnosticsTests).
    public class NullabilityCoverageTests
    {
        // ----------------------------------------------------------------
        // A) Populated — every permutation set to a non-null, meaningful value.
        // ----------------------------------------------------------------

        [Fact]
        public async Task AllPermutations_Populated_SerializeWithoutError()
        {
            var model = BuildFullyPopulated();
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            // Spot-check one slot per category — the important thing is that the call completes
            // and every Pop<T> dispatch resolves to a matching converter.
            Assert.Equal(7, data.GetProperty("Int").GetInt32());
            Assert.Equal(7, data.GetProperty("QInt").GetInt32());
            Assert.Equal("s", data.GetProperty("String").GetString());
            Assert.Equal("s", data.GetProperty("QString").GetString());
            Assert.Equal(1, data.GetProperty("Struct").GetProperty("X").GetInt32());
            Assert.Equal(1, data.GetProperty("QStruct").GetProperty("X").GetInt32());
            Assert.Equal(1, data.GetProperty("Class").GetProperty("Id").GetInt32());
            Assert.Equal(1, data.GetProperty("QClass").GetProperty("Id").GetInt32());

            Assert.Equal(3, data.GetProperty("ListInt").GetArrayLength());
            Assert.Equal(3, data.GetProperty("ListQInt").GetArrayLength());
            Assert.Equal(3, data.GetProperty("ListString").GetArrayLength());
            Assert.Equal(3, data.GetProperty("ListQString").GetArrayLength());
            Assert.Equal(2, data.GetProperty("ListStruct").GetArrayLength());
            Assert.Equal(2, data.GetProperty("ListQStruct").GetArrayLength());
            Assert.Equal(2, data.GetProperty("ListClass").GetArrayLength());
            Assert.Equal(2, data.GetProperty("ListQClass").GetArrayLength());

            Assert.Equal(3, data.GetProperty("ArrInt").GetArrayLength());
            Assert.Equal(3, data.GetProperty("ArrString").GetArrayLength());
            Assert.Equal(3, data.GetProperty("ArrQString").GetArrayLength());
            Assert.Equal(2, data.GetProperty("ArrClass").GetArrayLength());
            Assert.Equal(2, data.GetProperty("ArrQClass").GetArrayLength());

            Assert.Equal(2, data.GetProperty("DictInt").EnumerateObject().Count());
            Assert.Equal(2, data.GetProperty("DictQInt").EnumerateObject().Count());
            Assert.Equal(2, data.GetProperty("DictString").EnumerateObject().Count());
            Assert.Equal(2, data.GetProperty("DictQString").EnumerateObject().Count());
            Assert.Equal(1, data.GetProperty("DictStruct").EnumerateObject().Count());
            Assert.Equal(1, data.GetProperty("DictQStruct").EnumerateObject().Count());
            Assert.Equal(1, data.GetProperty("DictClass").EnumerateObject().Count());
            Assert.Equal(1, data.GetProperty("DictQClass").EnumerateObject().Count());

            Assert.Equal(1, data.GetProperty("ListOfDictOfClass").GetArrayLength());
            Assert.Equal(1, data.GetProperty("ListOfQDictOfClass").GetArrayLength());
            Assert.Equal(1, data.GetProperty("DictOfListOfClass").EnumerateObject().Count());
            Assert.Equal(1, data.GetProperty("DictOfQListOfClass").EnumerateObject().Count());
        }

        // ----------------------------------------------------------------
        // B) Null containers — every nullable container left null.
        // ----------------------------------------------------------------

        [Fact]
        public async Task NullableScalarProperties_WhenSetToNull_EmitJsonNull()
        {
            var model = new NullabilityCoverageModel(); // defaults leave every Q-property null
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.Equal(JsonValueKind.Null, data.GetProperty("QInt").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("QString").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("QStruct").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("QClass").ValueKind);
        }

        [Fact]
        public async Task NullableListContainers_WhenSetToNull_EmitJsonNull()
        {
            var model = new NullabilityCoverageModel();
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListInt_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListQInt_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListString_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListQString_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListStruct_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListQStruct_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListClass_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ListQClass_QContainer").ValueKind);
        }

        [Fact]
        public async Task NullableArrayContainers_WhenSetToNull_EmitJsonNull()
        {
            var model = new NullabilityCoverageModel();
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.Equal(JsonValueKind.Null, data.GetProperty("ArrInt_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ArrString_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("ArrClass_QContainer").ValueKind);
        }

        [Fact]
        public async Task NullableDictionaryContainers_WhenSetToNull_EmitJsonNull()
        {
            var model = new NullabilityCoverageModel();
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictInt_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictQInt_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictString_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictQString_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictStruct_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictQStruct_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictClass_QContainer").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("DictQClass_QContainer").ValueKind);
        }

        // ----------------------------------------------------------------
        // C) Null elements — non-nullable container, elements/values are null.
        // ----------------------------------------------------------------

        [Fact]
        public async Task ListOfNullableValueType_WithNullElements_EmitsJsonNullElements()
        {
            var model = new NullabilityCoverageModel
            {
                ListQInt = new List<int?> { 1, null, 3 }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListQInt]");
            var arr = doc.GetData().GetProperty("ListQInt");
            Assert.Equal(3, arr.GetArrayLength());
            Assert.Equal(1, arr[0].GetInt32());
            Assert.Equal(JsonValueKind.Null, arr[1].ValueKind);
            Assert.Equal(3, arr[2].GetInt32());
        }

        [Fact]
        public async Task ListOfNullableString_WithNullElements_EmitsJsonNullElements()
        {
            var model = new NullabilityCoverageModel
            {
                ListQString = new List<string?> { "a", null, "c" }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListQString]");
            var arr = doc.GetData().GetProperty("ListQString");
            Assert.Equal(3, arr.GetArrayLength());
            Assert.Equal("a", arr[0].GetString());
            Assert.Equal(JsonValueKind.Null, arr[1].ValueKind);
            Assert.Equal("c", arr[2].GetString());
        }

        [Fact]
        public async Task ListOfNullableClass_WithNullElements_EmitsJsonNullElements()
        {
            var model = new NullabilityCoverageModel
            {
                ListQClass = new List<NullClass?> { new NullClass { Id = 1, Name = "x" }, null, new NullClass { Id = 3 } }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListQClass[!all]]");
            var arr = doc.GetData().GetProperty("ListQClass");
            Assert.Equal(3, arr.GetArrayLength());
            Assert.Equal(1, arr[0].GetProperty("Id").GetInt32());
            Assert.Equal(JsonValueKind.Null, arr[1].ValueKind);
            Assert.Equal(3, arr[2].GetProperty("Id").GetInt32());
        }

        [Fact]
        public async Task ListOfNullableStruct_WithNullElements_EmitsJsonNullElements()
        {
            var model = new NullabilityCoverageModel
            {
                ListQStruct = new List<NullStruct?> { new NullStruct { X = 1, Label = "a" }, null }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListQStruct]");
            var arr = doc.GetData().GetProperty("ListQStruct");
            Assert.Equal(2, arr.GetArrayLength());
            Assert.Equal(1, arr[0].GetProperty("X").GetInt32());
            Assert.Equal(JsonValueKind.Null, arr[1].ValueKind);
        }

        [Fact]
        public async Task DictionaryOfNullableClass_WithNullValues_EmitsJsonNullValues()
        {
            var model = new NullabilityCoverageModel
            {
                DictQClass = new Dictionary<string, NullClass?>
                {
                    { "a", new NullClass { Id = 1, Name = "x" } },
                    { "b", null },
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[DictQClass[!all]]");
            var dict = doc.GetData().GetProperty("DictQClass");
            Assert.Equal(1, dict.GetProperty("a").GetProperty("Id").GetInt32());
            Assert.Equal(JsonValueKind.Null, dict.GetProperty("b").ValueKind);
        }

        [Fact]
        public async Task DictionaryOfNullableInt_WithNullValues_EmitsJsonNullValues()
        {
            var model = new NullabilityCoverageModel
            {
                DictQInt = new Dictionary<string, int?> { { "a", 1 }, { "b", null } }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[DictQInt]");
            var dict = doc.GetData().GetProperty("DictQInt");
            Assert.Equal(1, dict.GetProperty("a").GetInt32());
            Assert.Equal(JsonValueKind.Null, dict.GetProperty("b").ValueKind);
        }

        // ----------------------------------------------------------------
        // D) Nested containers — container-of-container permutations.
        // ----------------------------------------------------------------

        [Fact]
        public async Task ListOfDict_PopulatesNestedStructure()
        {
            var model = new NullabilityCoverageModel
            {
                ListOfDictOfClass = new()
                {
                    new Dictionary<string, NullClass> { { "a", new NullClass { Id = 1, Name = "x" } } }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListOfDictOfClass[!all]]");
            var outer = doc.GetData().GetProperty("ListOfDictOfClass");
            Assert.Equal(1, outer.GetArrayLength());
            Assert.Equal(1, outer[0].GetProperty("a").GetProperty("Id").GetInt32());
        }

        [Fact]
        public async Task ListOfNullableDict_WithNullEntry_EmitsJsonNull()
        {
            var model = new NullabilityCoverageModel
            {
                ListOfQDictOfClass = new()
                {
                    new Dictionary<string, NullClass> { { "a", new NullClass { Id = 1 } } },
                    null
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListOfQDictOfClass[!all]]");
            var outer = doc.GetData().GetProperty("ListOfQDictOfClass");
            Assert.Equal(2, outer.GetArrayLength());
            Assert.Equal(1, outer[0].GetProperty("a").GetProperty("Id").GetInt32());
            Assert.Equal(JsonValueKind.Null, outer[1].ValueKind);
        }

        [Fact]
        public async Task DictOfNullableList_WithNullValue_EmitsJsonNull()
        {
            var model = new NullabilityCoverageModel
            {
                DictOfQListOfClass = new Dictionary<string, List<NullClass>?>
                {
                    { "present", new List<NullClass> { new NullClass { Id = 1 } } },
                    { "missing", null }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[DictOfQListOfClass[!all]]");
            var outer = doc.GetData().GetProperty("DictOfQListOfClass");
            Assert.Equal(1, outer.GetProperty("present").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, outer.GetProperty("missing").ValueKind);
        }

        // ----------------------------------------------------------------
        // E) Default-attribute propagation — verify [Default] on NullClass.Id
        // still triggers default inclusion when NullClass appears through every
        // nullability shape.
        // ----------------------------------------------------------------

        [Fact]
        public async Task DefaultAttribute_OnNullableClassProperty_StillIncludedByDefault()
        {
            var model = new NullabilityCoverageModel { QClass = new NullClass { Id = 42, Name = "hidden" } };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[QClass]");
            var qClass = doc.GetData().GetProperty("QClass");
            Assert.Equal(42, qClass.GetProperty("Id").GetInt32());
            Assert.False(qClass.HasProperty("Name"), "Name has no [Default] attribute and should not be included on a bare request");
        }

        [Fact]
        public async Task DefaultAttribute_PropagatesThroughListOfNullableClass()
        {
            var model = new NullabilityCoverageModel
            {
                ListQClass = new List<NullClass?> { new NullClass { Id = 1, Name = "x" } }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ListQClass]");
            var item = doc.GetData().GetProperty("ListQClass")[0];
            Assert.True(item.HasProperty("Id"));
            Assert.False(item.HasProperty("Name"));
        }

        [Fact]
        public async Task DefaultAttribute_PropagatesThroughNullableDictOfClass()
        {
            var model = new NullabilityCoverageModel
            {
                DictQClass = new Dictionary<string, NullClass?>
                {
                    { "a", new NullClass { Id = 1, Name = "x" } }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[DictQClass]");
            var val = doc.GetData().GetProperty("DictQClass").GetProperty("a");
            Assert.True(val.HasProperty("Id"));
            Assert.False(val.HasProperty("Name"));
        }

        // ----------------------------------------------------------------
        // F) Root-level nullability — T in ApiResponse<T> is itself nullable.
        // For value types the nullable form is a different CLR type and gets its own converter.
        // For reference types, NRT annotations collapse to a single converter.
        // ----------------------------------------------------------------

        [Fact]
        public async Task RootLevel_NullableInt_NonNull_Serializes()
        {
            int? payload = 42;
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            Assert.Equal(42, doc.RootElement.GetProperty("Data").GetInt32());
        }

        [Fact]
        public async Task RootLevel_NullableInt_Null_EmitsJsonNull()
        {
            int? payload = null;
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
        }

        [Fact]
        public async Task RootLevel_NullableStruct_NonNull_Serializes()
        {
            NullStruct? payload = new NullStruct { X = 7, Label = "a" };
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.RootElement.GetProperty("Data");
            Assert.Equal(7, data.GetProperty("X").GetInt32());
            Assert.Equal("a", data.GetProperty("Label").GetString());
        }

        [Fact]
        public async Task RootLevel_NullableStruct_Null_EmitsJsonNull()
        {
            NullStruct? payload = null;
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
        }

        [Fact]
        public async Task RootLevel_NullableString_NonNull_Serializes()
        {
            string? payload = "hello";
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            Assert.Equal("hello", doc.RootElement.GetProperty("Data").GetString());
        }

        [Fact]
        public async Task RootLevel_NullableString_Null_EmitsJsonNull()
        {
            string? payload = null;
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
        }

        [Fact]
        public async Task RootLevel_ListOfNullableInt_WithNulls_SerializesCorrectly()
        {
            var payload = new List<int?> { 1, null, 3 };
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var arr = doc.RootElement.GetProperty("Data");
            Assert.Equal(3, arr.GetArrayLength());
            Assert.Equal(1, arr[0].GetInt32());
            Assert.Equal(JsonValueKind.Null, arr[1].ValueKind);
            Assert.Equal(3, arr[2].GetInt32());
        }

        [Fact]
        public async Task RootLevel_DictOfNullableInt_WithNulls_SerializesCorrectly()
        {
            var payload = new Dictionary<string, int?> { { "a", 1 }, { "b", null } };
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var obj = doc.RootElement.GetProperty("Data");
            Assert.Equal(1, obj.GetProperty("a").GetInt32());
            Assert.Equal(JsonValueKind.Null, obj.GetProperty("b").ValueKind);
        }

        // Both ApiResponse<NullClass> and ApiResponse<NullClass?> are registered in TestJsonContext.cs.
        // The two registrations resolve to the same CLR type (NRT annotation on a reference type
        // doesn't embed into Type). The generator must collapse them to a single converter without
        // a duplicate-name compile error. This test proves both registrations route through the
        // same write path and produce identical JSON for the same input.
        [Fact]
        public async Task RootLevel_Class_AndNullableClass_CoexistThroughSameConverter()
        {
            NullClass nonNull = new NullClass { Id = 1, Name = "x" };
            NullClass? nullable = new NullClass { Id = 1, Name = "x" };

            using var server1 = TestServerHelper.CreateServer(nonNull);
            using var server2 = TestServerHelper.CreateServer(nullable);

            var doc1 = await TestServerHelper.GetJsonAsync(server1.CreateClient(), "/test?include=[!all]");
            var doc2 = await TestServerHelper.GetJsonAsync(server2.CreateClient(), "/test?include=[!all]");

            Assert.Equal(doc1.GetData().GetRawText(), doc2.GetData().GetRawText());
        }

        [Fact]
        public async Task RootLevel_NullableClass_Null_EmitsJsonNull()
        {
            NullClass? payload = null;
            using var server = TestServerHelper.CreateServer(payload);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private static NullabilityCoverageModel BuildFullyPopulated()
        {
            var c = new NullClass { Id = 1, Name = "x" };
            var s = new NullStruct { X = 1, Label = "a" };

            return new NullabilityCoverageModel
            {
                Int = 7,
                QInt = 7,
                String = "s",
                QString = "s",
                Struct = s,
                QStruct = s,
                Class = c,
                QClass = c,

                ListInt = new() { 1, 2, 3 },
                ListInt_QContainer = new() { 1, 2, 3 },
                ListQInt = new() { 1, null, 3 },
                ListQInt_QContainer = new() { 1, null, 3 },

                ListString = new() { "a", "b", "c" },
                ListString_QContainer = new() { "a", "b", "c" },
                ListQString = new() { "a", null, "c" },
                ListQString_QContainer = new() { "a", null, "c" },

                ListStruct = new() { s, s },
                ListStruct_QContainer = new() { s, s },
                ListQStruct = new() { s, null },
                ListQStruct_QContainer = new() { s, null },

                ListClass = new() { c, c },
                ListClass_QContainer = new() { c, c },
                ListQClass = new() { c, null },
                ListQClass_QContainer = new() { c, null },

                ArrInt = new[] { 1, 2, 3 },
                ArrInt_QContainer = new[] { 1, 2, 3 },
                ArrString = new[] { "a", "b", "c" },
                ArrString_QContainer = new[] { "a", "b", "c" },
                ArrQString = new[] { "a", null, "c" },
                ArrClass = new[] { c, c },
                ArrQClass = new NullClass?[] { c, null },
                ArrClass_QContainer = new[] { c },

                DictInt = new() { { "a", 1 }, { "b", 2 } },
                DictInt_QContainer = new() { { "a", 1 }, { "b", 2 } },
                DictQInt = new() { { "a", 1 }, { "b", null } },
                DictQInt_QContainer = new() { { "a", 1 }, { "b", null } },

                DictString = new() { { "a", "x" }, { "b", "y" } },
                DictString_QContainer = new() { { "a", "x" }, { "b", "y" } },
                DictQString = new() { { "a", "x" }, { "b", null } },
                DictQString_QContainer = new() { { "a", "x" }, { "b", null } },

                DictStruct = new() { { "a", s } },
                DictStruct_QContainer = new() { { "a", s } },
                DictQStruct = new() { { "a", null } },
                DictQStruct_QContainer = new() { { "a", null } },

                DictClass = new() { { "a", c } },
                DictClass_QContainer = new() { { "a", c } },
                DictQClass = new() { { "a", null } },
                DictQClass_QContainer = new() { { "a", null } },

                ListOfDictOfClass = new()
                {
                    new Dictionary<string, NullClass> { { "a", c } },
                },
                ListOfQDictOfClass = new()
                {
                    new Dictionary<string, NullClass> { { "a", c } },
                },
                DictOfListOfClass = new()
                {
                    { "a", new List<NullClass> { c } }
                },
                DictOfQListOfClass = new()
                {
                    { "a", new List<NullClass> { c } }
                },
            };
        }
    }
}
