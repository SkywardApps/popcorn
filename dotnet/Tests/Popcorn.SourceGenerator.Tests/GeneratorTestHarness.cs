using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Popcorn.SourceGenerator;

namespace Popcorn.SourceGenerator.Tests;

internal static class GeneratorTestHarness
{
    public sealed record Result(ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult RunResult);

    // Same as Result but also carries the post-generator compilation and its diagnostics,
    // so tests can assert on CS86xx (nullability) warnings emitted against generated files.
    public sealed record CompileResult(
        ImmutableArray<Diagnostic> GeneratorDiagnostics,
        ImmutableArray<Diagnostic> CompilationDiagnostics,
        GeneratorDriverRunResult RunResult,
        Compilation OutputCompilation);

    public static Result Run(string source)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonSerializableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Popcorn.Shared.ApiResponse<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Popcorn.PopcornEnvelopeAttribute).Assembly.Location),
        };

        // netstandard + System.Runtime references required for the synthetic compilation to resolve core types.
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        foreach (var name in new[] { "netstandard", "System.Runtime", "System.Collections", "System.Linq", "Microsoft.Extensions.DependencyInjection.Abstractions" })
        {
            var match = trustedAssembliesPaths.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                references.Add(MetadataReference.CreateFromFile(match));
            }
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "PopcornGeneratorTest",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var generator = new ExpanderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        var runResult = driver.GetRunResult();

        return new Result(diagnostics, runResult);
    }

    // Run the generator and also materialize compile diagnostics against the post-generator
    // compilation. Useful for asserting on CS86xx (nullability) warnings that ride on generated
    // code rather than on the generator pipeline itself.
    public static CompileResult RunAndCompile(string source)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonSerializableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Popcorn.Shared.ApiResponse<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Popcorn.PopcornEnvelopeAttribute).Assembly.Location),
        };

        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        foreach (var name in new[] { "netstandard", "System.Runtime", "System.Collections", "System.Linq", "Microsoft.Extensions.DependencyInjection.Abstractions" })
        {
            var match = trustedAssembliesPaths.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                references.Add(MetadataReference.CreateFromFile(match));
            }
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "PopcornGeneratorTest",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var generator = new ExpanderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        // The post-generator compilation contains the generated syntax trees. Its Diagnostics
        // list includes anything the C# compiler would report on that combined source.
        return new CompileResult(
            generatorDiagnostics,
            outputCompilation.GetDiagnostics(),
            driver.GetRunResult(),
            outputCompilation);
    }
}
