using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PopcornCoreExample.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EmploymentType
    {
        Employed,
        PartTime,
        FullTime
    }
}