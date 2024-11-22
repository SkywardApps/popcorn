namespace Popcorn.SourceGenerator.Test
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using VerifyXunit;

    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Initialize();
        }
    }

    public static class TestHelper
    {
        private const string pathToNugetDLL = @"";


        public static Task Verify(string source)
        {

            // Parse the provided string into a C# syntax tree
            source = source + @"

namespace System.Runtime.CompilerServices
    {
          internal static class IsExternalInit {}
    }
";
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>
                {
                  MetadataReference.CreateFromFile(@"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.10\ref\net8.0\mscorlib.dll"),
                  MetadataReference.CreateFromFile(@"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.10\ref\net8.0\netstandard.dll"),
                  MetadataReference.CreateFromFile(@"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.10\ref\net8.0\System.Runtime.dll"),
                  MetadataReference.CreateFromFile(@"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.10\ref\net8.0\System.Runtime.Extensions.dll"),
                  MetadataReference.CreateFromFile(@"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.10\ref\net8.0\System.Collections.dll"),
                  MetadataReference.CreateFromFile(@"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.10\ref\net8.0\System.Text.Json.dll"),
                  MetadataReference.CreateFromFile(typeof(AlwaysAttribute).Assembly.Location),
                }
                .ToList();
            // Add in referenced libraries
            //references.Add(MetadataReference.CreateFromFile(typeof(JsonSerializableAttribute).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(JsonTypeInfo).Assembly.Location));
            //references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));

            // Create a Roslyn compilation for the syntax tree.
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                references: references,
                syntaxTrees: new[] { syntaxTree },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug));


            // Emit the compilation and inspect the results
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                // Compilation failed, handle the error
                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.Error.WriteLine(diagnostic);
                }
                throw new Exception("Error compiling test generated code:\n" + String.Join(";\n", result.Diagnostics));
            }


            // Create an instance of our EnumGenerator incremental source generator
            var generator = new ExpanderGenerator();

            // The GeneratorDriver is used to run our generator against a compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the source generator!
            driver = driver.RunGenerators(compilation);

            // Use verify to snapshot test the source generator output!
            return Verifier.Verify(driver);
        }
    }

}
