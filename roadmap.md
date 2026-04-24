# Popcorn Roadmap

Outstanding work on the `spike/source-generator` branch before it is ready to merge to `master` and ship as v2. For historical bug/fix context see `memory-bank/progress.md` and git log.

Last updated: 2026-04-23.

> **Scope update (2026-04-23).** `[ExpandFrom]` dropped from Tier-2. The v7 `MapEntityFramework` pattern intercepted serialization; `[ExpandFrom]` as specced only emitted a `From(TSource)` factory, so it wasn't clean parity anyway. The three real use cases have cleaner answers: `[Never]` on the source, a hand-written factory, or `Mapster.SourceGenerator` for complex mapping. See [docs/MigrationV7toV8.md §7](docs/MigrationV7toV8.md) for the recommendation and rationale.

## Status snapshot

- Core protocol (include parsing, attribute semantics, nested expansion, collections, dictionaries, enums, polymorphism-basic, circular refs, full nullability matrix): **working**.
- Tier-1 feature set — custom envelope + `UsePopcornExceptionHandler` + `[SubPropertyDefault]`: **shipped**.
- Test suite: 182 passing / 9 skipped / 0 failing in `Popcorn.FunctionalTests`. 19 passing in `Popcorn.SourceGenerator.Tests`. Zero CS86xx warnings in generated code.
- AOT/trim smoke: `PopcornAotExample` builds with `PublishAot=True` and exercises a custom `[PopcornEnvelope]` shape.
- Legacy reflection engine (`PopcornNetStandard*`): still in the tree, unchanged. Planned removal after v2 ships side-by-side for a release or two.

## Tier 2 — should ship with v2.0 or soon after

### `[Translator]` methods with DI
- **Why**: Computed properties already work (3 passing tests in `TranslatorTests.cs`). The remaining gap is translator methods that need injected services — e.g. `[Translator(nameof(Owner))] public static EmployeeRef? ResolveOwner(Car src, IEmployeeLookup lookup)`.
- **Test ledger**: 3 skipped in [`TranslatorTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/TranslatorTests.cs).
- **Generator work**: inspect method signature (first param = source type, rest = DI services); emit a call with `IServiceProvider.GetRequiredService<T>()` in the generated converter's Write path. Emit a diagnostic for invalid signatures.
- **Runtime work**: none — DI resolution is emitted inline.
- **Dependencies**: the converter must have access to `IServiceProvider`. `JsonSerializerOptions` doesn't carry one today; we may need to thread it through via `PopcornAccessor` (already scoped-per-request) and have the converter fetch it via an ambient context or via a generator-emitted wrapper.
- **Scope**: medium. 3–5 days including partial-method variant.

### `IPopcornBlindHandler<TFrom, TTo>`
- **Why**: external types like `NetTopologySuite.Geometry` that we can't decorate. The user registers a DI-resolved handler; generator sees `TFrom` during its type walk and emits a conversion call if a handler exists.
- **Test ledger**: 4 skipped in [`BlindHandlerTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/BlindHandlerTests.cs).
- **Generator work**: register known handler pairs at build time (via a generator-recognized DI marker or a static registration convention); in the type walk, when `TFrom` is encountered and a handler pair is registered, emit a DI resolve + `Convert()` call instead of a recursive converter.
- **Runtime work**: `IPopcornBlindHandler<TFrom, TTo>` interface in `Popcorn.Shared`; a `services.AddPopcornBlindHandler<TFrom, TTo>(func)` DI extension.
- **Scope**: medium. Similar complexity to `[Translator]`.

### Polymorphism — partial (2 skipped)
- **Test ledger**: 2 skipped in [`PolymorphismTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/PolymorphismTests.cs).
  - `PolymorphicCollection_EmitsDiscriminator_WhenConfigured`: abstract/interface base + registered derived types via `[JsonDerivedType]`; generator needs to emit per-item type-dispatch.
  - "Unknown at build time" case: **JSG008 diagnostic shipped** (`ExpanderGenerator.cs:PolymorphicUnknownDescriptor`). Generator now emits a warning when a member is typed `object`, abstract class, or interface. 5 tests in [`EnvelopeDiagnosticsTests.cs`](dotnet/Tests/Popcorn.SourceGenerator.Tests/EnvelopeDiagnosticsTests.cs) cover the positive and negative cases. Registered-derived support (the `[JsonDerivedType]` dispatch half) remains unimplemented.
- **Scope**: medium, mostly generator-side. Defer unless a consumer blocks on it.

## Tier 3 — v2.x or drop

- **Factories** — moot until deserialization ships. `[Factory]`-tagged static method for instantiating types during read.
- **Deserialization** — out of scope for v2.0. Generator currently emits write-only converters.
- **Legacy `Dictionary<string, object>` contexts** — dropped (superseded by DI).

## Non-functional / infrastructure

### Published benchmark report
- 3-way baseline committed: [`benchmarks/results/v2-baseline/`](benchmarks/results/v2-baseline/README.md). Covers Stj (reflection) vs Stj (source-gen) vs Popcorn (source-gen) vs legacy `PopcornNetStandard` (reflection) across SimpleModel, SimpleModelList[100], ComplexNestedModel, ComplexNestedModelList[25]. Three incremental generator optimizations landed after initial baseline capture — walk-through under [`opt-iterations/`](benchmarks/results/v2-baseline/opt-iterations/README.md).
- Headline: Popcorn source-gen beats legacy reflection in every scenario (3–8× for `All`, ~5.8× for `Default` on ComplexModelList). Popcorn-default on ComplexModelList is ~10× faster / ~5× less alloc than STJ reflection. Popcorn-all on ComplexModelList is **0.87× time / 0.93× alloc** — Popcorn is *faster* than STJ when emitting everything on nested data; legacy-all is 3.6× slower than STJ on the same shape.
- **Merge-gate item**: **closed**. "Perf parity or better" was the load-bearing thesis claim; 3-way report confirms it — and the three in-generator optimizations tipped it from parity-to-STJ into *better* than STJ on complex nested lists.

### CI: publish + run AOT example in a container
- Add a GitHub Actions job to `.github/workflows/main.yml` that runs `dotnet publish dotnet/PopcornAotExample -c Release -r linux-x64` with `PublishAot=True`, builds a container, runs it, and hits the three existing endpoints (`/todos`, `/null`, `/sub`) plus the `/boom` endpoint (which should return a 500 with the custom envelope shape).
- **Why**: merge gate item — prevents regressions in the AOT code path between PRs.

### NuGet packaging story
- Decide the v2 (v8 for the public release) package IDs. Proposed: `Skyward.Api.Popcorn.SourceGen` + `Skyward.Api.Popcorn.SourceGen.Shared` (or similar). Must be side-by-side-installable with the legacy `Skyward.Api.Popcorn` v7 package during transition.
- Update [`Popcorn.SourceGenerator.csproj`](dotnet/Popcorn.SourceGenerator/Popcorn.SourceGenerator.csproj) packaging: analyzer + Popcorn.Shared dll in `analyzers/dotnet/cs/`, Popcorn.Shared dll in `lib/netstandard2.0/` (already configured). Missing today: `PackageId`, `Version`, `Authors`, `Description`, `PackageReadmeFile`, `PackageLicenseExpression`, `RepositoryUrl`, SourceLink + `IncludeSymbols` + `SymbolPackageFormat=snupkg`.
- Extend [`.github/workflows/main.yml`](.github/workflows/main.yml) (today only publishes legacy v7) to also pack+push the v8 generator and runtime packages on tag releases.
- Tag a preview release (e.g. `8.0.0-preview.1`) and test install from a throwaway consumer project.
- **Why**: merge gate item — nothing ships without a publishable package.

### Legacy deprecation timeline + v1→v8 migration guide
- [docs/MigrationV7toV8.md](docs/MigrationV7toV8.md) shipped — covers attribute renames, dropped features (sorting/pagination/filtering/authorizers), DI replacement for `SetContext(dict)`, custom envelope + middleware for `SetInspector(lambda)`, include-parameter wire-name contract, JSG008 documentation, rollback plan.
- Decide concrete deprecation window for v7 packages: proposed "v7 remains on NuGet for at least one release after v8.0 ships; v7 gets a `<PackageReleaseNotes>` banner pointing at MigrationV7toV8.md."
- Update `Releases.md` (currently empty of v8 entries) with an `8.0.0-preview.1` entry when it cuts.

### Example projects refresh
- [`dotnet/Examples/PopcornNet5Example/`](dotnet/Examples/PopcornNet5Example/) still references the v7 reflection engine (`services.UsePopcorn((config) => config.UseDefaultConfiguration())`, `ExpandServiceFilter`) and targets `net5.0`. Either port to v8 (minimal API + `IPopcornAccessor` + `[JsonSerializable]` context) or delete and rely solely on [`dotnet/PopcornAotExample/`](dotnet/PopcornAotExample/) as the canonical example.
- **Why**: leaving a v7-shaped example next to a v8 release will confuse new adopters.

## Deferred-quality items (low severity, promoted from activeContext.md)

Confirmed against [`ExpanderGenerator.cs`](dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs) on 2026-04-23.

- **Pragma scope in generated converter files is slightly broad.** `ExpanderGenerator.cs:908` emits `#pragma warning disable CS8619, CS8600, CS8601, CS8625` at file scope. CS8619 / CS8625 are load-bearing (NRT-cast through generated code). CS8600 / CS8601 are pulled in defensively; could theoretically mask a real null bug introduced by a future generator change. Narrow to per-statement where feasible.
- **User-defined non-generic subclasses of Dictionary/IDictionary will crash the generator.** `class SettingsDict : Dictionary<string, string> {}` has `TypeArguments.Length == 0`; `ExpanderGenerator.cs:847` accesses `namedDictionaryTypeNonNullable.TypeArguments[1]` unguarded → `IndexOutOfRangeException`. No test hits it today. Fix: walk the `IDictionary<K, V>` interface chain for `TypeArguments` rather than reading the target type's own list.
- **`IsBlindSerializableType` uses stringly-typed hashset lookups.** Matches the pre-existing convention (`NumberTypes`, `StringTypes`, `BoolTypes`, `IgnoreTypes` all compared via `ToDisplayString().Replace("?", "")`). Fragile to Roslyn display-format changes but consistent. Future cleanup: replace all such lookups with `SpecialType` / `ITypeSymbol` identity comparisons.
- **Cycle-safety analyzer is conservative on non-blind unregistered types.** `IsNamedTypeCycleSafe` (`ExpanderGenerator.cs:133-164`) treats any type NOT in `allTypeNames` as cycle-safe. Correct today — unregistered user types fall through to `JsonSerializer.Serialize` which doesn't touch Popcorn's HashSet. Revisit if a future change starts recursing through such types (e.g. `IPopcornBlindHandler` landing).

## Remaining performance levers (promoted from opt-iterations/README.md)

Three generator-level optimizations considered but not taken in the 2026-04 opt pass. Listed in rough order of expected payoff.

- **Pre-encoded property names via `JsonEncodedText`.** STJ's own source-gen path does this; saves per-property UTF-16→UTF-8 encoding cost. Complicated by runtime `PropertyNamingPolicy` (the encoded form depends on options → forces per-options caching). Biggest remaining lever; would likely close most of the remaining `SimpleModelList_PopcornAll` 1.40× gap vs raw STJ.
- **Skip the per-property include-match scan when `useAll && !hasNegations`.** Every property unconditionally emits under `!all` with no negations, so the scan is pure overhead. A one-time check at the top of the body could bypass the per-property loop entirely. Moderate payoff on `Popcorn_All` scenarios.
- **Hashtable-keyed include-list lookup.** Current linear scan is O(n·m) in properties × include-list size. Marginal gain — include lists are typically small.

## Optional / open questions

### Header-based include transport (`POPCORN-INCLUDE`)
- **Why**: URLs have length limits; `GET /foo?include=[very,long,list,...]` can blow past proxy limits. An alternative header carries the same grammar, parsed by `PopcornAccessor` with header-first / query-fallback priority.
- **Design**: `PopcornAccessor.PropertyReferences` getter checks `HttpContext.Request.Headers["POPCORN-INCLUDE"]` first, falls back to query `?include=`.
- **Status**: spec'd in `memory-bank/apiDesign.md`, not started. Decision: implement in this spike, or defer to v2.1?
- **Scope**: tiny. ~1 day including tests.

### Schema / OpenAPI generation for include-aware endpoints
- Out of scope for v2.0. Note separately if a consumer requests it.

### Cross-language provider kit (JS/TS client)
- Out of scope for this .NET spike. Protocol decisions on this branch constrain any future client, but we don't block on client work.

## Suggested sequence

A defensible order that minimizes dependency chains and maximizes incremental merge-readiness:

1. **Publish a benchmark baseline.** Uses the current generator; doesn't depend on any Tier-2 feature. Makes the perf claim verifiable. **(Done)**
2. **Ship `[Translator]` with DI, then `IPopcornBlindHandler<TFrom,TTo>`.** These share the "generator emits DI resolution" infrastructure; doing them together reduces duplication.
3. **AOT CI job + NuGet preview.** Once the feature set is stable.
4. **Polymorphism dispatch** (if a consumer requests it; otherwise defer to v2.1).
5. **Header-based include** (opportunistic; ship whenever convenient).

Adjust based on what any real consumer blocks on first.

## Remaining merge-to-master gates

- [x] Published benchmark report. 3-way (Stj reflection vs Stj source-gen vs Popcorn source-gen vs legacy `PopcornNetStandard`) committed under `benchmarks/results/v2-baseline/`.
- [x] Fix the `Pop{X}Inner` regression on nested-collection registrations.
- [ ] CI job that publishes the AOT example and runs it in a container.
- [ ] NuGet packaging story for `Popcorn.SourceGenerator` + `Popcorn.Shared`.
- [x] v7→v8 migration guide ([docs/MigrationV7toV8.md](docs/MigrationV7toV8.md)).
- [x] JSG008 diagnostic for polymorphic unknown-at-build-time types.
