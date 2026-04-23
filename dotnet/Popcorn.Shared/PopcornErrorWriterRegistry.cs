#nullable enable
using System;
using System.Text.Json;
using System.Threading;

namespace Popcorn.Shared
{
    /// <summary>
    /// Process-global registry for the generator-emitted error-envelope writer.
    /// The source generator calls <see cref="Register"/> from within <c>AddPopcornOptions()</c> and
    /// <c>AddPopcornEnvelopes()</c>; the exception middleware calls <see cref="TryWrite"/> on the error path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registry holds a single writer delegate because it is derived from compile-time knowledge of
    /// every <c>[PopcornEnvelope]</c> type reachable from the user's <c>JsonSerializerContext</c>. In a
    /// single-assembly application the generator always emits the same delegate, so repeated registration
    /// is idempotent. In process-sharing scenarios (two Popcorn-using applications hosted in the same
    /// process), the last registration wins — treat this as a single-app-per-process abstraction.
    /// </para>
    /// <para>
    /// Writes are expected only at startup, but <see cref="Volatile"/> is used to make the happens-before
    /// relationship explicit in case registration and first read race across threads.
    /// </para>
    /// </remarks>
    public static class PopcornErrorWriterRegistry
    {
        private static Action<Utf8JsonWriter, Type, ApiError, JsonNamingPolicy?>? _writer;

        public static void Register(Action<Utf8JsonWriter, Type, ApiError, JsonNamingPolicy?> writer)
        {
            Volatile.Write(ref _writer, writer);
        }

        public static bool TryWrite(Utf8JsonWriter writer, Type envelopeType, ApiError error, JsonNamingPolicy? namingPolicy)
        {
            var current = Volatile.Read(ref _writer);
            if (current == null) return false;
            current(writer, envelopeType, error, namingPolicy);
            return true;
        }
    }
}
