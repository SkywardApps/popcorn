using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class JsonPropertyNameModel
    {
        [Default]
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public System.DateTimeOffset CreatedAt { get; set; }

        [Always]
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        public string InternalName { get; set; } = string.Empty;
    }
}
