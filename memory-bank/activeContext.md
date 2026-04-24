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

Tier-1 (MUST ship before v2.0 merge): custom envelope + exception middleware (shipped), `[SubPropertyDefault]` (shipped). **Tier-1 complete.**

Tier-2 (SHOULD ship with v2.0): `[Translator]` methods with DI, `IPopcornBlindHandler<TFrom,TTo>`. `[ExpandFrom]` was dropped 2026-04-23 — see `docs/MigrationV7toV8.md` §7 for the replacement patterns (`[Never]` on source / hand-written factory / Mapster).

Tier-3 (defer/drop): factories (moot until deserialization), deserialization (out of scope), legacy `Dictionary<string,object>` context (dropped — superseded by DI).

Already supported by construction: lazy loading, blind expansion of user-declared types, optional-property include prefix.

Genuine non-starter under AOT: polymorphic unknown-at-build-time types (trimmer removes the metadata). Document the requirement, emit a generator diagnostic.

## Known Issues
- *No open parser / dictionary / nullability bugs.* Recent fixes: (1) dictionary complex-value include passthrough (value.PropertyReferences now passed verbatim to dictionary value types). (2) Four-bug nullability cleanup — NRT-annotation normalization at every `Pop<T>` callsite, primitive-at-root no longer cross-contaminates `allTypeNames`, `IDictionary<K,V>` / `ReadOnlyDictionary<K,V>` target-type dispatch works (was failing due to whitespace mismatch in a constant), `RegisterConverters.g.cs` has `#nullable enable`. Generated-code warning count dropped from 64 CS86xx to 0. (3) `Pop{X}Inner` regression fix — `TargetEmitsInner(ITypeSymbol)` helper gates the fast per-item call in `CreateArraySerializer` / `CreateDictionarySerializer`; nested-collection / nested-dict / list-of-`Nullable<T>` shapes fall back to the 4-arg `Pop{X}` wrapper. Solution builds clean; FunctionalTests at 182 / 9 / 0 (down from 13 skipped after `[ExpandFrom]` was dropped from v2 scope and its 4 skipped tests were deleted).

## Deferred quality items (promoted to [roadmap.md](../roadmap.md) → "Deferred-quality items" section)

These five items are tracked in the roadmap now; kept here as a back-reference for future maintainers reviewing `activeContext.md`.

- Pragma scope in generated converter files is slightly broad (CS8619/CS8625 load-bearing; CS8600/CS8601 defensive).
- User-defined non-generic subclasses of `Dictionary`/`IDictionary` crash the generator at `ExpanderGenerator.cs:847`.
- `IsBlindSerializableType` uses stringly-typed hashset lookups (fragile to Roslyn display-format changes).
- Cycle-safety analyzer conservative on unregistered non-blind types (correct today; revisit when `IPopcornBlindHandler` lands).
- Remaining perf levers: pre-encoded `JsonEncodedText` property names (biggest), skip include-scan when `useAll && !hasNegations` (moderate), hashtable-keyed include match (marginal).

## Open Questions
- Header-based include (`POPCORN-INCLUDE`) — implement in this spike or defer?
- Schema/OpenAPI generation for include-aware endpoints — not in this spike.
- Client libraries (TS/JS) — out of scope for .NET spike, but protocol decisions here constrain them.

## Recent Activity (branch commits, most recent first)
- `a1c6078` Add JSG008 diagnostic + v7→v8 migration guide; promote deferred items to roadmap
- `8d6017a` Add user-facing Performance page + README summary
- `373f387` Add raw benchmark logs for each optimization step
- `03ff6a5` Three generator-level serialization optimizations (LINQ→for, hoist flags, elide HashSet)
- `9168c4c` Add legacy PopcornNetStandard to 3-way benchmark baseline
- `6f61238` Commit v2 baseline benchmark — Stj vs Popcorn serialization
- `c2f99e6` Ship [SubPropertyDefault] — Tier-1 complete
- `4aa9174` Fix enum + inherited-member bugs; add v2 API plan and TDD suite
- `f124e3a` Full benchmarks
- `50e1aa7` Working basic benchmark

## Immediate Next Steps (suggested, not committed)
1. Decide Tier-2 scope for the v2.0 merge bar (`[Translator]` / `IPopcornBlindHandler`).
2. CI job for AOT example + NuGet packaging story.
3. Consider header-based include decision (`POPCORN-INCLUDE`).

## Non-Goals for This Spike
- Deserialization / two-way serialization.
- Multi-platform providers (PHP, JS client).
- Schema generation.
- Rewriting the legacy projects — they stay available until the spike merges.
