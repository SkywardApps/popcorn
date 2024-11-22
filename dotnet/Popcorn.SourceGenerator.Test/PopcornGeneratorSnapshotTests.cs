namespace Popcorn.SourceGenerator.Test
{
    public class PopcornGeneratorSnapshotTests
    {
        [Fact]
        public Task BasicGenerate()
        {
            // Basic record that should get a converter
            var source = @"
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
#nullable enable
namespace Test1
{
    public record Todo(int Id, string? Title, DateTimeOffset? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
        public AppJsonSerializerContext() : base(null) {}

        protected override JsonSerializerOptions? GeneratedSerializerOptions => throw new NotImplementedException();

        public override System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(Type type)
        {
            throw new NotImplementedException();
        }
    }
}";

            return TestHelper.Verify(source);
        }


        [Fact]
        public Task JsonPropertyNameOverride()
        {
            // Apply a property name to the DueBy property
            var source = @"
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
#nullable enable
namespace Test1
{
    public record Todo(int Id, string? Title, [property: JsonPropertyName(""DueDate"")] DateTimeOffset? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
        public AppJsonSerializerContext() : base(null) {}

        protected override JsonSerializerOptions? GeneratedSerializerOptions => throw new NotImplementedException();

        public override System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(Type type)
        {
            throw new NotImplementedException();
        }
    }
}";

            return TestHelper.Verify(source);
        }


        [Fact]
        public Task AlwaysAttributeApplies()
        {
            // Add an 'Always' attribute to the Id so it is always included in serialization
            var source = @"
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Popcorn;

#nullable enable
namespace Test1
{
    public record Todo([property: Always] int Id, string? Title, DateTimeOffset? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
        public AppJsonSerializerContext() : base(null) {}

        protected override JsonSerializerOptions? GeneratedSerializerOptions => throw new NotImplementedException();

        public override System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(Type type)
        {
            throw new NotImplementedException();
        }
    }
}";

            // Pass the source code to our helper and snapshot test the output
            return TestHelper.Verify(source);
        }


        [Fact]
        public Task NeverAttributeApplies()
        {
            // Add a 'Never' attribute to the Title so it is never serialized
            var source = @"
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Popcorn;

#nullable enable
namespace Test1
{
    public record Todo(int Id, [property: Never] string? Title, DateTimeOffset? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
        public AppJsonSerializerContext() : base(null) {}

        protected override JsonSerializerOptions? GeneratedSerializerOptions => throw new NotImplementedException();

        public override System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(Type type)
        {
            throw new NotImplementedException();
        }
    }
}";

            // Pass the source code to our helper and snapshot test the output
            return TestHelper.Verify(source);
        }
    }
}