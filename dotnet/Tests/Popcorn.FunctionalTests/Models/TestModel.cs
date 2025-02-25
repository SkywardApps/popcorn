using System.Text.Json.Serialization;

namespace Popcorn.FunctionalTests.Models
{
    public class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
