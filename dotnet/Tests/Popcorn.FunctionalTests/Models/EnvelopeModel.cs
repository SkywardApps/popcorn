using Popcorn;

namespace Popcorn.FunctionalTests.Models
{
    public class EnvelopePayload
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string Name { get; set; } = string.Empty;
    }
}
