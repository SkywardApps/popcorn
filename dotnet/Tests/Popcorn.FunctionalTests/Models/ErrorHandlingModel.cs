using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class ErrorHandlingModel
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string Name { get; set; } = string.Empty;

        public NestedReference? Nested { get; set; }
    }

    public class NestedReference
    {
        [Default]
        public string Label { get; set; } = string.Empty;
    }
}
