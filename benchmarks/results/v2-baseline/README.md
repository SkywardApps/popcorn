# v2 baseline — serialization performance (3-way)

Captured 2026-04-23 from branch `spike/source-generator` at the head of that branch. Host: Windows 11, .NET 9.0.15 RyuJIT AVX2, .NET SDK 10.0.201, BenchmarkDotNet 0.14.0. Source: `dotnet/benchmarks/SerializationPerformance` ([`SerializationComparisonBenchmarks.cs`](../../../dotnet/benchmarks/SerializationPerformance/Benchmarks/SerializationComparisonBenchmarks.cs)).

Full numbers + CSV: [`SerializationComparison.md`](SerializationComparison.md) · [`SerializationComparison.csv`](SerializationComparison.csv).

> **Note on runtime drift.** The earlier 2-way baseline (not preserved — superseded by this run) recorded host `.NET 10.0.7`. This 3-way run landed on `.NET 9.0.15` because the benchmark project targets `net8.0` and the local dotnet host rolled forward to the next available shared runtime. Ratios are internally consistent within this run; absolute numbers drift ~15–25% slower than the earlier .NET 10 numbers across every engine. Re-running under a pinned .NET 10 host is a future cleanup if we want to compare ratios across time.

## What's measured

One benchmark class, four data shapes (SimpleModel, SimpleModelList[100], ComplexNestedModel, ComplexNestedModelList[25]). For each shape, eight method variants:

- **`Stj_Reflection`** — raw `System.Text.Json`, reflection-based resolver. The baseline everything else ratios against.
- **`Stj_SourceGen`** — raw `System.Text.Json`, `TypeInfoResolver = BenchmarkJsonContext.Default` (STJ's own source generator). What an AOT-targeting app would use without Popcorn.
- **`PopcornDefault`** — Popcorn source-generated converters, `?include=` empty. Emits only `[Always]` / `[Default]` members.
- **`PopcornAll`** — Popcorn source-gen, `?include=[!all]`. Emits everything (matches STJ output shape).
- **`PopcornCustom`** — Popcorn source-gen, hand-built include list.
- **`LegacyDefault`** — legacy `PopcornNetStandard` (runtime-reflection expander), empty includes. `PopcornFactory.Expand(...)` → `Dictionary<string, object?>` → reflection-STJ. Per-request `CreatePopcorn()` allocation included.
- **`LegacyAll`** — legacy with an explicit full enumeration of non-Never properties (`!all` in the legacy engine has a latent `ArgumentException` collision with `AlwaysInclude`, so we pass the explicit list instead).
- **`LegacyCustom`** — legacy with the same hand-built include list as `PopcornCustom`.

Both `DefaultJob` (out-of-process, more trustworthy) and `Job-HLUJQF` (`InProcessEmitToolchain`, configured in [`Program.cs`](../../../dotnet/benchmarks/SerializationPerformance/Program.cs)) are reported by BDN; numbers below quote `DefaultJob`.

## Headline results (DefaultJob, mean time and allocation ratio vs Stj_Reflection)

| Scenario | Stj_SrcGen | Popcorn_Default | Popcorn_All | Popcorn_Custom | Legacy_Default | Legacy_All | Legacy_Custom |
|---|---:|---:|---:|---:|---:|---:|---:|
| SimpleModel | 0.98× / 1.00× | 1.52× / 2.00× | 2.03× / 2.62× | 1.94× / 2.14× | **5.12× / 5.81×** | **8.64× / 9.59×** | **4.32× / 5.73×** |
| SimpleModelList (100) | 1.01× / 1.00× | 1.10× / 1.26× | 1.80× / 1.90× | 1.59× / 1.36× | **4.73× / 5.34×** | **8.46× / 9.26×** | **4.99× / 5.25×** |
| ComplexModel | 1.02× / 1.00× | **0.15× / 0.17×** | 1.59× / 1.28× | 1.41× / 1.23× | 0.47× / 0.54× | **4.67× / 4.65×** | **3.04× / 2.95×** |
| ComplexModelList (25) | 0.93× / 1.00× | **0.10× / 0.13×** | 0.97× / 1.03× | 0.94× / 0.92× | 0.48× / 0.67× | **3.40× / 4.41×** | **1.84× / 2.59×** |

(Time ratio / allocation ratio. Values < 1 mean faster or less allocation than reflection-STJ.)

## Findings

1. **Popcorn source-gen is dramatically faster than the legacy reflection engine, everywhere.** For the `All` case (fair fight — both engines emit the same fields), source-gen is 3–8× faster than legacy. For `Default` (engine's own include-filtering win), source-gen on `ComplexModelList` is **4.7× faster than legacy-default** (3.9µs vs 18.3µs) with 5× less allocation. This is the headline v2 migration claim validated end-to-end: moving from runtime reflection to the Roslyn generator buys real throughput.

2. **Legacy's reflection engine pays a heavy allocation tax.** Even best-case `LegacyDefault` on `SimpleModelList[100]` allocates **152KB vs STJ's 28KB** — a 5× bloat from building the intermediate `Dictionary<string, object?>` projection before JSON-serialization. Source-gen skips that step entirely.

3. **Popcorn's selectivity thesis holds, and holds harder against legacy.** `ComplexModelList_PopcornDefault` is 3,884 ns / 6,992 B: ~10× faster than STJ and ~5× faster than legacy-default for the same "mobile client asks for a subset" workload. The "pay less when you ask for less" claim is consistent across both comparison axes.

4. **STJ source-gen remains a wash vs STJ reflection** at these model sizes (0.98×–1.02× time, identical allocation). Metadata source-gen is about startup + AOT compatibility, not hot-path throughput. Popcorn's gains do not come from the STJ source-gen it sits on top of.

5. **Parity when emitting everything.** `Popcorn_All` on `ComplexModelList` is 0.97× time, 1.03× alloc vs reflection-STJ — indistinguishable. Popcorn adds no meaningful runtime cost when callers ask for everything. Legacy cannot say the same: `LegacyAll` on the same shape is 3.4× slower and 4.4× more alloc.

6. **Flat simple data is where Popcorn pays a tax, but still beats legacy.** `SimpleModelList_PopcornAll` is 1.80×/1.90× vs STJ — the per-property include-reference check overhead on flat scalars. But `SimpleModelList_LegacyAll` is 8.5×/9.3×, so even the "worst case for Popcorn" is 4.7× faster than legacy for the identical workload. The v2 migration has no scenario in which legacy is faster than v2.

7. **Single-object scalar overhead is the one real shrink.** `SimpleModel_PopcornAll` is 2.03× time (351 ns vs 173 ns) and 2.62× alloc (776 B vs 296 B). Absolute terms: ~180 ns / ~480 B envelope tax per request. Unlikely to matter outside extreme throughput paths; even so, still 4× faster than legacy's 1,493 ns.

## Legacy configuration caveats

The legacy `PopcornNetStandard` engine is configured in the benchmark to mirror the current models' `[Always]` / `[Default]` / `[Never]` attribute semantics via `PopcornFactory.ConfigureType<T>(...)`. Two concessions were made to keep the legacy engine runnable with the current model set:

- `AlwaysInclude` is left empty and `[Always]` fields are moved into `DefaultInclude` instead. Legacy's `DeterminePropertyReferences` adds `AlwaysInclude` entries without a `ContainsKey` guard; any include list (or `!all` enumeration) that already names that field throws `ArgumentException`. Putting the Always fields into `DefaultInclude` produces the same output for our three test include shapes (empty / all / custom) without tripping the collision.
- `!all` is replaced in the `LegacyAll` benchmark with an explicit enumeration of every non-Never property. Same reason — `!all`'s internal enumeration path trips the same collision when combined with any `AlwaysInclude` fields. Behaviorally identical for the benchmark models; avoids the latent bug.

Both are workarounds for real defects in the legacy engine that production callers would have either dodged with careful config or accepted as "don't combine these features." The benchmark numbers above reflect the engine running under configuration it supports.

## Gaps — what this baseline does NOT cover

- **Other benchmark classes.** Only `SerializationComparisonBenchmarks` was run. The `IncludeStrategyBenchmarks`, `ScalabilityBenchmarks`, `CircularReferenceBenchmarks`, and `AttributeProcessingBenchmarks` classes exist but are not captured in this baseline and do not have legacy variants wired in.
- **AOT/trim-published numbers.** JIT-compiled Release build only. Native AOT throughput belongs with the AOT CI job.
- **Runtime consistency with earlier 2-way baseline.** This run is on .NET 9.0.15 (rollforward); the earlier 2-way was on .NET 10.0.7. See note at the top.

## Reproducing

```bash
cd dotnet/benchmarks/SerializationPerformance
DOTNET_ROLL_FORWARD=Major dotnet run -c Release -- comparison
```

Output drops under `dotnet/benchmarks/SerializationPerformance/BenchmarkDotNet.Artifacts/`. The per-report markdown/CSV files are copied here manually.
