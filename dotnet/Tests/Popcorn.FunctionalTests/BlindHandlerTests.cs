using System.Text.Json;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-2 blind handlers (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - IPopcornBlindHandler<TFrom, TTo> { TTo Convert(TFrom); }
    //   - services.AddPopcornBlindHandler<TFrom, TTo>(...);
    //   - When the generator walks a property of type TFrom and a handler is registered,
    //     emits `writer.WriteValue(handler.Convert(value))` instead of the default converter.
    //   - Primary use case: externally-defined types the consumer can't annotate (e.g. NTS Geometry → WKT string).
    public class BlindHandlerTests
    {
        [Fact(Skip = "Pending: IPopcornBlindHandler<TFrom, TTo> not yet implemented.")]
        public async Task BlindHandler_ConvertsExternalTypeToSimplerForm()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: a registered handler for a type like NTS Geometry should receive the source value and return a string/primitive to emit in its place.");
        }

        [Fact(Skip = "Pending: handler receives DI services.")]
        public async Task BlindHandler_ReceivesDIServices()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: handler constructor can take DI services; generator resolves the handler via IServiceProvider at converter init.");
        }

        [Fact(Skip = "Pending: multiple handler registrations for different (TFrom, TTo) pairs coexist.")]
        public async Task BlindHandler_MultipleRegistrations_ResolvedByType()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: AddPopcornBlindHandler<Geometry,string>() and AddPopcornBlindHandler<Money,decimal>() must both be resolved correctly when both types appear in a single model.");
        }

        [Fact(Skip = "Pending: no handler → fall through to default System.Text.Json converter.")]
        public async Task BlindHandler_NotRegistered_FallsBackToDefault()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: if no IPopcornBlindHandler is registered for a type, the default System.Text.Json converter (or user-registered JsonConverter) is used.");
        }
    }
}
