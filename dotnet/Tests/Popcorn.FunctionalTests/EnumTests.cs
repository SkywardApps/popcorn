using System.Text.Json;
using System.Text.Json.Serialization;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class EnumTests
    {
        private static EnumTestModel Sample() => new()
        {
            FavoriteColor = Color.Green,
            NullableColor = Color.Blue,
            UserPermissions = Permissions.ReadWrite,
            ColorList = new() { Color.Red, Color.Green },
            StatusColor = Color.Red,
        };

        [Fact]
        public async Task Enum_DefaultInclude_EmitsDefaultAndAlwaysEnums()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("FavoriteColor"));
            Assert.True(data.HasProperty("StatusColor"));
            Assert.False(data.HasProperty("NullableColor"));
            Assert.False(data.HasProperty("UserPermissions"));
        }

        [Fact]
        public async Task Enum_AllInclude_EmitsEveryEnumProperty()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("FavoriteColor"));
            Assert.True(data.HasProperty("NullableColor"));
            Assert.True(data.HasProperty("UserPermissions"));
            Assert.True(data.HasProperty("ColorList"));
            Assert.True(data.HasProperty("StatusColor"));
        }

        [Fact]
        public async Task Enum_DefaultSerialization_EmitsNumericValue()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FavoriteColor]");
            var value = doc.GetData().GetProperty("FavoriteColor");

            Assert.Equal(JsonValueKind.Number, value.ValueKind);
            Assert.Equal((int)Color.Green, value.GetInt32());
        }

        [Fact]
        public async Task Enum_NullableWithValue_EmitsNumericValue()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[NullableColor]");
            var value = doc.GetData().GetProperty("NullableColor");

            Assert.Equal(JsonValueKind.Number, value.ValueKind);
            Assert.Equal((int)Color.Blue, value.GetInt32());
        }

        [Fact]
        public async Task Enum_NullableWithNull_EmitsNull()
        {
            var model = Sample();
            model.NullableColor = null;
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[NullableColor]");
            var value = doc.GetData().GetProperty("NullableColor");

            Assert.Equal(JsonValueKind.Null, value.ValueKind);
        }

        [Fact]
        public async Task Enum_Flags_EmitsCombinedNumericValue()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[UserPermissions]");
            var value = doc.GetData().GetProperty("UserPermissions");

            Assert.Equal(JsonValueKind.Number, value.ValueKind);
            Assert.Equal((int)Permissions.ReadWrite, value.GetInt32());
        }

        [Fact]
        public async Task Enum_InCollection_EmitsArrayOfEnumValues()
        {
            using var server = TestServerHelper.CreateServer(Sample());
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ColorList]");
            var list = doc.GetData().GetProperty("ColorList");

            Assert.Equal(JsonValueKind.Array, list.ValueKind);
            Assert.Equal(2, list.GetArrayLength());
            Assert.Equal((int)Color.Red, list[0].GetInt32());
            Assert.Equal((int)Color.Green, list[1].GetInt32());
        }

        // ----- String-form configuration -----

        [Fact]
        public async Task Enum_GlobalJsonStringEnumConverter_EmitsEnumName()
        {
            using var server = TestServerHelper.CreateServer(
                Sample(),
                opts => opts.Converters.Add(new JsonStringEnumConverter()));
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FavoriteColor]");
            var value = doc.GetData().GetProperty("FavoriteColor");

            Assert.Equal(JsonValueKind.String, value.ValueKind);
            Assert.Equal("Green", value.GetString());
        }

        [Fact]
        public async Task Enum_GlobalJsonStringEnumConverter_NullableEnum_EmitsName()
        {
            using var server = TestServerHelper.CreateServer(
                Sample(),
                opts => opts.Converters.Add(new JsonStringEnumConverter()));
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[NullableColor]");
            var value = doc.GetData().GetProperty("NullableColor");

            Assert.Equal(JsonValueKind.String, value.ValueKind);
            Assert.Equal("Blue", value.GetString());
        }

        [Fact]
        public async Task Enum_GlobalJsonStringEnumConverter_FlagsEnum_EmitsNames()
        {
            using var server = TestServerHelper.CreateServer(
                Sample(),
                opts => opts.Converters.Add(new JsonStringEnumConverter()));
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[UserPermissions]");
            var value = doc.GetData().GetProperty("UserPermissions");

            Assert.Equal(JsonValueKind.String, value.ValueKind);
            var s = value.GetString()!;
            Assert.Contains("Read", s);
            Assert.Contains("Write", s);
        }

        [Fact]
        public async Task Enum_GlobalJsonStringEnumConverter_InCollection_EmitsArrayOfNames()
        {
            using var server = TestServerHelper.CreateServer(
                Sample(),
                opts => opts.Converters.Add(new JsonStringEnumConverter()));
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[ColorList]");
            var list = doc.GetData().GetProperty("ColorList");

            Assert.Equal(JsonValueKind.Array, list.ValueKind);
            Assert.Equal(2, list.GetArrayLength());
            Assert.Equal("Red", list[0].GetString());
            Assert.Equal("Green", list[1].GetString());
        }

        [Fact]
        public async Task Enum_PerTypeJsonConverterAttribute_EmitsNameWithoutGlobalConfig()
        {
            // Season has [JsonConverter(typeof(JsonStringEnumConverter))] on its declaration;
            // Color does not. Both appear on the same model. Without any global registration,
            // Season should emit as a string, Color as a number.
            var model = new StringEnumTestModel { CurrentSeason = Season.Autumn, FavoriteColor = Color.Red };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            var season = data.GetProperty("CurrentSeason");
            Assert.Equal(JsonValueKind.String, season.ValueKind);
            Assert.Equal("Autumn", season.GetString());

            var color = data.GetProperty("FavoriteColor");
            Assert.Equal(JsonValueKind.Number, color.ValueKind);
            Assert.Equal((int)Color.Red, color.GetInt32());
        }

        [Fact]
        public async Task Enum_CamelCaseNamingPolicy_AppliedToEnumString()
        {
            // JsonStringEnumConverter accepts a naming policy; confirm it flows through.
            using var server = TestServerHelper.CreateServer(
                Sample(),
                opts => opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FavoriteColor]");
            var value = doc.GetData().GetProperty("FavoriteColor");

            Assert.Equal(JsonValueKind.String, value.ValueKind);
            Assert.Equal("green", value.GetString());
        }
    }
}
