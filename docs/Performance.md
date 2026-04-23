# [Popcorn](../README.md) > Performance

[Table Of Contents](TableOfContents.md)

Popcorn's value proposition — "let the client ask for exactly the fields it needs in one round trip" — has always had a performance story attached to it, but until recently that story wasn't benchmarked. This page summarizes what we measured on the v2 (source-generator) branch once the numbers were in.

> **Scope:** these numbers apply to the upcoming **v2** `.NET` provider (the Roslyn source generator on [the `spike/source-generator` branch](https://github.com/SkywardApps/popcorn/tree/spike/source-generator)), not the v1 reflection-based `Skyward.Api.Popcorn` package on NuGet today. v2 has not shipped yet.

## What we measured

Four payload shapes, seven comparison points each, using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet):

| Payload | Description |
|---|---|
| `SimpleModel` | One flat object, five scalar properties. |
| `SimpleModelList[100]` | 100 of those in an array. |
| `ComplexNestedModel` | One object with nested objects, collections, a dictionary, and a self-referencing child. |
| `ComplexNestedModelList[25]` | 25 of those, each with its own nested children. |

For each shape, we measured seven serialization paths:

| Path | What it represents |
|---|---|
| `Stj_Reflection` | Raw `System.Text.Json`, no source generator. The "how fast is plain JSON" baseline. |
| `Stj_SourceGen` | Raw `System.Text.Json` with its own `JsonSerializerContext` source generator. The AOT-friendly baseline with no Popcorn involvement. |
| `Popcorn_Default` | Popcorn v2, `?include=` empty. Server emits only `[Default]` / `[Always]` fields. |
| `Popcorn_All` | Popcorn v2, `?include=[!all]`. Server emits everything (same JSON shape as raw STJ). |
| `Popcorn_Custom` | Popcorn v2, client asks for a hand-picked subset. |
| `Legacy_Default` / `Legacy_All` / `Legacy_Custom` | Same shapes against the v1 `PopcornNetStandard` reflection engine — the library most users are running in production today. |

Full numbers, raw BDN output, and the methodology live under [`benchmarks/results/v2-baseline/`](../benchmarks/results/v2-baseline/README.md).

## Headline results

Ratios are relative to `Stj_Reflection` (smaller = faster / less allocation). **Bold** marks the most interesting cells.

| Scenario | Popcorn_Default | Popcorn_All | Legacy_Default | Legacy_All |
|---|---:|---:|---:|---:|
| SimpleModel | 1.24× / 1.41× | 1.77× / 2.03× | 5.12× / 5.81× | 8.60× / 9.59× |
| SimpleModelList (100) | **0.81× / 1.03×** | 1.40× / 1.67× | 4.50× / 5.34× | 8.08× / 9.26× |
| ComplexModel | **0.15× / 0.17×** | 1.21× / 1.16× | 0.49× / 0.55× | 4.73× / 4.67× |
| ComplexModelList (25) | **0.10× / 0.10×** | **0.87× / 0.93×** | 0.57× / 0.67× | 3.61× / 4.41× |

(Time ratio / allocation ratio. Values < 1 mean Popcorn is *faster* or *allocates less* than raw STJ.)

### Reading the table

- **`Popcorn_Default` on `ComplexModelList` is 0.10× time / 0.10× allocation** — ~10× faster than raw STJ, allocates ~10× less. This is the load-bearing claim: when the client asks for just a subset of a nested payload, Popcorn's selective emission at the serializer level pays off dramatically. The server doesn't materialize fields the client isn't going to read.
- **`Popcorn_All` on `ComplexModelList` is 0.87× / 0.93×** — Popcorn is *faster* than raw STJ even when asked to emit everything on nested data. The per-property include-reference check isn't free, but the generator's tight per-type write paths more than compensate. There's no "Popcorn tax" to pay for keeping the feature available.
- **Flat simple data is Popcorn's weakest scenario.** `SimpleModelList_PopcornAll` at 1.40× time / 1.67× alloc — the per-property include check has no payoff when every property fits in a few bytes. Still not parity with raw STJ, but acceptable — and still ~5.8× faster than the v1 legacy engine on the same workload.
- **Every cell beats the v1 legacy engine.** `Legacy_All` is 3–8× slower than raw STJ because the reflection engine materializes an intermediate `Dictionary<string, object?>` before serialization. The v2 source-generator migration has no regression scenario vs v1 — just wins.

## Why v2 is faster

Three mechanisms, in descending order of impact:

1. **Selective emission at the serializer level.** The source generator emits one `JsonConverter<T>` per registered type, with explicit per-property write statements gated on the incoming include list. When `include=` is empty or small, most properties never get touched — no getter call, no UTF-8 encoding, no allocation for their JSON representation.
2. **No runtime reflection on the hot path.** Every field access in a generated converter is a direct `.` property read emitted at build time. There's no `PropertyInfo.GetValue(...)` per request, no `GetCustomAttributes()` scan, no dynamic dispatch. This is also why v2 works under Native AOT (`PublishAot=True`) and trimmed publishes (`PublishTrimmed=True`) — no metadata to strip.
3. **No intermediate projection.** The v1 legacy engine builds a `Dictionary<string, object?>` describing the output, then serializes that dictionary. The v2 generator writes directly to the `Utf8JsonWriter` — the dictionary step is gone. This accounts for most of the 5× allocation gap between legacy and v2.

In addition to these, three generator-level optimizations landed after the initial baseline was captured (LINQ→for-loops in emitted code, hoisted `useAll`/`useDefault`/naming-policy setup out of list-iteration inner loops, and elided `HashSet<object>` allocation for type graphs the generator can prove are cycle-free). Their combined effect is what tipped `ComplexModelList_PopcornAll` from parity (0.97×) to faster-than-STJ (0.87×). Step-by-step breakdown + raw per-step BDN output: [`opt-iterations/`](../benchmarks/results/v2-baseline/opt-iterations/README.md).

## Caveats

- **v2 is not on NuGet yet.** These numbers describe the `spike/source-generator` branch. The shipping NuGet package (`Skyward.Api.Popcorn` v7) is the v1 reflection engine and matches the `Legacy_*` rows.
- **Numbers are Windows + .NET 9 + x64 RyuJIT AVX2.** Absolute times will differ on other platforms; ratios are more portable than absolutes.
- **Benchmark models are representative, not exhaustive.** Real API payloads vary; the ratios here are a useful guide but not a guarantee. If your workload looks different (very deep graphs, pathological include lists, unusually large primitives), measure against your own models before committing.
- **Include-parameter parsing is not benchmarked here.** `SerializationComparisonBenchmarks` measures serialization only — it assumes `PropertyReference[]` already in hand. The separate `ParsingIncludes` benchmark in the repo covers the parser.

## Reproducing

```bash
git checkout spike/source-generator
cd dotnet/benchmarks/SerializationPerformance
dotnet run -c Release -- comparison
```

Output drops under `BenchmarkDotNet.Artifacts/`. The `*-report-github.md` file matches the tables in [`benchmarks/results/v2-baseline/SerializationComparison.md`](../benchmarks/results/v2-baseline/SerializationComparison.md).

## Full numbers

- [`benchmarks/results/v2-baseline/README.md`](../benchmarks/results/v2-baseline/README.md) — narrative summary and methodology.
- [`benchmarks/results/v2-baseline/SerializationComparison.md`](../benchmarks/results/v2-baseline/SerializationComparison.md) — full BDN-formatted results.
- [`benchmarks/results/v2-baseline/SerializationComparison.csv`](../benchmarks/results/v2-baseline/SerializationComparison.csv) — raw data.
- [`benchmarks/results/v2-baseline/opt-iterations/README.md`](../benchmarks/results/v2-baseline/opt-iterations/README.md) — the three-step generator optimization walk.
