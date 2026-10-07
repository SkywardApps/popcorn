# Progress: Popcorn v8

Shipped as `8.0.0-preview.1` from `master` (2026-04-24). All former merge-to-master gates are closed; remaining work is tracked in [roadmap.md](../roadmap.md).

## What Exists and Works

### Source generator (`Popcorn.SourceGenerator`)
- `IIncrementalGenerator` pipeline: discovers `JsonSerializerContext` subclasses, filters `[JsonSerializable(typeof(ApiResponse<T>))]` (and `[PopcornEnvelope]` types), walks referenced types transitively (named types, arrays, `IEnumerable<T>`, `IDictionary<K,V>`).
- Emits one `JsonConverter<T>` per reachable type + `AddPopcornOptions()` / `AddPopcornEnvelopes()` registration hooks.
- Handles `[Always]` / `[Default]` / `[Never]` / `[SubPropertyDefault]`, `JsonPropertyName` mapping (wire-name contract — see `systemPatterns.md` § "Name resolution contract"), nullability, nesting, collections, dictionaries, enums (fall-through to STJ), circular-reference guard (`{"$ref":"circular"}`).
- Diagnostics JSG001, JSG003–JSG008 (malformed envelopes, AOT-incompatible polymorphism).

### Runtime shared library (`Popcorn.Shared`)
- Envelopes: `ApiResponse<T>`, `Pop<T>`, `ApiError`; custom envelopes via `[PopcornEnvelope]` markers.
- `PropertyReference` include parser; `IPopcornAccessor`/`PopcornAccessor` per-request parsing.
- `AddPopcorn()` DI, `PopcornOptions` (`EnvelopeType`, `DefaultNamingPolicy`), `PopcornErrorWriterRegistry`, `UsePopcornExceptionHandler()` middleware (buffers body; not for streaming endpoints — see `systemPatterns.md` § "Middleware Constraints").

### Tests
- `Popcorn.FunctionalTests` — xUnit, 20 files, **182 passing / 2 skipped / 0 failing**. The 2 skips are `[JsonDerivedType]` polymorphism dispatch (unimplemented, deferred). Single `TestJsonContext` drives the real generator; `TestServerHelper` for pipeline tests; generated sources inspectable at `$(BaseIntermediateOutputPath)Generated`.
- `Popcorn.SourceGenerator.Tests` — 19 passing; `CSharpGeneratorDriver` harness for JSG003–JSG008 diagnostics.
- Deleted with their features (v8 scope cuts): Sorting/Pagination/Filtering/Authorizer/ExpandFrom/BlindHandler test files.

### AOT smoke test (`PopcornAotExample`)
`CreateSlimBuilder`, `PublishAot=True`, `PublishTrimmed=True`, Dockerfile. Exercises nullable, nested, null-response, and exception-envelope endpoints. Run end-to-end in CI (`aot-ci.yml`).

### Benchmarks (`dotnet/benchmarks/`)
- `SerializationPerformance` — 3-way BenchmarkDotNet suite (STJ vs Popcorn source-gen vs legacy v7 reflection); committed baseline + optimization walk-through under `benchmarks/results/v2-baseline/`. `ci` subset feeds the `benchmarks.yml` ratio gate.
- `ParsingIncludes` — include-parser micro-benchmarks.
- `benchmarks/matrix/` — local .NET 8/9/10 × JIT/AOT investigation tool.
- Headline numbers: Popcorn-default ~10× faster / ~5× less alloc than STJ on ComplexModelList; Popcorn-all 0.87× time / 0.93× alloc (faster than STJ); worst case SimpleModelList-all 1.40×; beats legacy v7 in every scenario (3–8×).

## Generator pitfall ledger (historical bugs, all FIXED)
One line each; full narratives in git history. The conventions that prevent recurrence live in `systemPatterns.md` § "Load-bearing generator conventions".

1. **Enums treated as POCOs** → emitted `{}`; fixed by skipping enums in `GetReferencedTypes` (fall through to STJ, which honors `JsonStringEnumConverter`).
2. **Base-class attributes ignored on derived types** → `GetMembers()` is declared-only; fixed with `GetSerializableProperties`/`GetSerializableFields` walking derived→base with name dedupe.
3. **Dictionary complex-value include collapse** → `firstRef.Children` misread; fixed by passing `value.PropertyReferences` through verbatim.
4. **`Pop<T?>` vs `Pop<T>` NRT signature mismatch (62 CS8620)** → fixed by routing all `Pop<>` type-argument rendering through `TypeNameForPop`.
5. **Root-level primitive registration contaminated `allTypeNames`** → cascade of calls to nonexistent `Pop<primitive>` methods; fixed via `IsBlindSerializableType` filter + direct-serialize branch.
6. **`IDictionary<K,V>` dispatch never matched** → missing space in the type-name constant vs Roslyn's `", "` format; broke `IDictionary`/`ReadOnlyDictionary` targets.
7. **`RegisterConverters.g.cs` CS8669** → missing `#nullable enable` in emitted file.
8. **`Pop{X}Inner` fast-path regression on nested collections** → `TargetEmitsInner` now gates the per-item fast call; nested-collection/dict/`Nullable<T>` shapes use the 4-arg wrapper.

## Feature status
Canonical ledger (per-feature v7→v8 disposition): `apiDesign.md` § "Feature Feasibility Ledger". Summary:
- **Shipped**: include grammar, attribute semantics, custom envelope + exception middleware, `[SubPropertyDefault]`, computed-property translators, lazy loading + blind expansion of declared types (by construction), JSG008 polymorphism diagnostic.
- **Deferred**: `[JsonDerivedType]` dispatch (2 skipped tests), deserialization, factories, header-based include.
- **Dropped**: sorting, pagination, filtering, authorizers, `SetContext`, inspectors-as-lambdas, `[ExpandFrom]`, `[Translator]`+DI, `IPopcornBlindHandler` — replacements in `docs/MigrationV7toV8.md`.

## Thesis validation
1. **Perf parity or better** — confirmed by 3-way baseline; continuously enforced by `benchmarks.yml` ratio gate.
2. **Native AOT works** — enforced end-to-end by `aot-ci.yml` (container build + endpoint assertions).
3. **Trimming works** — `PublishTrimmed=True` rides along in the same AOT publish; no separate trim-only run documented.

## Legacy engine position
`PopcornNetStandard` + `PopcornNetStandard.WebApiCore` remain in-tree and on NuGet (`Skyward.Api.Popcorn` v7) during the deprecation window. `main.yml` no longer packs v7 (v8-only publishing).
