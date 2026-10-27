# Technical Context: Popcorn

## Stack
- **.NET 10** for runtime targets (SourceGenerator.Tests, AOT example, CI benchmarks); FunctionalTests multi-target `net8.0;net10.0`. `MatrixPerformance` stays `net8.0;net9.0;net10.0` (cross-runtime comparison, not in CI).
- **SDK**: `global.json` pins .NET SDK `10.0.100` + `rollForward: latestFeature`. Generator floor: SDK 9.0.100 / VS 17.12 (Roslyn 4.12); older compilers skip the generator with CS9057.
- **.NET Standard 2.0** for `Popcorn.Shared` and `Popcorn.SourceGenerator` (source generators *must* target netstandard2.0 per Roslyn rules).
- **Roslyn** — `Microsoft.CodeAnalysis.CSharp` 4.12.0 for the generator.
- **System.Text.Json** 9.0.1.
- **Microsoft.AspNetCore.Http.Abstractions** 2.3.0 (for `HttpContext` in netstandard2.0 land).
- **xUnit** 2.6.6 + `Microsoft.AspNetCore.TestHost` 10.0.14 for functional tests.
- **BenchmarkDotNet** for perf suite.

## Solution Layout (`dotnet/Popcorn.sln`)
```
dotnet/
├── Popcorn.sln
├── Popcorn.Shared/              # Runtime attrs, ApiResponse<T>, ApiError, Pop<T>, PropertyReference,
│                                #   PopcornAccessor, PopcornOptions, PopcornErrorWriterRegistry,
│                                #   ApplicationBuilderExtensions (UsePopcornExceptionHandler)
├── Popcorn.SourceGenerator/     # IIncrementalGenerator — ExpanderGenerator.cs, Plan.md
├── PopcornAotExample/           # AOT/trim smoke test: minimal API, CreateSlimBuilder, PublishAot=True, PublishTrimmed=True
├── PopcornNetStandard/          # LEGACY reflection-based expander (Abstractions, Expanders, Externals, Internals) — being replaced
├── PopcornNetStandard.WebApiCore/ # LEGACY middleware (ExpandResultAttribute, ExpandServiceFilter) — being replaced
├── Tests/
│   ├── Popcorn.FunctionalTests/ # xUnit tests for the source generator (22 files, 140+ tests)
│   │   ├── *Tests.cs            # PrimitiveTypes, ValueTypes, BasicCollectionTypes, CollectionEdgeCases,
│   │   │                        #   CollectionPropertyInclusion, DictionaryTypes, AlwaysAttribute,
│   │   │                        #   NestedAlwaysAttribute, CollectionAlwaysAttribute, ConflictingAttributes,
│   │   │                        #   DefaultBehavior, IncludeParameterVariation, BasicSerialization,
│   │   │                        #   Enum, JsonPropertyName, Polymorphism, Generic, IncludeParserEdge,
│   │   │                        #   ErrorHandling, Translator, SubPropertyDefault, CustomEnvelope,
│   │   │                        #   EnvelopeFixes
│   │   ├── Models/              # 20+ test model classes incl. EnvelopeModel (MyTestEnvelope, DerivedEnvelope, NestedEnvelopeContainer)
│   │   ├── TestJsonContext.cs   # JsonSerializerContext with [JsonSerializable] attrs for every test model
│   │   └── TestPlan.md
│   ├── Popcorn.SourceGenerator.Tests/ # Generator unit tests (CSharpGeneratorDriver)
│   │   ├── GeneratorTestHarness.cs    # In-memory compilation + generator-driver harness
│   │   └── EnvelopeDiagnosticsTests.cs# JSG003–JSG007 assertions + positive control
│   └── PopcornSpecTests/        # Protocol-level spec tests
├── benchmarks/
│   ├── ParsingIncludes/         # Include-string parser benchmarks
│   └── SerializationPerformance/ # End-to-end serialization benchmarks (JSON baseline vs Popcorn)
├── Examples/PopcornNet5Example/ # Older reflection-based demo
├── Build/                       # Build scripts
└── Projects.md                  # Project index
```

## Key AOT/Trim Settings (`PopcornAotExample.csproj`)
- `<PublishTrimmed>True</PublishTrimmed>`
- `<PublishAot>True</PublishAot>`
- `<InvariantGlobalization>true</InvariantGlobalization>`
- `WebApplication.CreateSlimBuilder(args)` — minimal hosting, AOT-compatible.
- `<EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>` with `<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>` — keeps the generated tree out of the csproj globs but still lets you inspect output.

## Source Generator Packaging
From `Popcorn.SourceGenerator.csproj`:
- `IsRoslynComponent=true`, `EnforceExtendedAnalyzerRules=true`.
- `IncludeBuildOutput=false` — consumers get the generator as an analyzer, not a runtime dll reference.
- Packs `Popcorn.SourceGenerator.dll` and `Popcorn.Shared.dll` under `analyzers/dotnet/cs` (so the generator sees the attribute types) AND `Popcorn.Shared.dll` under `lib/netstandard2.0` (so consumers get the attributes at runtime).

## Source Generator Entry Points (`ExpanderGenerator.cs`)
- `Initialize()` — pipeline: `ForAttributeWithMetadataName(JsonSerializableAttribute)` → filter to `JsonSerializerContext` subclasses → filter to `ApiResponse<T>` or `[PopcornEnvelope]` target types → emit converter + registrar.
- `GetJsonSerializerContextClass` — confirms base class is `System.Text.Json.Serialization.JsonSerializerContext`.
- `GetJsonSerializableTypes` — pulls `[JsonSerializable(typeof(Envelope<…>))]` attrs where the envelope is either `ApiResponse<T>` (or inherits) or decorated with `[PopcornEnvelope]`.
- `HasPopcornEnvelopeAttribute` — identifies user-declared custom envelopes.
- `AnalyzeEnvelope` — walks the envelope's base chain (via `GetSerializableProperties`) to collect `[PopcornSuccess]` / `[PopcornPayload]` / `[PopcornError]` slot names, detect duplicates, and record locations for diagnostics.
- `ReportEnvelopeDiagnostics` — emits JSG003–JSG007 for malformed envelopes.
- `InheritsOrImplements(typeSymbol, "Popcorn.Shared.ApiResponse<T>")` — used for both response filtering and for `IEnumerable<T>`/`IDictionary<K,V>` detection when walking properties.
- `IsPopOfT`, `IsApiError` — type predicates for JSG005 / JSG006 payload/error-slot validation.
- `OpenGenericCSharpName` — emits the open-generic C# syntax for `typeof(...)` (handles nested types inside non-generic outers; flags generic-outer nesting via JSG007).
- `GetReferencedTypes` — BFS over property types; handles arrays, dictionaries (walks value type), enumerables (walks item type), skips primitives/ignored types.
- `GenerateJsonConverter` → `CreateComplexObjectInnerBody` — emits the property-write body (circular guard + `WriteStartObject` + per-property emissions + `WriteEndObject`). Returns the *Inner body only; the 4-arg wrapper that computes `useAll`/`useDefault`/`naming` and delegates to `Pop{X}Inner` is emitted directly in `GenerateJsonConverter` using the `FlagSetupCode` constant. `emitInnerOverload` is the branch flag in `GenerateJsonConverter` — true for complex-object targets, false for collection/dict/blind/nullable-wrapper targets.
- `UnwrapPayloadType(ITypeSymbol)` — unwraps `Nullable<T>`, arrays, `IEnumerable<T>`, `IDictionary<K,V>` to their inner element/value type. Used by `IsConverterCycleSafe` to walk the effective payload of a converter.
- `IsConverterCycleSafe(ITypeSymbol, HashSet<string>)` / `IsNamedTypeCycleSafe(INamedTypeSymbol, HashSet<string>, HashSet<string>)` — DFS through the payload-type graph to detect whether a converter can transitively reach itself. Only types in `allTypeNames` are traversed (blind/unregistered types don't recurse through Popcorn). Cycle-safe converters pass `null` for `visitedObjects` at the entry point, saving a ~80B HashSet allocation per top-level call. Body ops are null-conditional so cycle-safe and cycle-risky converters compose correctly when a cycle-safe type is reached from a cycle-risky parent.
- Registration: emits `Popcorn.Shared.PopcornJsonOptionsExtension.AddPopcornOptions(JsonSerializerOptions)` which installs every generated converter, sets `NumberHandling = AllowNamedFloatingPointLiterals`, and (if any `[PopcornEnvelope]` types exist) registers `WriteCustomErrorEnvelope` in `PopcornErrorWriterRegistry`. Also emits `AddPopcornEnvelopes(IServiceCollection)` — the DI-time hook that registers the writer without requiring a `JsonSerializerOptions` call. Writer signature: `(Utf8JsonWriter, Type, ApiError, JsonNamingPolicy?)` — generator emits naming-policy-aware field names so error responses match success-path casing.

## Type Handling Buckets (`ExpanderGenerator.cs`)
- **Numbers**: all integer types and `decimal` (floats are in `IgnoreTypes`).
- **Strings**: `string`, `Span<char>`, `ReadOnlySpan<char>`, `Memory<char>`, `ReadOnlyMemory<char>`.
- **Bools**: `bool`, `System.Boolean`.
- **Ignored (write-as-is, don't expand)**: `char`, `float`, `double`, `Guid`, `DateTime`, `TimeSpan`, `DateTimeOffset`.
- **Collections**: dispatched via `InheritsOrImplements` for `IEnumerable<T>` / `IDictionary<K,V>`.
- **Enums and `Nullable<Enum>`**: skipped in `GetReferencedTypes` (not registered as Popcorn-aware types). Fall through to `JsonSerializer.Serialize(writer, value, options)`, which transparently picks up global `options.Converters.Add(new JsonStringEnumConverter())` registrations and per-enum `[JsonConverter(typeof(JsonStringEnumConverter))]` attributes. Default output is numeric; string-form is opt-in via standard System.Text.Json configuration — no Popcorn-specific API.

## CI / Publish
- GitHub Actions: `tests.yml`, `aot-ci.yml` (Docker, `sdk:10.0` image), `benchmarks.yml`, `main.yml` (release). SDK comes from `global.json` via `setup-dotnet` `global-json-file`.
- Release (`main.yml`, `release/v*` tags): `pack` job (uploads `nuget-packages` artifact) → `publish` job under environment `nuget-release` (required reviewer; only job using org secret `SKYWARDNUGETAPIKEY`; NuGet push + GitHub release, `--prerelease` when the version has a suffix). Workflow never edits csproj `<Version>`: bump it by PR before tagging (checklist: `docs/Releases.md`). `master` requires PRs + checks.
- `.gitlab-ci.yml` is legacy (v7 packages, `sdk:6.0`).
- `tests.yml` installs `8.0.x` alongside global.json: FunctionalTests' net8.0 TFM needs the 8.0 runtimes (SDK 10 ships only 10.0).
- NuGet: v8 `Skyward.Api.Popcorn.SourceGen` + `.SourceGen.Shared` `8.0.0-preview.1` published 2026-04-24 (tag `release/v8.0.0-preview.1`). Legacy v7: `Skyward.Api.Popcorn`, `Skyward.Api.Popcorn.WebApiCore`.

## Testing Strategy
- **Functional tests** (`Popcorn.FunctionalTests`) use `TestJsonContext` — a single `JsonSerializerContext` that declares `[JsonSerializable(typeof(ApiResponse<Model>))]` (and `[JsonSerializable(typeof(CustomEnvelope<Model>))]` for envelope fixtures) for every test model. The source generator runs against this and produces real converters, which the tests invoke via `JsonSerializer.Serialize` and real `TestServer` pipelines. To inspect generated output, look in `obj/Generated/<tfm>/` (csproj sets `EmitCompilerGeneratedFiles=true`). Targets `net8.0;net10.0` (TestHost 8.0.33 / 10.0.14 per TFM); CI runs both.
- **Generator diagnostic tests** (`Popcorn.SourceGenerator.Tests`) use Roslyn's `CSharpGeneratorDriver` against synthetic in-memory source. `GeneratorTestHarness.Run(source)` returns a `Result` of `Diagnostics + RunResult` for assertions. Covers JSG003–JSG007 plus a positive control.
- **No mocking** of the generator or the serializer — tests exercise the real pipeline.
- **Snapshot tests** (Verify.SourceGenerators/Verify.Xunit, as declared in previous notes) — referenced in prior memory, but current branch leans on functional-output assertions rather than snapshot files.

## Development Workflow
- Source-generator line is on `master`; changes land by PR.
- Code philosophy (from `Plan.md`): one improvement at a time, fully tested before the next; maintain or improve performance; minimalist code.

## Known External Dependencies / Constraints
- `PropertyReference.ParseIncludeStatement` lives in `Popcorn.Shared` (runtime library), not in the generator. Parsing bugs affect all consumers regardless of generator.
- Source generator must target netstandard2.0, so no `Span<T>` / `init`-only niceties in generator code itself (user models can use them).
- `Microsoft.AspNetCore.Http.Abstractions` 2.3.0 is the last netstandard2.0-compatible version.
