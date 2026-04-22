using System.Text.Json;
using Popcorn.FunctionalTests.Models;
using Xunit;

namespace Popcorn.FunctionalTests
{
    // Translator story — two modes (see memory-bank/apiDesign.md):
    //   Mode A: plain C# computed property — already works, no framework required.
    //   Mode B: [Translator(nameof(Prop))] static method taking the source + DI params — pending.
    public class TranslatorTests
    {
        [Fact]
        public async Task ComputedProperty_EmittedAsNormalProperty()
        {
            var model = new PersonWithComputed { FirstName = "Liz", LastName = "Lemon" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FullName]");
            var fullName = doc.GetData().GetProperty("FullName");

            Assert.Equal("Liz Lemon", fullName.GetString());
        }

        [Fact]
        public async Task ComputedProperty_HonorsDefaultAttribute()
        {
            var model = new PersonWithComputed { FirstName = "Liz", LastName = "Lemon" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("FullName"));
            Assert.Equal("Liz Lemon", data.GetProperty("FullName").GetString());
        }

        [Fact]
        public async Task ComputedProperty_CanBeExcluded()
        {
            var model = new PersonWithComputed { FirstName = "Liz", LastName = "Lemon" };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[FirstName]");
            var data = doc.GetData();

            Assert.True(data.HasProperty("FirstName"));
            Assert.False(data.HasProperty("FullName"));
        }

        [Fact(Skip = "Pending: [Translator] attribute not yet implemented. See apiDesign.md Mode B — static method with DI-injected parameters.")]
        public async Task TranslatorAttribute_StaticMethodWithDI_EmitsComputedValue()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: a [Translator(nameof(Prop))] static method whose signature is (SourceType, IService) should be invoked by the generator-emitted converter with the service resolved via IServiceProvider.");
        }

        [Fact(Skip = "Pending: [Translator] — void or invalid return type produces generator diagnostic.")]
        public async Task TranslatorAttribute_InvalidSignature_ProducesDiagnostic()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: generator should emit a JSG diagnostic if the [Translator] method's return type doesn't match the target property type.");
        }

        [Fact(Skip = "Pending: partial-method translator variant — user implements in a partial class file.")]
        public async Task PartialMethodTranslator_EmittedFromUserImplementation()
        {
            await Task.CompletedTask;
            Assert.Fail("Pending: partial method declared on the model, user-provided implementation in a separate partial file, generator wires the call at emit time.");
        }
    }
}
