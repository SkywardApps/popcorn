using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class IncludeParameterTestModel
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string UPPERCASEPROP { get; set; } = string.Empty;
        
        public string camelCaseProp { get; set; } = string.Empty;
        
        [Default]
        public int DefaultProperty1 { get; set; }
        
        [Default]
        public string DefaultProperty2 { get; set; } = string.Empty;
        
        [Always]
        public int AlwaysProperty { get; set; }
    }
}
