using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // Acceptance tests for Tier-1 [SubPropertyDefault]: when a property/member is included
    // without explicit sub-children, the attribute's include list is used as the default
    // instead of PropertyReference.Default. Explicit sub-children override the attribute.
    public class SubPropertyDefaultTests
    {
        [Fact]
        public async Task SubPropertyDefault_AppliesWhenParentIncludedWithoutSubIncludes()
        {
            var model = new OwnerWithVehicles
            {
                OwnerName = "Pat",
                Vehicles =
                {
                    new VehicleWithDefault { Make = "Toyota", Model = "Corolla", Year = "2020" },
                    new VehicleWithDefault { Make = "Ford", Model = "F-150", Year = "2022" },
                }
            };

            using var server = TestServerHelper.CreateServer(model);
            using var client = server.CreateClient();
            using var doc = await TestServerHelper.GetJsonAsync(client, "/test?include=[Vehicles]");

            var vehicles = doc.GetData().GetProperty("Vehicles");
            Assert.Equal(2, vehicles.GetArrayLength());
            foreach (var vehicle in vehicles.EnumerateArray())
            {
                Assert.True(vehicle.HasProperty("Make"));
                Assert.True(vehicle.HasProperty("Model"));
                Assert.False(vehicle.HasProperty("Year"), "Year must NOT be emitted — it isn't in the SubPropertyDefault list.");
            }
        }

        [Fact]
        public async Task SubPropertyDefault_OverriddenByExplicitSubIncludes()
        {
            var model = new OwnerWithVehicles
            {
                OwnerName = "Pat",
                Vehicles =
                {
                    new VehicleWithDefault { Make = "Toyota", Model = "Corolla", Year = "2020" },
                }
            };

            using var server = TestServerHelper.CreateServer(model);
            using var client = server.CreateClient();
            using var doc = await TestServerHelper.GetJsonAsync(client, "/test?include=[Vehicles[Year]]");

            var vehicles = doc.GetData().GetProperty("Vehicles");
            var first = vehicles[0];
            Assert.True(first.HasProperty("Year"));
            Assert.False(first.HasProperty("Make"), "Make must NOT be emitted — explicit [Year] override wins over SubPropertyDefault.");
            Assert.False(first.HasProperty("Model"), "Model must NOT be emitted — explicit [Year] override wins over SubPropertyDefault.");
        }

        [Fact]
        public async Task SubPropertyDefault_AppliesRecursivelyThroughNestedTypes()
        {
            var model = new RecursiveSubDefaultRoot
            {
                Profile = new RecursiveProfile
                {
                    Name = "Pat",
                    Bio = "should be dropped",
                    PrimaryVehicle = new VehicleWithDefault { Make = "Toyota", Model = "Corolla", Year = "2020" },
                },
            };

            using var server = TestServerHelper.CreateServer(model);
            using var client = server.CreateClient();
            using var doc = await TestServerHelper.GetJsonAsync(client, "/test?include=[Profile]");

            var profile = doc.GetData().GetProperty("Profile");
            Assert.True(profile.HasProperty("Name"), "Outer SubPropertyDefault includes Name.");
            Assert.True(profile.HasProperty("PrimaryVehicle"), "Outer SubPropertyDefault includes PrimaryVehicle.");
            Assert.False(profile.HasProperty("Bio"), "Outer SubPropertyDefault excludes Bio.");

            var primary = profile.GetProperty("PrimaryVehicle");
            Assert.True(primary.HasProperty("Make"), "Inner SubPropertyDefault includes Make.");
            Assert.False(primary.HasProperty("Model"), "Inner SubPropertyDefault excludes Model.");
            Assert.False(primary.HasProperty("Year"), "Inner SubPropertyDefault excludes Year.");
        }

        [Fact]
        public async Task SubPropertyDefault_InteractsWithOtherAttributes()
        {
            var model = new OwnerWithMarkedVehicle
            {
                Vehicle = new VehicleWithMarkers
                {
                    Make = "Toyota",
                    Vin = "VIN-123",
                    Secret = "shh",
                    Model = "Corolla",
                },
            };

            using var server = TestServerHelper.CreateServer(model);
            using var client = server.CreateClient();

            // Case 1: SubPropertyDefault fallback. Make (from attr) and Vin (from [Always]) emit;
            //         Secret ([Never]) does not; Model isn't in the attr list so doesn't emit.
            using (var doc = await TestServerHelper.GetJsonAsync(client, "/test?include=[Vehicle]"))
            {
                var vehicle = doc.GetData().GetProperty("Vehicle");
                Assert.True(vehicle.HasProperty("Make"));
                Assert.True(vehicle.HasProperty("Vin"), "[Always] members must emit regardless of SubPropertyDefault.");
                Assert.False(vehicle.HasProperty("Secret"), "[Never] members must NOT emit even under SubPropertyDefault.");
                Assert.False(vehicle.HasProperty("Model"));
            }

            // Case 2: explicit override [Model]. Model and Vin emit; Make doesn't; Secret still doesn't.
            using (var doc = await TestServerHelper.GetJsonAsync(client, "/test?include=[Vehicle[Model]]"))
            {
                var vehicle = doc.GetData().GetProperty("Vehicle");
                Assert.True(vehicle.HasProperty("Model"));
                Assert.True(vehicle.HasProperty("Vin"), "[Always] still wins under explicit override.");
                Assert.False(vehicle.HasProperty("Make"));
                Assert.False(vehicle.HasProperty("Secret"), "[Never] still wins under explicit override.");
            }
        }
    }
}
