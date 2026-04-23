using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Popcorn.SourceGenerator;

namespace Popcorn.SourceGenerator.Tests;

internal static class GeneratorTestHarness
{
    public sealed record Result(ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult RunResult);

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
}
