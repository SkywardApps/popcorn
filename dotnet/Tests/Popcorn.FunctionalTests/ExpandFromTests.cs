using System.Text.Json;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-2 [ExpandFrom] projections (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - [ExpandFrom(typeof(CarSource))] public class CarProjection { ... }
    //   - Generator emits a CarProjection.From(CarSource) copy method that maps matching properties.
    //   - Use case: hide internal properties (e.g. CarSource.InternalNotes) from the API surface.
    //   - Most users will NOT need this (serialize the source type directly with [Never] on internal props
    //     is simpler), but parity with the legacy Map<Source,Projection> concept is useful.
    public class ExpandFromTests
    {
        [Fact(Skip = "Pending: [ExpandFrom] attribute not yet implemented.")]
        public async Task ExpandFrom_CopiesMatchingPropertiesFromSource()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: when an endpoint returns a CarSource but registers CarProjection as the response type, the generator emits copy logic that populates CarProjection from CarSource by matching property names.");
        }

        [Fact(Skip = "Pending: [ExpandFrom] excludes properties not present on the projection.")]
        public async Task ExpandFrom_ExcludesPropertiesNotOnProjection()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: CarSource.InternalNotes should never appear in a CarProjection response because the projection doesn't declare it.");
        }

        [Fact(Skip = "Pending: [ExpandFrom] supports inheritance chains.")]
        public async Task ExpandFrom_HandlesInheritedSourceProperties()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: ExpandFrom should work when the source has inherited properties.");
        }

        [Fact(Skip = "Pending: [ExpandFrom] with default includes on the projection.")]
        public async Task ExpandFrom_RespectsDefaultIncludesOnProjection()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: [Default]/[Always] attributes on the projection class must still apply.");
        }
    }
}
