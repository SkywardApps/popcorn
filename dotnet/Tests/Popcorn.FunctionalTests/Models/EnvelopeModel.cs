using System.Collections.Generic;
using System.Text.Json.Serialization;
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

    // Envelope declared as a record class. Covers: record-shape envelopes work like plain classes
    // through both the generator's AnalyzeEnvelope walker and the generated writer.
    [PopcornEnvelope]
    public record class RecordEnvelope<T>
    {
        [PopcornSuccess] public bool Ok { get; init; } = true;
        [PopcornPayload] public Pop<T> Cargo { get; init; }
        [PopcornError]   public ApiError? Issue { get; init; }
    }

    // Envelope whose marker properties use [JsonPropertyName] to rename the wire field.
    // Covers: the generator honors the JSON name override when emitting the error writer.
    [PopcornEnvelope]
    public class RenamedMarkerEnvelope<T>
    {
        [PopcornSuccess]
        [JsonPropertyName("success_flag")]
        public bool Ok { get; set; } = true;

        [PopcornPayload]
        [JsonPropertyName("payload")]
        public Pop<T> Payload { get; set; }

        [PopcornError]
        [JsonPropertyName("problem_details")]
        public ApiError? Problem { get; set; }
    }

    // Second custom envelope type. Used together with MyTestEnvelope<T> to verify the generator
    // emits dispatch for multiple envelopes in the same JsonSerializerContext.
    [PopcornEnvelope]
    public class AlternateEnvelope<T>
    {
        [PopcornSuccess] public bool State { get; set; } = true;
        [PopcornPayload] public Pop<T> Contents { get; set; }
        [PopcornError]   public ApiError? Boom { get; set; }
    }
}
