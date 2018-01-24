using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ExampleModel.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EmploymentType
    {
        Employed,
        PartTime,
        FullTime
    }
}