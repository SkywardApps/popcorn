# Progress: Popcorn `spike/source-generator`

## What Exists and Works

### Source generator (`Popcorn.SourceGenerator`)
- `IIncrementalGenerator`-based pipeline.
- Discovers `JsonSerializerContext` subclasses and filters `[JsonSerializable(typeof(ApiResponse<T>))]` attrs.
- Walks referenced types transitively (named types, arrays, `IEnumerable<T>`, `IDictionary<K,V>`).
- Emits one `JsonConverter<T>` per reachable type + a `PopcornJsonOptionsExtension.AddPopcornOptions()` extension that registers them and sets `NumberHandling.AllowNamedFloatingPointLiterals`.
- Handles `[Always]` / `[Default]` / `[Never]` attributes, `JsonPropertyName` mapping, nullable types, nested types, collections, dictionaries.
- Runtime circular-reference guard via visited-object `HashSet` with `{"$ref":"circular"}` output.

### Runtime shared library (`Popcorn.Shared`)
- `ApiResponse<T>`, `Pop<T>` envelopes.
- `PropertyReference` include parser (stateful recursive descent over the bracketed grammar).
- `PopcornAccessor` + `IPopcornAccessor` — per-request include parsing off `HttpContext.Request.Query["include"]`.
- `AddPopcorn()` DI registration, `HttpContextExtensions.Respond<T>`.

### Functional test suite (`dotnet/Tests/Popcorn.FunctionalTests`)
- xUnit, 21 test files (after deprecation cleanup + review-fix coverage + `ExpandFromTests.cs` deletion).
- **191 tests total**: 182 passing, 0 failing, 9 skipped (TDD-pending Tier-2 features). Sorting/Pagination/Filtering/Authorizer test files were deleted as part of the v2 scope decision; their 30 skipped tests are gone. Custom envelope + exception middleware shipped: 5 more tests flipped from skipped to passing. Review-driven fixes added `EnvelopeFixesTests.cs` with 9 new passing tests. Dictionary complex-value passthrough bug fixed + 4 new tests. Nullability coverage model (`NullabilityCoverageModel` + `NullabilityCoverageTests.cs`) added: 26 tests spanning every (type-kind × position × container-nullability × element-nullability) cell, all passing after the four-bug nullability fix landed. `[SubPropertyDefault]` shipped (Tier-1 complete): 4 more tests flipped from skipped to passing.
- Covers attribute semantics, include-parameter variations, primitives, value types, basic + advanced collections, dictionaries, nesting, conflicting attributes, default-behavior rules, enums, `JsonPropertyName`, inheritance, generics, include-parser edge cases, error handling, middleware integration, computed-property translators, custom envelope shape + exception wrapping.
- Single `TestJsonContext` drives the generator over 30+ `ApiResponse<Model>` and `MyTestEnvelope<Model>` declarations.
- Generated sources visible at `$(BaseIntermediateOutputPath)Generated` for debugging.
- `TestServerHelper` reduces per-test boilerplate to ~5 lines.

### Bugs surfaced by new test coverage
1. **Enum serialization — FIXED.** Generator was treating enum types as POCOs in `GetReferencedTypes`, registering them in `allTypeNames`, which caused `AddMemberSerializationCode` to recurse through a broken `PopEnumType` converter that emitted `{}`. Fix: added an early `continue` in `GetReferencedTypes` for `TypeKind.Enum` and `Nullable<Enum>`, so enum members fall through to the default `JsonSerializer.Serialize(writer, value, options)` path. This path honors global `JsonStringEnumConverter` registrations and per-type `[JsonConverter(typeof(JsonStringEnumConverter))]` attributes transparently — no Popcorn-specific API needed. Covered by `EnumTests` (10 tests: numeric default, nullable, flags, in-collection, global string converter, camelCase naming policy, per-type attribute).
2. **Inheritance: base-class attributes ignored on derived types — FIXED.** `[Default]` / `[Always]` on a base class were dropped when serializing a derived runtime type because `INamedTypeSymbol.GetMembers()` only returns members declared directly on the type. Fix: introduced `GetSerializableProperties` / `GetSerializableFields` helpers that walk derived → base up to `System.Object`, dedupe by name (so `new`/`override` shadows resolve to the derived declaration), and reused them at every member-enumeration site in the generator (`GetReferencedTypes` + `CreateComplexObjectSerialization`). Covered by `PolymorphismTests.DerivedType_InheritedAttributesApply` and `DerivedType_SerializedAsBaseType_EmitsBaseProperties`.
3. **Dictionary complex-value include passthrough — FIXED.** `CreateDictionarySerializer` was reading `firstRef.Children` on the first sibling of `value.PropertyReferences` to decide what include list to pass to each dictionary value. The parser eagerly sets `Children = PropertyReference.Default` on every name it parses, so the old code couldn't distinguish "no children" from "real sibling list with 1+ entries" and silently collapsed to `Default`. `?include=[Dict[Id,Name]]` therefore rendered each value with `Default` rules, not `[Id, Name]`. Fix: pass `value.PropertyReferences` through verbatim. Covered by four new tests in `DictionaryTypesTests.cs` (explicit subset, wildcard, negation, nested-dictionary propagation) that all fail against the old code. The skipped parser test in `IncludeParserEdgeTests.cs` was a false alarm (parser is correct) — un-skipped and its assertions strengthened to walk the full tree.
4. **Nullability: `Pop<T?>` vs `Pop<T>` signature mismatch — FIXED.** The generator's converter-registration pipeline canonicalized type names via `NameType` (strips NRT `?`), but the actual callsites used raw `ToDisplayString()` which preserved NRT annotations. Result: 62 CS8620 warnings at consumer-build time on properties like `List<int>?` or `List<string?>` where the emitted `Pop<List<int>?>` didn't match the registered signature `Pop<List<int>>`. Fix: route every `Pop<T>` type-argument emission through a new `TypeNameForPop(ITypeSymbol)` helper that uses a `SymbolDisplayFormat` without `IncludeNullableReferenceTypeModifier`. NRT `?` on reference types vanishes; `Nullable<T>` on value types is preserved (a CLR-distinct type). Covered by `NullabilityDiagnosticsTests` (driver-level regression guard) + `NullabilityCoverageTests` (runtime invariants).
5. **Nullability: root-level primitive registration cross-contamination — FIXED (Bug 4).** Registering `[JsonSerializable(typeof(ApiResponse<int?>))]` (or `string`, `List<int?>`, `Dictionary<string,int?>`) added the primitive type name to the generator's `allTypeNames` set without emitting a `Pop<primitive>` body. Every downstream list/dict/array converter that checks `allTypeNames.Contains(...)` then decided "yes, I should recurse via Pop" and emitted a call to a method that never existed (`PopSystemInt32`, `PopSystemString`, …) — **one primitive registration cascaded into compile errors across unrelated converters**. Fix: (a) introduced `IsBlindSerializableType` that covers primitives/enums/ignored types (and their `Nullable<T>` wrappers); (b) filter those from `allTypeNames` at construction; (c) emit a direct `JsonSerializer.Serialize` path when the target type is itself blind-serializable. Exercised by new root-level tests: `ApiResponse<int?>`, `<NullStruct?>`, `<string?>`, `<List<int?>>`, `<Dictionary<string,int?>>`.
6. **Nullability: `IDictionary<K,V>` / `ReadOnlyDictionary<K,V>` broken iterator — FIXED (Bug 5).** The `IDictionaryTypeName` constant was `"System.Collections.Generic.IDictionary<TKey,TValue>"` (no space), but Roslyn's `ToDisplayString()` emits the generic arg list with `", "` (comma + space). `InheritsOrImplements` compared by exact string, so no type ever matched the IDictionary check. For `Dictionary<K,V>` the downstream Dictionary-specific fallback at line 687 picked it up anyway, but `IDictionary<K,V>` and `ReadOnlyDictionary<K,V>` as *target* types fell through to the IEnumerable branch — which iterated `KeyValuePair<K,V>` while treating `TypeArguments[0]` (the key type) as the item type, producing CS0029 "Cannot implicitly convert KeyValuePair to K". Fix: one-character edit to the constant. Exposed by the same root-level dictionary registrations that exposed Bug 4.
7. **Nullability: `RegisterConverters.g.cs` CS8669 — FIXED (Bug 6).** The emitted file referenced `JsonNamingPolicy? namingPolicy` but had no `#nullable enable` directive at its top, producing CS8669 "annotation for nullable reference types should only be used in code within a '#nullable' annotations context". Fix: prepend `#nullable enable` to the emitted source.

### Established Contract: `?include=` uses wire names only
The include list is part of the API contract — it uses the wire name (from `[JsonPropertyName]` if present, otherwise the C# name). The C# name is an implementation detail the client has no visibility into. A client sees `{"display_name":...}` in responses and must request `?include=[display_name]`; `?include=[DisplayName]` is treated as an unknown name and the property is not emitted. This matches how the generator currently behaves and is the correct design. Tests in `JsonPropertyNameTests.cs` assert this contract explicitly (both positive: `IncludeMatchesWireName`, and negative: `IncludeByCSharpName_DoesNotMatch`).

### TDD-pending test ledger (9 skipped)
Every in-scope planned feature from `apiDesign.md` has corresponding skipped tests. When implementation lands, remove the `Skip=` attribute and the test becomes active. Files:
- `CustomEnvelopeTests.cs` — **0 skipped, 4 passing** (Tier-1 SHIPPED).
- `ErrorHandlingTests.cs` — **0 skipped, 6 passing** (includes new `SerializationException_ProducesErrorEnvelope`).
- `SubPropertyDefaultTests.cs` — **0 skipped, 4 passing** (Tier-1 SHIPPED).
- `TranslatorTests.cs` (3 skipped, 3 passing for computed properties), `BlindHandlerTests.cs` (4) — Tier-2.
- `PolymorphismTests.cs` (2 skipped). `IncludeParserEdgeTests.cs` previously had 1 skipped for an alleged parser bug — un-skipped after the dictionary-value bug was traced to the generator instead.

**Deleted test files (v2 scope decision):** `SortingTests.cs`, `PaginationTests.cs`, `FilteringTests.cs`, `AuthorizerTests.cs`, `ExpandFromTests.cs`. Corresponding model files (`SortingModel.cs`, `AuthorizationModel.cs`) deleted. See `migrationAnalysis.md` for the scope rationale. `ExpandFromTests.cs` was dropped 2026-04-23 when `[ExpandFrom]` was cut from v2 — the replacement recommendation lives in `docs/MigrationV7toV8.md` §7.

### AOT smoke test (`PopcornAotExample`)
- `WebApplication.CreateSlimBuilder`, `PublishAot=True`, `PublishTrimmed=True`, Dockerfile.
- Three endpoints exercising nullable, nested, and null-response shapes.
- Validates that generated converters pass the AOT analyzer.

### Benchmarks (`dotnet/benchmarks/`)
- `SerializationPerformance` — BenchmarkDotNet suite: serialization comparison (stock vs Popcorn source-gen vs legacy `PopcornNetStandard`), include strategies, scalability (flat + deep), circular-ref overhead, attribute processing. 7 test models with seeded `TestDataGenerator`. `MemoryDiagnoser` enabled. Command-line selector (`comparison` | `includes` | `scalability` | `circular` | `attributes` | `all`). Legacy 3-way setup lives inside `SerializationComparisonBenchmarks.Setup()` — configures `PopcornFactory` programmatically to mirror the new models' attribute semantics.
- `ParsingIncludes` — include-string parser micro-benchmarks.

### Generator performance optimizations (three incremental changes)
Three build-time optimizations landed as a bundled commit (`03ff6a5`) after the 3-way baseline was captured. Each was isolated, tested against the full functional suite, and benchmarked before the next was applied. Walk-through + raw per-step BDN logs in [`benchmarks/results/v2-baseline/opt-iterations/`](../benchmarks/results/v2-baseline/opt-iterations/README.md).
1. **LINQ `.Any()` / `.FirstOrDefault()` → index-based for-loops** in the emitted converter body. No per-call delegate dispatch or iterator machinery. Biggest time win on complex objects (ComplexModel_PopcornAll −29%).
2. **Hoist `useAll` / `useDefault` / `naming` setup into list/dict callers** via a new `Pop{X}Inner` overload that accepts pre-computed flags. List converters call `Inner` per item after computing once before the foreach. Single-member callsites (nested properties with differing `propertyReference.Children`) keep using the 4-arg `Pop{X}` wrapper. Eliminates the per-item `Func<string, string>` naming-closure allocation that dominated the list-scenario allocation delta (−10% to −22% across list scenarios).
3. **Elide `HashSet<object>` allocation for cycle-safe type graphs.** `IsConverterCycleSafe` DFS classifies every converter; cycle-safe converters (SimpleModel, ScalableModel, any list/dict/nullable wrapper of cycle-safe types) pass `null` at entry. Cycle-risky types (ComplexNestedModel with `Child`, CircularReferenceModel, DeepNestingModel) allocate as before. Body ops are null-conditional to compose both cases.

Cumulative effect on the 3-way baseline (DefaultJob, `spike/source-generator` commit `373f387`):
- Popcorn vs STJ-reflection: `ComplexModelList_PopcornAll` is now **0.87× time / 0.93× alloc** (was 0.97×/1.03× pre-optimization) — Popcorn is *faster* than STJ when emitting everything on nested data.
- `SimpleModelList_PopcornAll` worst case: 1.40× (was 1.80×); still not STJ-parity but ~5.8× faster than legacy `LegacyAll` on the same shape.
- `ComplexModelList_PopcornDefault` unchanged (already dominant at 0.10×).
- All 182 functional tests remain passing; cycle-safety analyzer verified by inspecting generated output (cycle-safe types pass `null`, cycle-risky types still allocate).

## What's Broken or Incomplete

### Known failing / flaky
- *None.*

### Dropped from v2 scope (explicitly not porting)
- Sorting
- Pagination
- Filtering
- Authorization / permissioning

Never used in practice with the legacy engine. Callers that need these behaviors implement them at the endpoint level with standard ASP.NET tools. See `migrationAnalysis.md` > "Scope Decision" for full rationale.

### Not yet ported (still in scope)
- `[Translator]` methods with DI, `IPopcornBlindHandler<TFrom,TTo>` (Tier-2). `[ExpandFrom]` was dropped 2026-04-23 — replacement patterns documented in `docs/MigrationV7toV8.md` §7.

### Shipped: `[SubPropertyDefault]` (Tier-1 complete)
- `SubPropertyDefaultAttribute` in `Popcorn.Shared/PopAttribute.cs` — `[AttributeUsage(Property | Field)]`, constructor takes the include string.
- Generator reads the attribute in `AddMemberSerializationCode`; when present:
  - Emits a process-level `private static readonly IReadOnlyList<PropertyReference> __SubDefault_{parentDisc}_{memberName}` field with `PropertyReference.ParseIncludeStatement(...)` so the parse happens once per process.
  - Rewrites the "fall back to `PropertyReference.Default`" expression at the two nested-`Pop<T>` callsites (complex member + complex array element) to use `ReferenceEquals(propertyReference.Children, PropertyReference.Default)` as the signal that the client gave no explicit sub-children, falling back to the attribute's list instead.
- Recursive by construction — each `Pop<T>` call threads the substituted list into the child serializer, which applies its own `SubPropertyDefault` at each level.
- `[Always]` / `[Never]` on the sub-type still win; `SubPropertyDefault` only replaces what the "default set" for that property is.
- Dictionary values handled transparently: if the outer property has `[SubPropertyDefault("[X,Y]")]`, the substituted list flows into the dictionary-value serializer via its existing `value.PropertyReferences.Any()` branch.

### Shipped in the Tier-1 envelope work
- `ApiError` record in `Popcorn.Shared` (`ApiError.cs`).
- `ApiResponse<T>` extended with `Error: ApiError?` slot + `FromError(ApiError)` factory (`ApiResponse.cs`). Parameterless ctor is `internal` to prevent construction of the "Success=true / no Data" shape.
- Marker attributes: `[PopcornEnvelope]`, `[PopcornPayload]`, `[PopcornError]`, `[PopcornSuccess]` in `Popcorn.Shared/PopAttribute.cs`.
- `PopcornOptions` class with `EnvelopeType` (default `typeof(ApiResponse<>)`) and `DefaultNamingPolicy` (honored by the exception middleware). `AddPopcorn(Action<PopcornOptions>)` is idempotent — repeated calls mutate the single options singleton.
- `PopcornErrorWriterRegistry` static registry with `Volatile` read/write; generator registers a writer via two hooks: `AddPopcornOptions()` (JSON-options-level) and `AddPopcornEnvelopes()` (DI-time, AOT-friendly). The writer signature accepts a `JsonNamingPolicy?` so custom envelope field names are policy-converted to match the success path.
- `UsePopcornExceptionHandler()` middleware in `Popcorn.Shared/ApplicationBuilderExtensions.cs` — buffers the response, strips `Content-Length` from the aborted inner handler, preserves custom response headers, applies the configured naming policy, writes a structured envelope on exception (default shape or custom via registry). XML docs document the buffering tradeoff (no streaming).
- Source generator (`ExpanderGenerator.cs`) now recognizes `[PopcornEnvelope]`-decorated types in `[JsonSerializable]` attributes alongside `ApiResponse<T>`, walks the `[PopcornPayload]` type for inner references, walks the envelope's base chain for markers, supports envelopes nested inside non-generic outer types, and emits the per-envelope error writer. Emits diagnostics JSG003–JSG007 for malformed envelopes (missing payload, duplicate markers, wrong payload/error type, generic-outer nesting).

### Supported by construction (no porting needed)
- Lazy loading (generator never touches excluded properties)
- Blind expansion of user-declared types (generator walks reachable types from `[JsonSerializable]`)

### Out of scope for this spike (explicit)
- Deserialization (generated converters are write-only).
- Header-based `POPCORN-INCLUDE` selection.
- Schema / OpenAPI generation.
- Cross-language providers (PHP, JS, TS).

## Branch Position
- Base: `master` at `dacf03d` (NuGet publish CI).
- 13 commits ahead of `master`; ~12k lines added, ~225 removed across 82 files.
- `PopcornNetStandard` and `PopcornNetStandard.WebApiCore` remain in the tree unchanged — this spike does not delete the legacy engine. Legacy packages on NuGet (`Skyward.Api.Popcorn`) continue shipping from `master`.

## Migration Thesis (validation status)
1. **Perf parity or better vs System.Text.Json AND vs legacy reflection engine** — 3-way baseline committed under `benchmarks/results/v2-baseline/` (post-optimization state). Popcorn source-gen beats legacy reflection in every scenario (3–8× faster for `All`, ~5.8× faster for `Default` on ComplexModelList). Popcorn-default is ~10× faster / ~5× less alloc than STJ on ComplexModelList; Popcorn-all is **faster** than STJ on nested data (0.87× time, 0.93× alloc on ComplexModelList) after the three generator optimizations landed. Legacy-all is 3.6× slower than STJ on the same shape — the v2 migration has no regression scenario. Benchmark project has a direct `ProjectReference` to `PopcornNetStandard`; 12 `*_Legacy_*` benchmarks run alongside the existing 20 Popcorn/STJ methods. Caveat: host rolled forward to .NET 9.0.15 for this run (was .NET 10 in the prior 2-way); numbers are internally consistent but ~20% slower absolute than the .NET 10 run.
2. **Native AOT works** — `PopcornAotExample` builds with `PublishAot=True`. End-to-end runtime validation: done locally per recent commits, no CI job yet.
3. **Trimming works** — `PublishTrimmed=True` set alongside AOT. No separate trim-only run documented.

## Merge-to-master Gates (suggested, not formalized)
- [x] Decision on in-scope vs. deferred legacy features. **Resolved:** sorting/pagination/filtering/authorizers dropped; custom envelope + `[SubPropertyDefault]` remain as Tier-1.
- [x] Custom envelope + exception middleware (Tier-1).
- [x] `[SubPropertyDefault]` (Tier-1 complete).
- [x] Dictionary/nested parser fix.
- [x] Published benchmark report comparing legacy reflection vs. source-generated vs. raw `System.Text.Json`. 3-way baseline under `benchmarks/results/v2-baseline/`.
- [ ] CI job that publishes the AOT example and runs it in a container to prove end-to-end.
- [ ] NuGet packaging story for `Popcorn.SourceGenerator` + `Popcorn.Shared`.
