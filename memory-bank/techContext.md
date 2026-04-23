# Technical Context: Popcorn

## Stack
- **.NET 8+** for runtime targets (test project, AOT example).
- **.NET Standard 2.0** for `Popcorn.Shared` and `Popcorn.SourceGenerator` (source generators *must* target netstandard2.0 per Roslyn rules).
- **Roslyn** — `Microsoft.CodeAnalysis.CSharp` 4.12.0 for the generator.
- **System.Text.Json** 9.0.1.
- **Microsoft.AspNetCore.Http.Abstractions** 2.3.0 (for `HttpContext` in netstandard2.0 land).
- **xUnit** 2.6.6 + `Microsoft.AspNetCore.TestHost` 8.0.0 for functional tests.
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
│   │   │                        #   EnvelopeFixes, BlindHandler, ExpandFrom
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
- `GenerateJsonConverter` → `CreateComplexObjectSerialization` — emits per-property write logic with attribute + property-reference gating.
- Registration: emits `Popcorn.Shared.PopcornJsonOptionsExtension.AddPopcornOptions(JsonSerializerOptions)` which installs every generated converter, sets `NumberHandling = AllowNamedFloatingPointLiterals`, and (if any `[PopcornEnvelope]` types exist) registers `WriteCustomErrorEnvelope` in `PopcornErrorWriterRegistry`. Also emits `AddPopcornEnvelopes(IServiceCollection)` — the DI-time hook that registers the writer without requiring a `JsonSerializerOptions` call. Writer signature: `(Utf8JsonWriter, Type, ApiError, JsonNamingPolicy?)` — generator emits naming-policy-aware field names so error responses match success-path casing.

## Type Handling Buckets (`ExpanderGenerator.cs`)
- **Numbers**: all integer types and `decimal` (floats are in `IgnoreTypes`).
- **Strings**: `string`, `Span<char>`, `ReadOnlySpan<char>`, `Memory<char>`, `ReadOnlyMemory<char>`.
- **Bools**: `bool`, `System.Boolean`.
- **Ignored (write-as-is, don't expand)**: `char`, `float`, `double`, `Guid`, `DateTime`, `TimeSpan`, `DateTimeOffset`.
- **Collections**: dispatched via `InheritsOrImplements` for `IEnumerable<T>` / `IDictionary<K,V>`.
- **Enums and `Nullable<Enum>`**: skipped in `GetReferencedTypes` (not registered as Popcorn-aware types). Fall through to `JsonSerializer.Serialize(writer, value, options)`, which transparently picks up global `options.Converters.Add(new JsonStringEnumConverter())` registrations and per-enum `[JsonConverter(typeof(JsonStringEnumConverter))]` attributes. Default output is numeric; string-form is opt-in via standard System.Text.Json configuration — no Popcorn-specific API.

## CI / Publish
- GitHub Actions (`.github/workflows/main.yml`) and `.gitlab-ci.yml` for builds.
- AppVeyor badge still in README (legacy).
- NuGet package `Skyward.Api.Popcorn` (legacy) — source-generator packages not yet published.
- Recent `master` commits: CI pipeline for NuGet publishing (#72), unrelated bugfix `ELUM-3318_ErrorsReturnedAsOK` (#71).

## Testing Strategy
- **Functional tests** (`Popcorn.FunctionalTests`) use `TestJsonContext` — a single `JsonSerializerContext` that declares `[JsonSerializable(typeof(ApiResponse<Model>))]` (and `[JsonSerializable(typeof(CustomEnvelope<Model>))]` for envelope fixtures) for every test model. The source generator runs against this and produces real converters, which the tests invoke via `JsonSerializer.Serialize` and real `TestServer` pipelines. To inspect generated output, look in `$(BaseIntermediateOutputPath)Generated` (csproj sets `EmitCompilerGeneratedFiles=true`).
- **Generator diagnostic tests** (`Popcorn.SourceGenerator.Tests`) use Roslyn's `CSharpGeneratorDriver` against synthetic in-memory source. `GeneratorTestHarness.Run(source)` returns a `Result` of `Diagnostics + RunResult` for assertions. Covers JSG003–JSG007 plus a positive control.
- **No mocking** of the generator or the serializer — tests exercise the real pipeline.
- **Snapshot tests** (Verify.SourceGenerators/Verify.Xunit, as declared in previous notes) — referenced in prior memory, but current branch leans on functional-output assertions rather than snapshot files.

## Development Workflow
- Current active branch: `spike/source-generator`. Diff vs `master`: ~12k lines added across 82 files, mostly functional tests (~4k), benchmarks (~2k), source generator (~1.5k), and memory-bank docs.
- Do not merge to `master` until feature parity decisions are made on sorting/pagination/filtering/authorization and perf benchmarks are validated.
- Code philosophy (from `Plan.md`): one improvement at a time, fully tested before the next; maintain or improve performance; minimalist code.

## Known External Dependencies / Constraints
- `PropertyReference.ParseIncludeStatement` lives in `Popcorn.Shared` (runtime library), not in the generator. Parsing bugs affect all consumers regardless of generator.
- Source generator must target netstandard2.0, so no `Span<T>` / `init`-only niceties in generator code itself (user models can use them).
- `Microsoft.AspNetCore.Http.Abstractions` 2.3.0 is the last netstandard2.0-compatible version.
