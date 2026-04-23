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

Tier-2 (SHOULD ship with v2.0): `[Translator]` methods with DI, `IPopcornBlindHandler<TFrom,TTo>`, `[ExpandFrom]`.

Tier-3 (defer/drop): factories (moot until deserialization), deserialization (out of scope), legacy `Dictionary<string,object>` context (dropped — superseded by DI).

Already supported by construction: lazy loading, blind expansion of user-declared types, optional-property include prefix.

Genuine non-starter under AOT: polymorphic unknown-at-build-time types (trimmer removes the metadata). Document the requirement, emit a generator diagnostic.

## Known Issues
- *No open parser / dictionary / nullability bugs.* Recent fixes: (1) dictionary complex-value include passthrough (value.PropertyReferences now passed verbatim to dictionary value types). (2) Four-bug nullability cleanup — NRT-annotation normalization at every `Pop<T>` callsite, primitive-at-root no longer cross-contaminates `allTypeNames`, `IDictionary<K,V>` / `ReadOnlyDictionary<K,V>` target-type dispatch works (was failing due to whitespace mismatch in a constant), `RegisterConverters.g.cs` has `#nullable enable`. Generated-code warning count dropped from 64 CS86xx to 0.

## Deferred quality items (low severity, documented for future maintainers)
- **Pragma scope in generated converter files is slightly broad.** Each converter emits `#pragma warning disable CS8619, CS8600, CS8601, CS8625` at file scope. CS8619 / CS8625 are load-bearing (NRT-cast through generated code). CS8600 / CS8601 are pulled in defensively to cover element-assignment cases, but could theoretically mask a genuine null bug introduced by a future generator change. Scope is generated-code only; generator-code warnings still fire.
- **User-defined non-generic subclasses of Dictionary/IDictionary will crash the generator.** `class SettingsDict : Dictionary<string, string> {}` has `TypeArguments.Length == 0`, so `namedDictionaryTypeNonNullable.TypeArguments[1]` in `GenerateJsonConverter`'s dictionary branch will `IndexOutOfRangeException`. Pre-existing latent bug (pre-fix the branch was dead via whitespace-mismatch; post-fix the branch is live but still assumes target-type's own TypeArguments rather than walking the IDictionary interface). No test currently hits it. If we ever touch dict dispatch, walk the interface chain for `IDictionary<K, V>`'s TypeArguments instead.
- **`IsBlindSerializableType` uses stringly-typed hashset lookups.** Matches the pre-existing convention used by the rest of the generator (`NumberTypes`, `StringTypes`, `BoolTypes`, `IgnoreTypes` all compared via `ToDisplayString().Replace("?", "")`). Fragile to Roslyn display-format changes but consistent with the existing codebase. A future cleanup would replace all such string lookups with `SpecialType` / `ITypeSymbol` identity comparisons; not worth doing piecemeal.
- **Cycle-safety analyzer is conservative on non-blind unregistered types.** `IsNamedTypeCycleSafe` treats any type NOT in `allTypeNames` as cycle-safe (returns early before DFS). That's correct for blind types (primitives/enums/ignored) but also applies to user types that Popcorn doesn't walk (e.g. an external library type used as a property value without its own `[JsonSerializable]` registration). Those fall through to `JsonSerializer.Serialize` which doesn't touch Popcorn's HashSet, so the classification is safe. If a future change starts recursing through such types (e.g. adding `IPopcornBlindHandler`), the analyzer must be revisited so those paths either participate in the DFS or explicitly force cycle-risky.
- **Leftover potential for bigger perf wins.** `opt-iterations/README.md` enumerates three remaining levers: pre-encoded `JsonEncodedText` property names (biggest), hashtable-keyed include-match lookup (marginal), and skipping the per-property include scan when `useAll` is set and no negations are present (moderate). Not attempted in this pass.

## Open Questions
- Header-based include (`POPCORN-INCLUDE`) — implement in this spike or defer?
- Schema/OpenAPI generation for include-aware endpoints — not in this spike.
- Client libraries (TS/JS) — out of scope for .NET spike, but protocol decisions here constrain them.

## Recent Activity (branch commits, most recent first)
- `373f387` Add raw benchmark logs for each optimization step
- `03ff6a5` Three generator-level serialization optimizations (LINQ→for, hoist flags, elide HashSet)
- `9168c4c` Add legacy PopcornNetStandard to 3-way benchmark baseline
- `6f61238` Commit v2 baseline benchmark — Stj vs Popcorn serialization
- `c2f99e6` Ship [SubPropertyDefault] — Tier-1 complete
- `4aa9174` Fix enum + inherited-member bugs; add v2 API plan and TDD suite
- `f124e3a` Full benchmarks
- `50e1aa7` Working basic benchmark

## Immediate Next Steps (suggested, not committed)
1. Decide Tier-2 scope for the v2.0 merge bar (`[Translator]` / `IPopcornBlindHandler` / `[ExpandFrom]`).
2. CI job for AOT example + NuGet packaging story.
3. Consider header-based include decision (`POPCORN-INCLUDE`).

## Non-Goals for This Spike
- Deserialization / two-way serialization.
- Multi-platform providers (PHP, JS client).
- Schema generation.
- Rewriting the legacy projects — they stay available until the spike merges.
