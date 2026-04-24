using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace MatrixPerformance;

internal static class Program
{
    // This process runs ONE cell of the (.NET version × JIT/AOT) matrix — whatever runtime is
    // hosting it. The orchestration that spans all six cells lives in run-matrix.sh /
    // run-matrix.ps1 at the repo root under benchmarks/matrix/. Each invocation there builds
    // the binary for a specific TFM (and optionally AOT-publishes it), runs this program, and
    // tags the JSON output with a cell label.
    //
    // Why not BDN's built-in cross-runtime jobs? BDN 0.14.0 predates .NET 10, so its toolchain
    // constants (CsProjCoreToolchain.NetCoreApp100, NativeAotToolchain.Net100) don't exist.
    // Building them manually hits an "Invalid TFM: net10.0" error inside BDN's internal TFM
    // parser. Shell orchestration sidesteps this.
    public static int Main(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default.WithToolchain(InProcessNoEmitToolchain.Instance))
            .AddExporter(JsonExporter.Full)
            .AddExporter(MarkdownExporter.GitHub);

        // Why InProcess: BDN's default toolchain builds a tiny auxiliary csproj and spawns a
        // child dotnet process per benchmark. For an AOT-published single-file binary there
        // IS no csproj to rebuild — BDN's validation silently aborts. InProcessEmitToolchain
        // runs the benchmarks directly in this process, which is exactly what we want for
        // the matrix orchestration (runtime selection happens at the outer level, not inside
        // BDN).
        //
        // DON'T pass `args` — BDN's ConfigParser uses CommandLineParser, which reflects on
        // CommandLineOptions. That reflection fails under Native AOT with "Type ... appears
        // to be immutable, but no constructor found to accept values." The matrix script runs
        // all five benchmarks per cell anyway, so filter args aren't needed on the binary.
        var summary = BenchmarkRunner.Run<MatrixBenchmarks>(config);
        return summary.HasCriticalValidationErrors ? 1 : 0;
    }
}
