using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using SerializationPerformance.Benchmarks;

namespace SerializationPerformance;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance))
            .AddExporter(CsvExporter.Default)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(JsonExporter.Full)
            .AddExporter(MarkdownExporter.GitHub);

        if (args.Length == 0)
        {
            ShowMenu();
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "all":
                RunAllBenchmarks(config);
                break;
            case "comparison":
                BenchmarkRunner.Run<SerializationComparisonBenchmarks>(config);
                break;
            case "includes":
                BenchmarkRunner.Run<IncludeStrategyBenchmarks>(config);
                break;
            case "scalability":
                BenchmarkRunner.Run<ScalabilityBenchmarks>(config);
                break;
            case "circular":
                BenchmarkRunner.Run<CircularReferenceBenchmarks>(config);
                break;
            case "attributes":
                BenchmarkRunner.Run<AttributeProcessingBenchmarks>(config);
                break;
            case "ci":
                // Focused benchmark pass for the CI ratio gate. Filters to the 5 benchmarks that
                // back the three load-bearing ratios (worst-case, headline, selectivity) vs their
                // STJ source-gen baselines. ShortRun is too noisy for ratio detection — the
                // denominator (Stj_SourceGen) can jump ~30% on a cold iteration, squashing the
                // ratio and producing false passes/fails. So we run the class's default
                // [SimpleJob] (3 warmup + 15 iterations) — ~90 seconds total on a typical box.
                // Pair with .github/scripts/compare-benchmark-ratios.py for the gate check.
                var ciConfig = ManualConfig.Create(DefaultConfig.Instance)
                    .AddExporter(JsonExporter.Full);
                BenchmarkRunner.Run<SerializationComparisonBenchmarks>(ciConfig, new[]
                {
                    "--filter",
                    "*SimpleModelList_Stj_SourceGen",
                    "*SimpleModelList_PopcornAll",
                    "*ComplexModelList_Stj_SourceGen",
                    "*ComplexModelList_PopcornAll",
                    "*ComplexModelList_PopcornDefault",
                });
                break;
            case "parsing":
                Console.WriteLine("Running existing parsing benchmarks...");
                Console.WriteLine("Please run the ParsingIncludes project separately.");
                break;
            default:
                Console.WriteLine($"Unknown benchmark: {args[0]}");
                ShowMenu();
                break;
        }

        Console.WriteLine("Benchmark completed.");
        if (!Console.IsInputRedirected && Environment.UserInteractive)
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine("Popcorn Performance Benchmarks");
        Console.WriteLine("==============================");
        Console.WriteLine();
        Console.WriteLine("Usage: SerializationPerformance.exe [benchmark]");
        Console.WriteLine();
        Console.WriteLine("Available benchmarks:");
        Console.WriteLine("  all         - Run all benchmarks (may take a long time)");
        Console.WriteLine("  comparison  - System.Text.Json vs Popcorn serialization comparison");
        Console.WriteLine("  includes    - Include strategy performance (default vs all vs custom)");
        Console.WriteLine("  scalability - Big O analysis (flat lists and deep nesting)");
        Console.WriteLine("  circular    - Circular reference detection overhead");
        Console.WriteLine("  attributes  - Attribute processing overhead ([Always], [Never], [Default])");
        Console.WriteLine("  ci          - CI ratio gate: ShortRun over 5 benchmarks (~2-3 min)");
        Console.WriteLine("  parsing     - Property reference parsing (run ParsingIncludes project)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SerializationPerformance.exe comparison");
        Console.WriteLine("  SerializationPerformance.exe scalability");
        Console.WriteLine();
        Console.WriteLine("Note: Some benchmarks use placeholder JSON serialization until");
        Console.WriteLine("actual Popcorn serialization integration is implemented.");
    }

    private static void RunAllBenchmarks(IConfig config)
    {
        Console.WriteLine("Running all performance benchmarks...");
        Console.WriteLine("This may take a significant amount of time.");
        Console.WriteLine();

        Console.WriteLine("1/5 - Serialization Comparison Benchmarks");
        BenchmarkRunner.Run<SerializationComparisonBenchmarks>(config);

        Console.WriteLine("2/5 - Include Strategy Benchmarks");
        BenchmarkRunner.Run<IncludeStrategyBenchmarks>(config);

        Console.WriteLine("3/5 - Scalability Benchmarks");
        BenchmarkRunner.Run<ScalabilityBenchmarks>(config);

        Console.WriteLine("4/5 - Circular Reference Benchmarks");
        BenchmarkRunner.Run<CircularReferenceBenchmarks>(config);

        Console.WriteLine("5/5 - Attribute Processing Benchmarks");
        BenchmarkRunner.Run<AttributeProcessingBenchmarks>(config);

        Console.WriteLine("All benchmarks completed!");
    }
}
