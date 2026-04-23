# v2 baseline — serialization performance

Captured 2026-04-23 from branch `spike/source-generator` at commit `c2f99e6`. Host: Windows 11, .NET 10.0.7 RyuJIT AVX2, .NET SDK 10.0.201, BenchmarkDotNet 0.14.0. Source: `dotnet/benchmarks/SerializationPerformance` ([`SerializationComparisonBenchmarks.cs`](../../../dotnet/benchmarks/SerializationPerformance/Benchmarks/SerializationComparisonBenchmarks.cs)).

Full numbers + CSV: [`SerializationComparison.md`](SerializationComparison.md) · [`SerializationComparison.csv`](SerializationComparison.csv).

## What's measured

One benchmark class, four data shapes (SimpleModel, SimpleModelList[100], ComplexNestedModel, ComplexNestedModelList[25]). For each shape:

- **`Stj_Reflection`** — raw System.Text.Json, reflection-based resolver. The baseline everything else ratios against.
- **`Stj_SourceGen`** — raw System.Text.Json, `TypeInfoResolver = BenchmarkJsonContext.Default` (STJ's own source generator). What an AOT app would use without Popcorn.
- **`PopcornDefault`** — Popcorn source-generated converters, `?include=` empty. Emits only `[Always]` / `[Default]` members.
- **`PopcornAll`** — Popcorn, `?include=[!all]`. Emits everything (matches STJ output shape).
- **`PopcornCustom`** — Popcorn, hand-built include list (`[Id,Name,IsActive]` or similar depending on model).

Both `DefaultJob` (out-of-process, more trustworthy) and `Job-YCYDSY` (`InProcessEmitToolchain`, configured in [`Program.cs`](../../../dotnet/benchmarks/SerializationPerformance/Program.cs)) are reported by BDN; numbers below quote `DefaultJob`.

## Headline results (DefaultJob, mean time and allocation ratio vs Stj_Reflection)

| Scenario | Stj_SrcGen | Popcorn_Default | Popcorn_All | Popcorn_Custom |
|---|---:|---:|---:|---:|
| SimpleModel (scalar) | 0.98× / 1.00× | 1.50× / 1.78× | 2.14× / 2.41× | 1.99× / 1.92× |
| SimpleModelList (100) | 1.09× / 1.00× | 1.07× / 1.03× | 1.80× / 1.67× | 1.66× / 1.14× |
| ComplexModel | 0.99× / 1.00× | **0.16× / 0.15×** | 1.25× / 1.07× | 1.40× / 1.02× |
| ComplexModelList (25) | 0.98× / 1.00× | **0.12× / 0.10×** | 0.97× / 0.84× | 1.08× / 0.74× |

(Time ratio / allocation ratio. Values < 1 mean Popcorn is faster or allocates less.)

## Findings

1. **STJ source-gen is a wash against STJ reflection at these model sizes.** 0.98×–1.09× time, identical allocation. Metadata source-gen wins on startup and AOT compatibility, not on hot-path serialization cost. This means the interesting comparison is between Popcorn and reflection-STJ — same story either way.

2. **Popcorn's selectivity thesis holds for complex nested data.** `ComplexModelList` (25 nested objects) at `?include=` default is **~8× faster than STJ and allocates ~10× less**. A realistic "mobile client wants a subset" request recovers real cycles. This is the load-bearing claim for v2.

3. **Parity when emitting everything.** `Popcorn_All` on `ComplexModelList` is 0.97× time, 0.84× alloc — indistinguishable from STJ. No runtime cost for having Popcorn in the pipeline when callers ask for everything.

4. **Flat simple data is where Popcorn pays a tax.** `SimpleModelList_PopcornAll` is 1.80×. The per-property include-reference check adds overhead that simple scalars don't recover through selectivity. Acceptable for the selective-fetch value proposition but worth noting: if every caller always asks for everything on flat payloads, Popcorn is slower than raw STJ.

5. **Single-object scalar overhead is visible but small absolute.** `SimpleModel_PopcornAll` is 2.14× time (298ns vs 139ns) and 2.41× alloc (712B vs 296B). In absolute terms this is a ~160ns / ~400B envelope tax per request. Unlikely to matter outside extreme throughput paths.

## Gaps — what this baseline does NOT cover

- **Legacy `PopcornNetStandard` (reflection engine) comparison.** The roadmap calls for 3-way legacy vs source-gen vs STJ. This report does 2-way. Adding legacy requires wiring `PopcornFactory` + reflection-based `.Expand()` into the benchmark project with a compatible configuration for the current benchmark models. Tracked as a follow-up on the merge-gate in [roadmap.md](../../../roadmap.md).
- **Other benchmark classes.** Only `SerializationComparisonBenchmarks` was run. The `IncludeStrategyBenchmarks`, `ScalabilityBenchmarks`, `CircularReferenceBenchmarks`, and `AttributeProcessingBenchmarks` classes exist but are not captured in this baseline.
- **AOT/trim-published numbers.** These measurements are from a JIT-compiled Release build. Native AOT throughput can differ; that comparison belongs with the AOT CI job.

## Reproducing

```bash
cd dotnet/benchmarks/SerializationPerformance
dotnet run -c Release -- comparison
```

Output drops under `BenchmarkDotNet.Artifacts/results/`. Copy the `-report-github.md` and `-report.csv` files here and re-run this summary.
