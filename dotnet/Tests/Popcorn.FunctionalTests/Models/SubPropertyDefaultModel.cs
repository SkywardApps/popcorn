using System.Collections.Generic;

namespace Popcorn.FunctionalTests.Models
{
    // Test 1/2 — SubPropertyDefault on a collection member.
    public class OwnerWithVehicles
    {
        public string? OwnerName { get; set; }

        [SubPropertyDefault("[Make,Model]")]
        public List<VehicleWithDefault> Vehicles { get; set; } = new();
    }

    public class VehicleWithDefault
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? Year { get; set; }
    }

    // Test 3 — recursive SubPropertyDefault tree.
    public class RecursiveSubDefaultRoot
    {
        [SubPropertyDefault("[Name,PrimaryVehicle]")]
        public RecursiveProfile? Profile { get; set; }
    }

    public class RecursiveProfile
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }

        [SubPropertyDefault("[Make]")]
        public VehicleWithDefault? PrimaryVehicle { get; set; }
    }

    // Test 4 — interaction with [Always] / [Never].
    public class OwnerWithMarkedVehicle
    {
        [SubPropertyDefault("[Make]")]
        public VehicleWithMarkers? Vehicle { get; set; }
    }

    public class VehicleWithMarkers
    {
        public string? Make { get; set; }

        [Always]
        public string? Vin { get; set; }

        [Never]
        public string? Secret { get; set; }

        public string? Model { get; set; }
    }
}
