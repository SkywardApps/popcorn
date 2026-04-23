using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Popcorn.SourceGenerator.Tests;

// Asserts the generator produces warning-free output across the nullability matrix.
// These are compile-time diagnostics (CS8620 / CS8625 / CS8669) that fire against the
// generated .g.cs files under `<Nullable>enable</Nullable>`. They don't fail normal builds
// today, but a consumer with <TreatWarningsAsErrors>true</TreatWarningsAsErrors> would
// break on them — and they signal a real inconsistency in how the generator emits
// type arguments for Pop<T> at call sites vs. converter registrations.
public class NullabilityDiagnosticsTests
{
    private readonly ITestOutputHelper _output;

    public NullabilityDiagnosticsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private const string NullabilityMatrixSource = @"
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;
using Popcorn.Shared;

namespace NullabilityMatrix
{
    public struct Pt { public int X { get; set; } }

    public class Leaf
    {
        [Default] public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Root
    {
        // Scalars
        public int Int { get; set; }
        public int? QInt { get; set; }
        public string String { get; set; } = string.Empty;
        public string? QString { get; set; }
        public Pt Struct { get; set; }
        public Pt? QStruct { get; set; }
        public Leaf Class { get; set; } = new();
        public Leaf? QClass { get; set; }

        // Lists
        public List<int> ListInt { get; set; } = new();
        public List<int>? ListIntQ { get; set; }
        public List<int?> ListQInt { get; set; } = new();
        public List<int?>? ListQIntQ { get; set; }
        public List<string> ListString { get; set; } = new();
        public List<string>? ListStringQ { get; set; }
        public List<string?> ListQString { get; set; } = new();
        public List<string?>? ListQStringQ { get; set; }
        public List<Pt> ListStruct { get; set; } = new();
        public List<Pt?> ListQStruct { get; set; } = new();
        public List<Leaf> ListClass { get; set; } = new();
        public List<Leaf?> ListQClass { get; set; } = new();
        public List<Leaf>? ListClassQ { get; set; }
        public List<Leaf?>? ListQClassQ { get; set; }

        // Arrays
        public int[] ArrInt { get; set; } = Array.Empty<int>();
        public int[]? ArrIntQ { get; set; }
        public string?[] ArrQString { get; set; } = Array.Empty<string?>();
        public Leaf[]? ArrClassQ { get; set; }

        // Dictionaries
        public Dictionary<string, int> DictInt { get; set; } = new();
        public Dictionary<string, int>? DictIntQ { get; set; }
        public Dictionary<string, int?> DictQInt { get; set; } = new();
        public Dictionary<string, string?> DictQString { get; set; } = new();
        public Dictionary<string, Leaf> DictClass { get; set; } = new();
        public Dictionary<string, Leaf?> DictQClass { get; set; } = new();
        public Dictionary<string, Leaf>? DictClassQ { get; set; }

        // Nested combos
        public List<Dictionary<string, Leaf>>? ListOfDictQ { get; set; }
        public Dictionary<string, List<Leaf>?> DictOfQList { get; set; } = new();
    }

    [JsonSerializable(typeof(ApiResponse<Root>))]
    internal partial class Ctx : JsonSerializerContext { }
}
";

    [Fact]
    public void NullabilityMatrix_ProducesNoGeneratorErrors()
    {
        // JSG002 is a trace/debug "Show" warning the generator emits during its walk —
        // ignore it; it's noise, not a diagnostic about user code. Assert only on the
        // diagnostics that indicate a real generator-reported problem (JSG001 / JSG003+).
        var result = GeneratorTestHarness.RunAndCompile(NullabilityMatrixSource);
        var real = result.GeneratorDiagnostics.Where(d => d.Id != "JSG002").ToArray();

        foreach (var d in real)
        {
            _output.WriteLine(d.ToString());
        }

        Assert.Empty(real);
    }

    [Fact]
    public void NullabilityMatrix_ProducesNoCS8620_InGeneratedCode()
    {
        // CS8620 = "Argument of type 'Pop<T?>' cannot be used for parameter 'value' of type 'Pop<T>'
        //  ... due to differences in the nullability of reference types."
        // This is the core bug 3 surface.
        var result = GeneratorTestHarness.RunAndCompile(NullabilityMatrixSource);
        var violations = result.CompilationDiagnostics
            .Where(d => d.Id == "CS8620" && IsInGeneratedFile(d))
            .ToArray();

        foreach (var v in violations)
        {
            _output.WriteLine(v.ToString());
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void NullabilityMatrix_ProducesNoCS8669_InGeneratedCode()
    {
        // CS8669 = "The annotation for nullable reference types should only be used in code within
        //   a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable'
        //   directive in source."
        // Fires when a generated file uses `T?` without a `#nullable enable` directive at the top.
        var result = GeneratorTestHarness.RunAndCompile(NullabilityMatrixSource);
        var violations = result.CompilationDiagnostics
            .Where(d => d.Id == "CS8669" && IsInGeneratedFile(d))
            .ToArray();

        foreach (var v in violations)
        {
            _output.WriteLine(v.ToString());
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void NullabilityMatrix_ProducesNoOtherNullabilityWarnings_InGeneratedCode()
    {
        // Catch-all for the other CS86xx nullability family. If new ones surface as we work on
        // other features we'll see them here before they pile up.
        var result = GeneratorTestHarness.RunAndCompile(NullabilityMatrixSource);
        var violations = result.CompilationDiagnostics
            .Where(d => d.Id.StartsWith("CS86") && d.Id != "CS8620" && d.Id != "CS8669" && IsInGeneratedFile(d))
            .ToArray();

        foreach (var v in violations)
        {
            _output.WriteLine(v.ToString());
        }

        Assert.Empty(violations);
    }

    private static bool IsInGeneratedFile(Diagnostic d)
    {
        var path = d.Location.SourceTree?.FilePath ?? string.Empty;
        // Generator-emitted trees are added with hintName-derived paths ending in .g.cs. The driver
        // assigns paths like "Popcorn.SourceGenerator/Popcorn.SourceGenerator.ExpanderGenerator/<name>.g.cs".
        return path.EndsWith(".g.cs", StringComparison.Ordinal);
    }

    // ---------- Reference-type de-dup: MyClass and MyClass? registered together ----------
    //
    // NRT annotation on a reference type doesn't carry into the runtime Type — typeof(MyClass) and
    // typeof(MyClass?) are the same Type. The generator must walk the shared symbol once, emit one
    // converter, and route both registrations through it. This test locks that invariant so any
    // future change to how the walker canonicalizes symbols doesn't silently duplicate.
    private const string BothClassAndNullableClassSource = @"
#nullable enable
using System.Text.Json.Serialization;
using Popcorn;
using Popcorn.Shared;

namespace DedupRef
{
    public class Thing
    {
        [Default] public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [JsonSerializable(typeof(ApiResponse<Thing>))]
    [JsonSerializable(typeof(ApiResponse<Thing?>))]
    internal partial class Ctx : JsonSerializerContext { }
}
";

    [Fact]
    public void BothClassAndNullableClassRegistration_EmitsOneConverter_NotTwo()
    {
        // Invariant: registering both `ApiResponse<Thing>` and `ApiResponse<Thing?>` must produce
        // ONE Thing converter — the NRT `?` annotation doesn't change Type identity for a ref type.
        // Duplicate emission would collide on hint name (or at worst CS0579) at the consumer's build.
        //
        // We deliberately don't check compile diagnostics here — the synthetic harness doesn't wire
        // up the System.Text.Json JsonSerializerContext generator (that partial-class filler) nor
        // `using System.Linq`, so compiles are always noisy. Generated-tree count is the invariant.
        var result = GeneratorTestHarness.RunAndCompile(BothClassAndNullableClassSource);

        var thingConverterTrees = result.RunResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith("JsonConverter.g.cs", StringComparison.Ordinal))
            .Where(t => t.FilePath.Contains("Thing", StringComparison.Ordinal))
            .ToArray();
        foreach (var c in thingConverterTrees) _output.WriteLine(c.FilePath);

        Assert.Single(thingConverterTrees);

        // Content invariant: the registered method signature and the JsonConverter<Pop<T>> base
        // class must use the NRT-stripped form `Pop<...Thing>`, never `Pop<...Thing?>`. If a future
        // regression picks the annotated form, consumer builds re-introduce CS8620 at every
        // callsite that's already using the stripped form — and this test catches it before ship.
        var source = thingConverterTrees[0].GetText().ToString();
        Assert.Contains("Pop<DedupRef.Thing>", source);
        Assert.DoesNotContain("Pop<DedupRef.Thing?>", source);
    }

    [Fact]
    public void BothClassAndNullableClassRegistration_HasNoCS8620_InGenerated()
    {
        var result = GeneratorTestHarness.RunAndCompile(BothClassAndNullableClassSource);
        var violations = result.CompilationDiagnostics
            .Where(d => d.Id == "CS8620" && IsInGeneratedFile(d))
            .ToArray();
        foreach (var v in violations) _output.WriteLine(v.ToString());
        Assert.Empty(violations);
    }

    // ---------- Collection-element NRT de-dup: List<T> and List<T?> for T = ref type ----------
    //
    // Same principle: List<Thing> and List<Thing?> share a CLR Type. Shouldn't emit two list
    // converters. If they do, both will fight over the same hint name.
    private const string BothListRefAndListQRefSource = @"
#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;
using Popcorn.Shared;

namespace DedupL
{
    public class Th { [Default] public int Id { get; set; } }

    [JsonSerializable(typeof(ApiResponse<List<Th>>))]
    [JsonSerializable(typeof(ApiResponse<List<Th?>>))]
    internal partial class Ctx : JsonSerializerContext { }
}
";

    [Fact]
    public void BothListRefAndListNullableRef_EmitsOneListConverter()
    {
        var result = GeneratorTestHarness.RunAndCompile(BothListRefAndListQRefSource);

        // Exactly one List<Th> converter — NRT on element type doesn't split the registration.
        // File-name disambiguation: the List<Th> file starts with SystemCollectionsGenericList;
        // the standalone Th converter is just `DedupLThJsonConverter.g.cs` with no List prefix.
        var listConverterTrees = result.RunResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith("JsonConverter.g.cs", StringComparison.Ordinal))
            .Where(t => System.IO.Path.GetFileName(t.FilePath).StartsWith("SystemCollectionsGenericList", StringComparison.Ordinal))
            .Where(t => t.FilePath.Contains("DedupLTh", StringComparison.Ordinal))
            .ToArray();
        foreach (var c in listConverterTrees) _output.WriteLine(c.FilePath);

        Assert.Single(listConverterTrees);

        // Content invariant: the element type inside Pop<List<...>> must be `Th`, never `Th?`.
        var source = listConverterTrees[0].GetText().ToString();
        Assert.Contains("Pop<System.Collections.Generic.List<DedupL.Th>>", source);
        Assert.DoesNotContain("List<DedupL.Th?>", source);
    }

    // ---------- Value-type nullable MUST NOT be collapsed: List<int> vs List<int?> ----------
    //
    // List<int> and List<int?> ARE different CLR types (the latter is List<Nullable<int>>). The
    // generator must emit a separate converter for each and MUST NOT collapse them. This is the
    // contra-invariant to the two tests above — if the future fix for bug 3 over-normalizes,
    // this test catches the regression.
    private const string BothListIntAndListQIntSource = @"
#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Popcorn;
using Popcorn.Shared;

namespace DedupListVal
{
    public class Holder
    {
        public List<int> Plain { get; set; } = new();
        public List<int?> Nullable { get; set; } = new();
    }

    [JsonSerializable(typeof(ApiResponse<Holder>))]
    internal partial class Ctx : JsonSerializerContext { }
}
";

    [Fact]
    public void ListOfInt_AndListOfNullableInt_EmitSeparateConverters()
    {
        var result = GeneratorTestHarness.RunAndCompile(BothListIntAndListQIntSource);

        // Contra-invariant: Nullable<int> is a distinct CLR type. The generator MUST emit two
        // converters, not collapse. Guards against an over-zealous bug-3 normalization.
        var files = result.RunResult.GeneratedTrees.Select(t => t.FilePath).ToArray();
        var listIntFile = files.FirstOrDefault(p =>
            p.EndsWith("JsonConverter.g.cs", StringComparison.Ordinal)
            && p.Contains("ListSystemInt32", StringComparison.Ordinal)
            && !p.Contains("SystemNullable", StringComparison.Ordinal));
        var listNullableIntFile = files.FirstOrDefault(p =>
            p.EndsWith("JsonConverter.g.cs", StringComparison.Ordinal)
            && p.Contains("ListSystemNullableSystemInt32", StringComparison.Ordinal));

        foreach (var f in files.Where(p => p.Contains("List", StringComparison.Ordinal))) _output.WriteLine(f);

        Assert.NotNull(listIntFile);
        Assert.NotNull(listNullableIntFile);
        Assert.NotEqual(listIntFile, listNullableIntFile);
    }
}
