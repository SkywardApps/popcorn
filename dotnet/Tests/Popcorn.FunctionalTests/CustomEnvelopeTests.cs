using System.Text.Json;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 custom response envelope (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - Default envelope: ApiResponse<T> { Success, Data: Pop<T>, Error?, Page? }
    //   - Custom envelope via [PopcornEnvelope] on a user type + services.AddPopcorn(o => o.EnvelopeType = typeof(...))
    //   - Exception → Envelope.Success=false + Error populated, via UsePopcornExceptionHandler() middleware.
    //   - One envelope per application (multi-envelope out of scope).
    public class CustomEnvelopeTests
    {
        [Fact(Skip = "Pending: ApiError type + structured error envelope.")]
        public async Task DefaultEnvelope_Error_EmittedWhenExceptionThrown()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: UsePopcornExceptionHandler middleware should rewrite unhandled exceptions as ApiResponse { Success=false, Error=ApiError(...) }.");
        }

        [Fact(Skip = "Pending: PageInfo envelope metadata.")]
        public async Task DefaultEnvelope_Page_EmittedWhenPaginationActive()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: when pagination middleware applies Skip/Take, Page info should be set on the envelope.");
        }

        [Fact(Skip = "Pending: [PopcornEnvelope] attribute on custom user type.")]
        public async Task CustomEnvelope_ReplacesDefaultShape()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: a type decorated with [PopcornEnvelope] and registered via AddPopcorn(o => o.EnvelopeType=typeof(MyEnvelope<>)) should replace the ApiResponse<T> shape.");
        }

        [Fact(Skip = "Pending: custom envelope receives the payload, applies user-defined wrapping.")]
        public async Task CustomEnvelope_WrapsPayloadWithUserFields()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: MyEnvelope<T> { Ok, Payload, Messages } should wrap the data and be serialized by the generator-emitted converter chain.");
        }

        [Fact(Skip = "Pending: exception middleware respects custom envelope shape.")]
        public async Task CustomEnvelope_ExceptionHandler_UsesCustomShape()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: UsePopcornExceptionHandler must detect the configured envelope type and populate its error fields (user-defined names).");
        }
    }
}
