using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class AlwaysAttributeTestModel
    {
        [Always]
        public int AlwaysIncludedId { get; set; }
        
        public string RegularProperty { get; set; } = string.Empty;
        
        [Default]
        public int DefaultProperty { get; set; }
    }
}
