# Popcorn Roadmap

Living document for work left on the `spike/source-generator` branch before it is ready to merge to `master` and ship as v2.

Last updated: 2026-04-23.

## Status snapshot

- Core protocol (include parsing, attribute semantics, nested expansion, collections, dictionaries, enums, polymorphism-basic, circular refs): **working**.
- Custom response envelope + `UsePopcornExceptionHandler` exception middleware: **shipped**.
- Test suite: 151 passing / 17 skipped / 0 failing in `Popcorn.FunctionalTests`. 6 passing in `Popcorn.SourceGenerator.Tests`.
- AOT/trim smoke: `PopcornAotExample` builds with `PublishAot=True`; now exercises a custom `[PopcornEnvelope]` shape.
- Legacy reflection engine (`PopcornNetStandard*`): still in the tree, unchanged. Planned removal after v2 ships side-by-side for a release or two.

## Tier 1 — blocks v2.0 merge

### `[SubPropertyDefault]` attribute
- **Why**: last Tier-1 feature not yet implemented. Common include ergonomic — "when this property is included without sub-includes, use this include list as its default".
- **Test ledger**: 4 skipped in [`SubPropertyDefaultTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/SubPropertyDefaultTests.cs).
- **Shape**: `[SubPropertyDefault("[Make,Model]")] public List<Car> Vehicles`. Parser already handles the grammar (reuses existing `PropertyReference.ParseIncludeStatement`). Generator change: read the attribute argument at emit time and inject it as the default include list when the caller's include has no children for this property.
- **Scope**: small. 1–2 days. No runtime types, no DI, no middleware.

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

### `[ExpandFrom]` projection attribute
- **Why**: Projection classes that copy a subset of fields from a source type — `[ExpandFrom(typeof(CarSource))] public class CarProjection { ... }`. Generator emits a `CarProjection.From(CarSource)` static method.
- **Test ledger**: 4 skipped in [`ExpandFromTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/ExpandFromTests.cs).
- **Generator work**: purely code-gen; emit a static `From(TSource)` method on the projection type that copies properties whose names match. Respect inheritance. No runtime wiring.
- **Scope**: small–medium. No DI, no middleware.

## Tier 3 — v2.x or drop

- **Factories** — moot until deserialization ships. `[Factory]`-tagged static method for instantiating types during read.
- **Deserialization** — out of scope for v2.0. Generator currently emits write-only converters.
- **Legacy `Dictionary<string, object>` contexts** — dropped (superseded by DI).

## Known bugs (in-scope)

*No open bugs in this list.*

The previous entry blaming `PropertyReference.ParseIncludeStatement` for dictionary-value misbehavior was a mis-diagnosis. The parser was fine; the real bug was in [`ExpanderGenerator.CreateDictionarySerializer`](dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs), which took `firstRef.Children` instead of `value.PropertyReferences` when descending into a dictionary's value type. Siblings were dropped and the `PropertyReference.Default` placeholder was mistaken for a real include list — so `?include=[Dict[Id,Name]]` silently fell back to `!default` and looked fine against weak assertions. Fixed by passing `value.PropertyReferences` through verbatim, covered by four new tests in `DictionaryTypesTests.cs` (explicit subset, wildcard, negation, and nested-dictionary propagation). Parser test un-skipped and strengthened.

## Polymorphism — partial (2 skipped)

- **Test ledger**: 2 skipped in [`PolymorphismTests.cs`](dotnet/Tests/Popcorn.FunctionalTests/PolymorphismTests.cs).
  - `PolymorphicCollection_EmitsDiscriminator_WhenConfigured`: abstract/interface base + registered derived types via `[JsonDerivedType]`; generator needs to emit per-item type-dispatch.
  - The "unknown at build time" case is documented as the only genuine AOT non-starter (trimmer strips the metadata). Ship a clear generator diagnostic for that case rather than trying to support it.
- **Scope**: medium, mostly generator-side. Deferred to Tier-2-after-Translator unless a consumer specifically blocks on it.

## Non-functional / infrastructure

### Published benchmark report
- Run the BenchmarkDotNet suite in `dotnet/benchmarks/SerializationPerformance` and produce a report comparing:
  - Stock `System.Text.Json` with generated metadata.
  - Popcorn (source generator) with various include strategies.
  - Legacy `PopcornNetStandard` (reflection).
- Commit the report as `benchmarks/results/v2-baseline.md` (or similar).
- **Why**: merge gate item — "perf parity or better" is a load-bearing thesis claim.

### CI: publish + run AOT example in a container
- Add a GitHub Actions job to `.github/workflows/main.yml` that runs `dotnet publish dotnet/PopcornAotExample -c Release -r linux-x64` with `PublishAot=True`, builds a container, runs it, and hits the three existing endpoints (`/todos`, `/null`, `/sub`) plus the new `/boom` endpoint (which should return a 500 with the custom envelope shape).
- **Why**: merge gate item — prevents regressions in the AOT code path between PRs.

### NuGet packaging story
- Decide the v2 package IDs. Proposed: `Skyward.Api.Popcorn.SourceGen` + `Skyward.Api.Popcorn.SourceGen.Shared` (or similar). Must be side-by-side-installable with the legacy `Skyward.Api.Popcorn` package during transition.
- Update `Popcorn.SourceGenerator.csproj` packaging: analyzer + Popcorn.Shared dll in `analyzers/dotnet/cs/`, Popcorn.Shared dll in `lib/netstandard2.0/` (already configured).
- Tag a preview release (e.g. `2.0.0-preview.1`) and test install from a throwaway consumer project.
- **Why**: merge gate item — nothing ships without a publishable package.

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

1. **Ship `[SubPropertyDefault]`.** Closes Tier-1.
2. **Publish a benchmark baseline.** Uses the current generator; doesn't depend on any Tier-2 feature. Makes the perf claim verifiable.
3. **Ship `[ExpandFrom]`.** Pure code-gen, no runtime wiring — the lowest-risk Tier-2.
4. **Ship `[Translator]` with DI, then `IPopcornBlindHandler<TFrom,TTo>`.** These share the "generator emits DI resolution" infrastructure; doing them together reduces duplication.
5. **AOT CI job + NuGet preview.** Once the feature set is stable.
6. **Polymorphism dispatch** (if a consumer requests it; otherwise defer to v2.1).
7. **Header-based include** (opportunistic; ship whenever convenient).

Adjust based on what any real consumer blocks on first.

## Merge-to-master gates

- [x] Decision on in-scope vs. deferred legacy features.
- [x] Custom envelope + exception middleware (Tier-1).
- [x] Dictionary complex-value include passthrough fix.
- [ ] `[SubPropertyDefault]` (Tier-1 remaining).
- [ ] Published benchmark report (legacy reflection vs source-gen vs raw `System.Text.Json`).
- [ ] CI job that publishes the AOT example and runs it in a container.
- [ ] NuGet packaging story for `Popcorn.SourceGenerator` + `Popcorn.Shared`.
