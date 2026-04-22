using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class PolymorphismTests
    {
        [Fact]
        public async Task DerivedType_SerializedAsBaseType_EmitsBaseProperties()
        {
            Vehicle model = new Car { Id = 1, Make = "Toyota", Model = "Camry", Year = 2024, NumberOfDoors = 4, BodyStyle = "Sedan" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("Id"));
            Assert.True(data.HasProperty("Make"));
            Assert.True(data.HasProperty("Model"));
            Assert.True(data.HasProperty("Year"));
        }

        [Fact]
        public async Task DerivedType_InheritedAttributesApply()
        {
            var model = new Car { Id = 1, Make = "Toyota", Model = "Camry", Year = 2024, NumberOfDoors = 4 };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("Id"));
            Assert.True(data.HasProperty("Make"));
            Assert.True(data.HasProperty("Model"));
            Assert.False(data.HasProperty("Year"));
        }

        [Fact(Skip = "Pending: polymorphic collection serialization — generator must emit type-dispatch for base-typed collections. See apiDesign.md and migrationAnalysis.md (polymorphic unknown-at-build-time is the only non-starter; registered derived types must still work).")]
        public async Task PolymorphicCollection_RegisteredDerivedTypes_SerializedWithDerivedProperties()
        {
            var collection = new VehicleCollection
            {
                Owner = "Liz",
                Vehicles = new()
                {
                    new Car { Id = 1, Make = "Toyota", Model = "Camry", NumberOfDoors = 4, BodyStyle = "Sedan" },
                    new Truck { Id = 2, Make = "Ford", Model = "F-150", PayloadCapacity = 1000, HasTrailer = true },
                },
            };
            using var server = TestServerHelper.CreateServer(collection);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[!all]");
            var vehicles = doc.GetData().GetProperty("Vehicles");

            Assert.Equal(2, vehicles.GetArrayLength());
            Assert.True(vehicles[0].HasProperty("NumberOfDoors"), "Derived Car properties should be emitted when the runtime type is Car");
            Assert.True(vehicles[1].HasProperty("PayloadCapacity"), "Derived Truck properties should be emitted when the runtime type is Truck");
        }

        [Fact(Skip = "Pending: abstract/interface base with [JsonDerivedType] discriminator support in the generator.")]
        public async Task PolymorphicCollection_EmitsDiscriminator_WhenConfigured()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: generator must honor [JsonDerivedType] + [JsonPolymorphic] attributes on the base type and emit a '$type' discriminator.");
        }
    }
}
