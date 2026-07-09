# Popcorn Roadmap

Open work on the v8 (source-generator) line. The former `spike/source-generator` branch merged to `master` and shipped as **`8.0.0-preview.1`** (tag `release/v8.0.0-preview.1`, 2026-04-24) — all pre-merge gates are closed (see "Completed milestones" at the bottom). For historical bug/fix context see `memory-bank/progress.md` and git log.

Last updated: 2026-07-09.

## Status snapshot

- Core protocol (include parsing, attribute semantics, nested expansion, collections, dictionaries, enums, polymorphism-basic, circular refs, full nullability matrix): **shipped**.
- Tier-1 feature set — custom envelope + `UsePopcornExceptionHandler` + `[SubPropertyDefault]`: **shipped**.
- Test suite: 182 passing / 2 skipped / 0 failing in `Popcorn.FunctionalTests` (the 2 skips are `[JsonDerivedType]` polymorphism dispatch); 19 passing in `Popcorn.SourceGenerator.Tests`. Zero CS86xx warnings in generated code.
- CI: [`tests.yml`](.github/workflows/tests.yml) (unit/functional), [`aot-ci.yml`](.github/workflows/aot-ci.yml) (container end-to-end), [`benchmarks.yml`](.github/workflows/benchmarks.yml) (relative-perf gate), [`main.yml`](.github/workflows/main.yml) (tag `release/vX.Y.Z` → NuGet + GitHub release).
- Packages on NuGet: `Skyward.Api.Popcorn.SourceGen` + `Skyward.Api.Popcorn.SourceGen.Shared` at 8.0.0-preview.1. Legacy v7 `Skyward.Api.Popcorn` still available; no longer packed by CI.
- Legacy reflection engine (`PopcornNetStandard*`): still in the tree, unchanged. Planned removal after v8 ships side-by-side for a release or two.

## Open items

### Toward 8.0.0 stable
- Gather preview feedback; decide what (if anything) gates dropping the `-preview` suffix.
- **v7 deprecation window**: proposed "v7 remains on NuGet for at least one release after v8.0 ships; v7 gets a `<PackageReleaseNotes>` banner pointing at [docs/MigrationV7toV8.md](docs/MigrationV7toV8.md)." Not yet actioned.
- ~~**Example projects refresh**~~ **Resolved 2026-07-09**: `dotnet/Examples/PopcornNet5Example/` (v7-shaped, `net5.0`/EOL, CI-invisible) was deleted along with the rest of `dotnet/Examples/`; [`dotnet/PopcornAotExample/`](dotnet/PopcornAotExample/) is the canonical example. If a controllers-based (non-minimal-API) v8 sample is ever wanted, track it as a new item.

### Polymorphism dispatch — deferred (2 skipped tests)
- [`PolymorphismTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/PolymorphismTests.cs): `[JsonDerivedType]`-registered derived types need generator-emitted per-item type dispatch.
- The "unknown at build time" half is done: **JSG008 diagnostic shipped** — warns when a member is typed `object`, abstract class, or interface.
- Scope: medium, mostly generator-side. Defer unless a consumer blocks on it.

### Header-based include transport (`POPCORN-INCLUDE`)
- **Why**: URLs have length limits; `GET /foo?include=[very,long,list,...]` can blow past proxy limits.
- **Design**: `PopcornAccessor.PropertyReferences` checks `HttpContext.Request.Headers["POPCORN-INCLUDE"]` first, falls back to query `?include=`. No breaking change.
- **Status**: spec'd in `memory-bank/apiDesign.md`, not started. Scope: tiny, ~1 day including tests.

### Tier 3 — v8.x or drop
- **Factories** — moot until deserialization ships. `[Factory]`-tagged static method for instantiating types during read.
- **Deserialization** — out of scope for v8.0. Generator currently emits write-only converters.

## Deferred-quality items (low severity)

Confirmed against [`ExpanderGenerator.cs`](dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs) on 2026-04-23.

- **Pragma scope in generated converter files is slightly broad.** `#pragma warning disable CS8619, CS8600, CS8601, CS8625` at file scope. CS8619 / CS8625 are load-bearing (NRT-cast through generated code). CS8600 / CS8601 are defensive; could mask a real null bug introduced by a future generator change. Narrow to per-statement where feasible.
- **User-defined non-generic subclasses of Dictionary/IDictionary will crash the generator.** `class SettingsDict : Dictionary<string, string> {}` has `TypeArguments.Length == 0`; the generator accesses `TypeArguments[1]` unguarded → `IndexOutOfRangeException`. Fix: walk the `IDictionary<K, V>` interface chain for `TypeArguments` rather than reading the target type's own list.
- **`IsBlindSerializableType` uses stringly-typed hashset lookups.** Matches the pre-existing convention (`NumberTypes`, `StringTypes`, `BoolTypes`, `IgnoreTypes` compared via `ToDisplayString().Replace("?", "")`). Fragile to Roslyn display-format changes. Future cleanup: replace with `SpecialType` / `ITypeSymbol` identity comparisons.
- **Cycle-safety analyzer is conservative on non-blind unregistered types.** `IsNamedTypeCycleSafe` treats any type NOT in `allTypeNames` as cycle-safe. Correct today — unregistered user types fall through to `JsonSerializer.Serialize`. Revisit if a future change starts recursing through such types.

## Remaining performance levers

Considered but not taken in the 2026-04 optimization pass. In rough order of expected payoff:

- **Pre-encoded property names via `JsonEncodedText`.** STJ's own source-gen path does this; saves per-property UTF-16→UTF-8 encoding. Complicated by runtime `PropertyNamingPolicy` (forces per-options caching). Biggest remaining lever; would likely close most of the `SimpleModelList_PopcornAll` 1.40× gap vs raw STJ.
- **Skip the per-property include-match scan when `useAll && !hasNegations`.** Every property unconditionally emits under `!all` with no negations; the scan is pure overhead. Moderate payoff on `Popcorn_All` scenarios.
- **Hashtable-keyed include-list lookup.** Current linear scan is O(n·m). Marginal — include lists are typically small.

Benchmark discipline: when a real optimization ships, re-run `dotnet run -- ci` in `SerializationPerformance` locally and commit the new [`benchmarks/results/ci-baseline.json`](benchmarks/results/ci-baseline.json) in the same PR. When an intentional regression ships, bump the baseline upward. The gate compares Popcorn/STJ *ratios* (stable across runners), not absolute timings (±20–30% runner noise).

## Open questions
- Schema / OpenAPI generation for include-aware endpoints — out of scope; revisit if a consumer requests it.
- Cross-language provider kit (JS/TS client) — protocol decisions constrain future clients; no active work.

## Completed milestones (was: merge-to-master gates)

All pre-merge gates closed before `8.0.0-preview.1`:

- [x] Scope decision: sorting/pagination/filtering/authorizers dropped; Tier-2 (`[ExpandFrom]`, `[Translator]`+DI, `IPopcornBlindHandler`) cleared 2026-04-23 with replacements documented in [docs/MigrationV7toV8.md](docs/MigrationV7toV8.md) §5/§7/§8.
- [x] Tier-1: custom envelope + exception middleware; `[SubPropertyDefault]`.
- [x] 3-way benchmark baseline ([`benchmarks/results/v2-baseline/`](benchmarks/results/v2-baseline/README.md)) + three generator optimizations ([walk-through](benchmarks/results/v2-baseline/opt-iterations/README.md)). Headline: Popcorn-all on ComplexModelList 0.87× time / 0.93× alloc vs STJ; Popcorn-default 0.11×; worst case SimpleModelList-all 1.49×; beats legacy v7 everywhere (3–8×).
- [x] CI: tests, AOT container end-to-end, relative-perf ratio gate.
- [x] NuGet two-package design, SourceLink, snupkg; local + consumer-project smoke tests.
- [x] v7→v8 migration guide; JSG008 diagnostic.
- [x] Tag `release/v8.0.0-preview.1` pushed; release workflow live ([`main.yml`](.github/workflows/main.yml)).
