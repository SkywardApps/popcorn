using System.Text.Json;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // TDD ledger for Tier-1 SubPropertyDefault (see memory-bank/apiDesign.md).
    // v2 plan:
    //   - [SubPropertyDefault("[Make,Model]")] on a property declares the default include list
    //     to use when that property is expanded without explicit sub-includes.
    //   - Replaces legacy [SubPropertyIncludeByDefault].
    public class SubPropertyDefaultTests
    {
        [Fact(Skip = "Pending: [SubPropertyDefault] attribute not yet implemented. See apiDesign.md.")]
        public async Task SubPropertyDefault_AppliesWhenParentIncludedWithoutSubIncludes()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: given a property [SubPropertyDefault(\"[Make,Model]\")] public List<Car> Vehicles, a request ?include=[Vehicles] should emit each Car with only Make and Model.");
        }

        [Fact(Skip = "Pending: explicit sub-includes override SubPropertyDefault.")]
        public async Task SubPropertyDefault_OverriddenByExplicitSubIncludes()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: ?include=[Vehicles[Year]] should emit only Year, NOT the SubPropertyDefault list.");
        }

        [Fact(Skip = "Pending: SubPropertyDefault applies recursively through nested types.")]
        public async Task SubPropertyDefault_AppliesRecursivelyThroughNestedTypes()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: a tree of [SubPropertyDefault] attributes must resolve at each level.");
        }

        [Fact(Skip = "Pending: SubPropertyDefault interacts correctly with [Always] / [Default] / [Never] on the target type.")]
        public async Task SubPropertyDefault_InteractsWithOtherAttributes()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: [Always] properties on the sub-type still emit; [Never] properties still don't; SubPropertyDefault only affects what is default-included.");
        }
    }
}
