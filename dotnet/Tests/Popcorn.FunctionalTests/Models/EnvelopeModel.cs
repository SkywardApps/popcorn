using System.Collections.Generic;
using Popcorn;
using Popcorn.Shared;

namespace Popcorn.FunctionalTests.Models
{
    public class EnvelopePayload
    {
        [Default]
        public int Id { get; set; }

        [Default]
        public string Name { get; set; } = string.Empty;
    }

    [PopcornEnvelope]
    public class MyTestEnvelope<T>
    {
        [PopcornSuccess]
        public bool Ok { get; set; } = true;

        [PopcornPayload]
        public Pop<T> Payload { get; set; }

        [PopcornError]
        public ApiError? Problem { get; set; }

        public List<string> Messages { get; set; } = new();
    }

    // Base envelope declaring Success/Error markers. The derived envelope class below adds [PopcornPayload].
    // Covers: envelope marker inheritance — generator must walk the base chain.
    public abstract class BaseEnvelope<T>
    {
        [PopcornSuccess]
        public bool Okay { get; set; } = true;

        [PopcornError]
        public ApiError? Mishap { get; set; }
    }

    [PopcornEnvelope]
    public class DerivedEnvelope<T> : BaseEnvelope<T>
    {
        [PopcornPayload]
        public Pop<T> Body { get; set; }
    }

    // Envelope nested inside a non-generic outer type. Covers: OpenGenericCSharpName handles nested types.
    public static class NestedEnvelopeContainer
    {
        [PopcornEnvelope]
        public class NestedEnvelope<T>
        {
            [PopcornSuccess]
            public bool Ok { get; set; } = true;

            [PopcornPayload]
            public Pop<T> Payload { get; set; }

            [PopcornError]
            public ApiError? Fault { get; set; }
        }
    }
}
