using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PopcornNetCoreExample.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EmploymentType
    {
        Employed,
        PartTime,
        FullTime
    }
}