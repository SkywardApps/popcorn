using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class ConflictingAttributesTestModel
    {
        [Always]
        [Never]
        public int AlwaysNeverProperty { get; set; }
        
        [Always]
        [Default]
        public int AlwaysDefaultProperty { get; set; }
    }
}
