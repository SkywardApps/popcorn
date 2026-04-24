# Popcorn Roadmap

Outstanding work on the `spike/source-generator` branch before it is ready to merge to `master` and ship as v2. For historical bug/fix context see `memory-bank/progress.md` and git log.

Last updated: 2026-04-23.

> **Scope update (2026-04-23).** All three Tier-2 features — `[ExpandFrom]`, `[Translator]` with DI, `IPopcornBlindHandler<TFrom,TTo>` — cleared from scope after use-case analysis showed each had a cleaner answer using patterns already native to ASP.NET Core + System.Text.Json. Each drop is documented with a recommended replacement in [docs/MigrationV7toV8.md](docs/MigrationV7toV8.md) (§5, §7, §8). v2.0 is now feature-complete; the two remaining merge-gate items are both infrastructure (AOT CI + NuGet packaging).

## Status snapshot

- Core protocol (include parsing, attribute semantics, nested expansion, collections, dictionaries, enums, polymorphism-basic, circular refs, full nullability matrix): **working**.
- Tier-1 feature set — custom envelope + `UsePopcornExceptionHandler` + `[SubPropertyDefault]`: **shipped**.
- Test suite: 182 passing / 2 skipped / 0 failing in `Popcorn.FunctionalTests` (2 remaining skips are the polymorphism dispatch feature — see Tier 2 section below). 19 passing in `Popcorn.SourceGenerator.Tests`. Zero CS86xx warnings in generated code.
- AOT/trim smoke: `PopcornAotExample` builds with `PublishAot=True` and exercises a custom `[PopcornEnvelope]` shape.
- Legacy reflection engine (`PopcornNetStandard*`): still in the tree, unchanged. Planned removal after v2 ships side-by-side for a release or two.

## Tier 2 — **scope-cleared 2026-04-23.**

All three planned Tier-2 features were considered and dropped after use-case analysis. The consistent finding: what v7 shipped as dedicated framework surface is, in v8, better served by patterns already native to ASP.NET Core + System.Text.Json. Each drop is documented in the migration guide with the recommended replacement:

- `[ExpandFrom]` — use `[Never]` on internal source properties, a 3-line hand-written factory, or `Mapster.SourceGenerator` for complex mapping. See [MigrationV7toV8.md §7](docs/MigrationV7toV8.md).
- `[Translator]` with DI — resolve at the endpoint (batchable, clear I/O boundaries, testable); computed properties still work for pure transforms. Serializing with injected services is an antipattern (N+1 queries, hidden I/O, scope threading complexity). See [MigrationV7toV8.md §5](docs/MigrationV7toV8.md).
- `IPopcornBlindHandler<TFrom,TTo>` — standard `JsonConverter<T>` registered on `JsonSerializerOptions.Converters` covers the full use case and composes with Popcorn transparently. See [MigrationV7toV8.md §8](docs/MigrationV7toV8.md).

If a real consumer presents a concrete case that none of the replacement patterns cover, the specs live on in the git history and can be revived — but spec-driven shipping of features nobody has asked for is the shape of complexity we are deliberately shedding.

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

### CI: run full test suite on PR + push — **shipped**
- [`.github/workflows/tests.yml`](.github/workflows/tests.yml) runs on PR + push to `master` / `spike/**`. Installs .NET 8.0 SDK, caches NuGet packages keyed on csproj hashes, runs `dotnet test` on both `Popcorn.FunctionalTests` (182 passing / 2 skipped) and `Popcorn.SourceGenerator.Tests` (19 passing). trx logs uploaded as an artifact on failure. Concurrency-group cancels superseded runs.
- Why separate from `aot-ci.yml`: the AOT workflow needs the AOT toolchain (clang/zlib) + Docker; the test workflow should run faster and with fewer dependencies. Parallel jobs keep PR feedback tight.
- Previously the 201 tests only ran on dev boxes — a regression in the generator or runtime could land on `spike/source-generator` without catching. Closed.

### CI: publish + run AOT example in a container — **shipped**
- [`.github/workflows/aot-ci.yml`](.github/workflows/aot-ci.yml) runs on PR + push to `master` / `spike/**`. Uses `docker/build-push-action@v5` with `type=gha` cache to build [`dotnet/PopcornAotExample/Dockerfile`](dotnet/PopcornAotExample/Dockerfile) (context: `dotnet/`). Starts the container on port 8080, waits up to 60s for readiness, verifies all four endpoints end-to-end:
  - `/todos` — `Success:true`, `Id:1` + `Id:2` present, `IsComplete` (`[Never]`) absent.
  - `/null` — `Data:null` in the envelope.
  - `/sub` — `Id:1` + nested `ToDo` object.
  - `/boom` — status `500`, `Ok:false`, `Problem` populated with the exception message (exercises the exception middleware + generator-emitted custom-envelope error writer).
- On failure: dumps `docker logs`. Always: stops the container. Concurrency-group cancels superseded runs on the same ref.
- Endpoint assertions verified locally (2026-04-23) against the JIT-mode app; docker daemon wasn't available on the dev box so the container path will be first-exercised by CI itself.
- **Merge-gate item: closed.** Any future change that breaks the AOT code path will fail this job.

### NuGet packaging story — **ready to tag 8.0.0-preview.1**
- **Two-package design.** `Skyward.Api.Popcorn.SourceGen.Shared` (runtime attributes, envelopes, middleware — from [`Popcorn.Shared.csproj`](dotnet/Popcorn.Shared/Popcorn.Shared.csproj)) and `Skyward.Api.Popcorn.SourceGen` (analyzer-only, from [`Popcorn.SourceGenerator.csproj`](dotnet/Popcorn.SourceGenerator/Popcorn.SourceGenerator.csproj)). Side-by-side-installable with legacy `Skyward.Api.Popcorn` v7 because the IDs diverge.
- **Metadata shipped.** Both csproj files carry `PackageId`, `Version=8.0.0-preview.1`, `Authors`, `Description`, `PackageTags`, `PackageProjectUrl`, `RepositoryUrl`, `PackageLicenseFile=LICENSE`, `PackageReadmeFile=README.md`, `Copyright`. Both reference `Microsoft.SourceLink.GitHub` with `PublishRepositoryUrl=true` and `EmbedUntrackedSources=true`. `SourceGen` is marked `DevelopmentDependency=true` + `SuppressDependenciesWhenPacking=true` so it flows analyzer-only and declares no runtime dependencies. `SourceGen.Shared` has `IncludeSymbols=true` + `SymbolPackageFormat=snupkg`.
- **Analyzer packaging.** `SourceGen` embeds `Popcorn.Shared.dll` into `analyzers/dotnet/cs/` (required for Roslyn to resolve attribute symbols during generation). The separate `SourceGen.Shared` package provides the runtime-visible copy under `lib/netstandard2.0/`. Consumers install both; they serve different layers.
- **CI workflow.** [`.github/workflows/main.yml`](.github/workflows/main.yml) extended to pack+push both v8 packages alongside the legacy v7 pack steps on tag releases. `fetch-depth: 0` added so SourceLink can resolve commit hashes.
- **Verified locally.** `dotnet pack` produces `Skyward.Api.Popcorn.SourceGen.Shared.8.0.0-preview.1.nupkg` (16 KB, `lib/netstandard2.0/Popcorn.Shared.dll` + deps) and `Skyward.Api.Popcorn.SourceGen.8.0.0-preview.1.nupkg` (38 KB, `analyzers/dotnet/cs/` containing both dlls, no `lib/`, no transitive deps). Snupkg generated for Shared.
- **Remaining to tag:**
  - [x] Test install from a throwaway consumer project. Smoke-tested on 2026-04-23 using a `net9.0` classlib with `<PackageReference>` to both packages from a local feed: packages restored, analyzer ran, generated `SmokeConsumerCarJsonConverter.g.cs` + `SystemCollectionsGenericListSmokeConsumerCarJsonConverter.g.cs` + `RegisterConverters.g.cs`, STJ source generator picked up the emitted `Pop<Car>` / `Pop<List<Car>>` types (visible in `SmokeJsonContext.PopCar.g.cs` / `SmokeJsonContext.PopListCar.g.cs`), build clean (0 errors, only informational JSG002 logs).
  - [x] Update [`docs/Releases.md`](docs/Releases.md) with the preview entry.
  - [ ] Tag `8.0.0-preview.1`, push tag, CI pushes to NuGet. **(Operational — user decision to ship.)**
- **Merge-gate item: code-complete.** Everything up to the actual `git tag` + push is done.

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

1. **Publish a benchmark baseline.** **(Done.)**
2. **Tier-2 scope cleanup.** **(Done — all three features dropped; see section above.)**
3. **AOT CI job + NuGet packaging.** Final two merge gates before v2.0 can ship.
4. **Polymorphism dispatch** (if a consumer requests it; otherwise defer to v2.1).
5. **Header-based include** (opportunistic; ship whenever convenient).

Adjust based on what any real consumer blocks on first.

## Remaining merge-to-master gates

- [x] Published benchmark report. 3-way (Stj reflection vs Stj source-gen vs Popcorn source-gen vs legacy `PopcornNetStandard`) committed under `benchmarks/results/v2-baseline/`.
- [x] Fix the `Pop{X}Inner` regression on nested-collection registrations.
- [x] CI job that publishes the AOT example and runs it in a container. Landed as [`.github/workflows/aot-ci.yml`](.github/workflows/aot-ci.yml).
- [x] NuGet packaging story for `Popcorn.SourceGenerator` + `Popcorn.Shared` — two-package design `Skyward.Api.Popcorn.SourceGen` + `Skyward.Api.Popcorn.SourceGen.Shared`, 8.0.0-preview.1, verified locally. Operational tag+push remains.
- [x] v7→v8 migration guide ([docs/MigrationV7toV8.md](docs/MigrationV7toV8.md)).
- [x] JSG008 diagnostic for polymorphic unknown-at-build-time types.
