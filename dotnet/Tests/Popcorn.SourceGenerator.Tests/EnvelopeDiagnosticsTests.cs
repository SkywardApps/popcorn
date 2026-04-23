using Microsoft.CodeAnalysis;
using Xunit;

namespace Popcorn.SourceGenerator.Tests;

public class EnvelopeDiagnosticsTests
{
    private const string Prelude = @"
using System.Text.Json.Serialization;
using Popcorn;
using Popcorn.Shared;

public class Payload { public int Id { get; set; } }
";

    // JSG003: [PopcornEnvelope] type without [PopcornPayload] emits a warning.
    [Fact]
    public void JSG003_WhenEnvelopeHasNoPopcornPayload()
    {
        var source = Prelude + @"
[PopcornEnvelope]
public class BadEnvelope<T>
{
    [PopcornSuccess] public bool Ok { get; set; }
    [PopcornError] public ApiError? Err { get; set; }
}

[JsonSerializable(typeof(BadEnvelope<Payload>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG003");
    }

    // JSG004: multiple properties with the same marker.
    [Fact]
    public void JSG004_WhenMultiplePropertiesShareMarker()
    {
        var source = Prelude + @"
[PopcornEnvelope]
public class DupEnvelope<T>
{
    [PopcornSuccess] public bool First { get; set; }
    [PopcornSuccess] public bool Second { get; set; }
    [PopcornPayload] public Pop<T> Payload { get; set; }
}

[JsonSerializable(typeof(DupEnvelope<Payload>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG004");
    }

    // JSG005: [PopcornPayload] property is not Pop<T>.
    [Fact]
    public void JSG005_WhenPayloadIsNotPopOfT()
    {
        var source = Prelude + @"
[PopcornEnvelope]
public class WrongPayloadTypeEnvelope<T>
{
    [PopcornSuccess] public bool Ok { get; set; }
    [PopcornPayload] public T? Payload { get; set; }
}

[JsonSerializable(typeof(WrongPayloadTypeEnvelope<Payload>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG005");
    }

    // JSG006: [PopcornError] property is not ApiError / ApiError?.
    [Fact]
    public void JSG006_WhenErrorSlotIsNotApiError()
    {
        var source = Prelude + @"
[PopcornEnvelope]
public class WrongErrorTypeEnvelope<T>
{
    [PopcornPayload] public Pop<T> Payload { get; set; }
    [PopcornError] public string? Err { get; set; }
}

[JsonSerializable(typeof(WrongErrorTypeEnvelope<Payload>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG006");
    }

    // JSG007: envelope nested inside a generic outer type cannot produce a valid typeof(...) expression.
    [Fact]
    public void JSG007_WhenEnvelopeIsInsideGenericOuter()
    {
        var source = Prelude + @"
public class GenericOuter<X>
{
    [PopcornEnvelope]
    public class NestedEnvelope<T>
    {
        [PopcornSuccess] public bool Ok { get; set; }
        [PopcornPayload] public Pop<T> Payload { get; set; }
    }
}

[JsonSerializable(typeof(GenericOuter<Payload>.NestedEnvelope<Payload>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG007");
    }

    // Positive case: a well-formed envelope produces no envelope-related diagnostics.
    [Fact]
    public void Valid_Envelope_ProducesNoEnvelopeDiagnostics()
    {
        var source = Prelude + @"
[PopcornEnvelope]
public class GoodEnvelope<T>
{
    [PopcornSuccess] public bool Ok { get; set; }
    [PopcornPayload] public Pop<T> Payload { get; set; }
    [PopcornError] public ApiError? Err { get; set; }
}

[JsonSerializable(typeof(GoodEnvelope<Payload>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        var envelopeIds = new[] { "JSG003", "JSG004", "JSG005", "JSG006", "JSG007" };
        Assert.DoesNotContain(result.Diagnostics, d => envelopeIds.Contains(d.Id));
    }

    // JSG008: property declared as System.Object is unresolvable under AOT.
    [Fact]
    public void JSG008_WhenPropertyIsObject()
    {
        var source = @"
using System.Text.Json.Serialization;
using Popcorn.Shared;

public class Bag { public object? Tag { get; set; } public int Id { get; set; } }

[JsonSerializable(typeof(ApiResponse<Bag>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG008");
    }

    // JSG008: interface-typed property (e.g. IAnimal) is unresolvable under AOT.
    [Fact]
    public void JSG008_WhenPropertyIsInterface()
    {
        var source = @"
using System.Text.Json.Serialization;
using Popcorn.Shared;

public interface IAnimal { string Name { get; } }
public class Zoo { public IAnimal? Headliner { get; set; } public int Id { get; set; } }

[JsonSerializable(typeof(ApiResponse<Zoo>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG008");
    }

    // JSG008: abstract-class-typed property is unresolvable under AOT.
    [Fact]
    public void JSG008_WhenPropertyIsAbstractClass()
    {
        var source = @"
using System.Text.Json.Serialization;
using Popcorn.Shared;

public abstract class Shape { public abstract double Area { get; } }
public class Drawing { public Shape? Primary { get; set; } public int Id { get; set; } }

[JsonSerializable(typeof(ApiResponse<Drawing>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG008");
    }

    // JSG008: collection-of-object unwraps through IEnumerable<T> to System.Object → still flagged.
    [Fact]
    public void JSG008_WhenPropertyIsListOfObject()
    {
        var source = @"
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn.Shared;

public class BagList { public List<object>? Items { get; set; } public int Id { get; set; } }

[JsonSerializable(typeof(ApiResponse<BagList>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "JSG008");
    }

    // JSG008 negative: a concrete class-typed property does NOT trigger the diagnostic.
    [Fact]
    public void JSG008_NotEmittedForConcreteType()
    {
        var source = @"
using System.Text.Json.Serialization;
using Popcorn.Shared;

public class Child { public int N { get; set; } }
public class Parent { public Child? Kid { get; set; } public int Id { get; set; } }

[JsonSerializable(typeof(ApiResponse<Parent>))]
public partial class Ctx : JsonSerializerContext { }
";
        var result = GeneratorTestHarness.Run(source);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "JSG008");
    }
}
