# Active Context: Popcorn

## Current Branch
`spike/source-generator` — experimental, not merged to `master`.

**Mission**: replace the legacy reflection-based Popcorn engine (`PopcornNetStandard*`) with a Roslyn source generator (`Popcorn.SourceGenerator`) so Popcorn can run under Native AOT (`PublishAot=True`) and IL trimming (`PublishTrimmed=True`), while also reducing serialization overhead.

## Focus Areas

### 1. Source generator correctness
Generator at `dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs`. Discovery via `ForAttributeWithMetadataName(JsonSerializableAttribute)` + filter for `JsonSerializerContext` subclasses whose generic argument inherits `ApiResponse<T>`. Emits one `JsonConverter` per reachable type + a `PopcornJsonOptionsExtension.AddPopcornOptions()` registrar.

### 2. Functional parity on the generator path
`dotnet/Tests/Popcorn.FunctionalTests` covers: primitives, value types, basic + advanced collections, dictionaries (incl. `ConcurrentDictionary`, `ImmutableArray`, `HashSet`, `ReadOnlyCollection`, `ObservableCollection`), nested `[Always]`, conflicting attributes, include-parameter variations, default-behavior rules. See `TestPlan.md` for the full matrix.

### 3. AOT smoke test
`PopcornAotExample` — `WebApplication.CreateSlimBuilder`, `PublishAot=True`, `PublishTrimmed=True`, Dockerfile. Confirms the generated pipeline runs in AOT. Keep this project working; it is the canary for the whole migration thesis.

### 4. Performance benchmarks
`dotnet/benchmarks/SerializationPerformance` (BenchmarkDotNet) compares stock `System.Text.Json` vs Popcorn across include strategies, scalability (flat 10→100k, nested 1→20), circular-ref overhead, attribute processing. `dotnet/benchmarks/ParsingIncludes` benchmarks the include-string parser specifically.

## Known Gaps vs Legacy Reflection Engine
See `apiDesign.md` for the v2 API plan and `migrationAnalysis.md` for the feasibility analysis per feature.

**Dropped from v2 scope (will NOT be implemented)**: sorting, filtering, pagination, authorizers. Never used in practice with the legacy engine; complexity not justified. Callers that need these behaviors implement them at the endpoint level.

Tier-1 (MUST ship before v2.0 merge): custom envelope + exception middleware, `[SubPropertyDefault]`.

Tier-2 (SHOULD ship with v2.0): `[Translator]` methods with DI, `IPopcornBlindHandler<TFrom,TTo>`, `[ExpandFrom]`.

Tier-3 (defer/drop): factories (moot until deserialization), deserialization (out of scope), legacy `Dictionary<string,object>` context (dropped — superseded by DI).

Already supported by construction: lazy loading, blind expansion of user-declared types, optional-property include prefix.

Genuine non-starter under AOT: polymorphic unknown-at-build-time types (trimmer removes the metadata). Document the requirement, emit a generator diagnostic.

## Known Issues
- `PropertyReference.ParseIncludeStatement` in `Popcorn.Shared` has a parsing issue around nested dictionary values — surfaced by one failing functional test. Fix required in shared library, not in the generator itself.
- Dictionary complex-value attribute application still imperfect for some nested shapes.

## Open Questions
- Header-based include (`POPCORN-INCLUDE`) — implement in this spike or defer?
- Schema/OpenAPI generation for include-aware endpoints — not in this spike.
- Client libraries (TS/JS) — out of scope for .NET spike, but protocol decisions here constrain them.

## Recent Activity (branch commits, most recent first)
- `f124e3a` Full benchmarks
- `50e1aa7` Working basic benchmark
- `8e059de` Undid some regressions
- `de9d328` 98% success
- `7cf86db` Nullable and reference types
- `5ae4bd1` Simple types testing
- `6dccf38` Test special parameter handling
- `0325e48` Working initial batch of tests
- `387ff4d` Add early support for IEnumerable
- `3ffc9c2` Refined options to accept naming convention; update libraries
- `857d89f` HttpContext-relative implementation
- `571545b` Use JsonSerializerAttribute instead of our own
- `1b39e30` Early experimentation with source generation

## Immediate Next Steps (suggested, not committed)
1. Land `[SubPropertyDefault]` (Tier-1 remaining).
2. Fix `PropertyReference.ParseIncludeStatement` nested-structure bug and re-run dictionary complex-value test.
3. Run the BenchmarkDotNet suite and record a baseline; compare against legacy reflection numbers.
4. Decide Tier-2 scope for the v2.0 merge bar (`[Translator]` / `IPopcornBlindHandler` / `[ExpandFrom]`).
5. Consider header-based include decision (`POPCORN-INCLUDE`).

## Non-Goals for This Spike
- Deserialization / two-way serialization.
- Multi-platform providers (PHP, JS client).
- Schema generation.
- Rewriting the legacy projects — they stay available until the spike merges.
