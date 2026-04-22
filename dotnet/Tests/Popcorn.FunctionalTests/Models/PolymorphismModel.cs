using System.Collections.Generic;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class Vehicle
    {
        [Always]
        public int Id { get; set; }

        [Default]
        public string Make { get; set; } = string.Empty;

        [Default]
        public string Model { get; set; } = string.Empty;

        public int Year { get; set; }
    }

    public class Car : Vehicle
    {
        public int NumberOfDoors { get; set; }

        public string BodyStyle { get; set; } = string.Empty;
    }

    public class Truck : Vehicle
    {
        public int PayloadCapacity { get; set; }

        public bool HasTrailer { get; set; }
    }

    public class VehicleCollection
    {
        [Always]
        public string Owner { get; set; } = string.Empty;

        [Default]
        public List<Vehicle> Vehicles { get; set; } = new();
    }
}
