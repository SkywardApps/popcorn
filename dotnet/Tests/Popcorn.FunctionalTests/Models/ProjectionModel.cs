using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class CarSource
    {
        public int Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string InternalNotes { get; set; } = string.Empty;
    }

    public class CarProjection
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string Make { get; set; } = string.Empty;

        [Default]
        public string Model { get; set; } = string.Empty;
    }
}
