using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class NestedAlwaysAttributeTestModel
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public NestedModel NestedObject { get; set; } = new NestedModel();
    }

    public class NestedModel
    {
        [Always]
        public int AlwaysIncludedId { get; set; }
        
        public string RegularProperty { get; set; } = string.Empty;
    }
}
