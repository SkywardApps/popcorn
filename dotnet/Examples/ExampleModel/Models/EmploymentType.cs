using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PopcornNetFrameworkExample.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EmploymentType
    {
        Employed,
        PartTime,
        FullTime
    }
}