using System.Text.Json.Serialization;
using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    // Per-type opt-in: attribute on the enum declaration makes every use of it serialize as a string,
    // regardless of global options. Standard System.Text.Json pattern; the generic converter is the
    // AOT-compatible form (SYSLIB1034).
    [JsonConverter(typeof(JsonStringEnumConverter<Season>))]
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    public class StringEnumTestModel
    {
        [Default]
        public Season CurrentSeason { get; set; }

        [Default]
        public Color FavoriteColor { get; set; }
    }
}
