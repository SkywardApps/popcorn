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
- xUnit, 22 test files (after deprecation cleanup + review-fix coverage).
- **158 tests total**: 140 passing, 0 failing, 18 skipped (TDD-pending features). Sorting/Pagination/Filtering/Authorizer test files were deleted as part of the v2 scope decision; their 30 skipped tests are gone. Custom envelope + exception middleware shipped: 5 more tests flipped from skipped to passing. Review-driven fixes added `EnvelopeFixesTests.cs` with 9 new passing tests covering naming-policy conversion, header preservation, idempotency, inheritance, and nested envelopes.
- Covers attribute semantics, include-parameter variations, primitives, value types, basic + advanced collections, dictionaries, nesting, conflicting attributes, default-behavior rules, enums, `JsonPropertyName`, inheritance, generics, include-parser edge cases, error handling, middleware integration, computed-property translators, custom envelope shape + exception wrapping.
- Single `TestJsonContext` drives the generator over 30+ `ApiResponse<Model>` and `MyTestEnvelope<Model>` declarations.
- Generated sources visible at `$(BaseIntermediateOutputPath)Generated` for debugging.
- `TestServerHelper` reduces per-test boilerplate to ~5 lines.

### Bugs surfaced by new test coverage
1. **Enum serialization — FIXED.** Generator was treating enum types as POCOs in `GetReferencedTypes`, registering them in `allTypeNames`, which caused `AddMemberSerializationCode` to recurse through a broken `PopEnumType` converter that emitted `{}`. Fix: added an early `continue` in `GetReferencedTypes` for `TypeKind.Enum` and `Nullable<Enum>`, so enum members fall through to the default `JsonSerializer.Serialize(writer, value, options)` path. This path honors global `JsonStringEnumConverter` registrations and per-type `[JsonConverter(typeof(JsonStringEnumConverter))]` attributes transparently — no Popcorn-specific API needed. Covered by `EnumTests` (10 tests: numeric default, nullable, flags, in-collection, global string converter, camelCase naming policy, per-type attribute).
2. **Inheritance: base-class attributes ignored on derived types — FIXED.** `[Default]` / `[Always]` on a base class were dropped when serializing a derived runtime type because `INamedTypeSymbol.GetMembers()` only returns members declared directly on the type. Fix: introduced `GetSerializableProperties` / `GetSerializableFields` helpers that walk derived → base up to `System.Object`, dedupe by name (so `new`/`override` shadows resolve to the derived declaration), and reused them at every member-enumeration site in the generator (`GetReferencedTypes` + `CreateComplexObjectSerialization`). Covered by `PolymorphismTests.DerivedType_InheritedAttributesApply` and `DerivedType_SerializedAsBaseType_EmitsBaseProperties`.

### Established Contract: `?include=` uses wire names only
The include list is part of the API contract — it uses the wire name (from `[JsonPropertyName]` if present, otherwise the C# name). The C# name is an implementation detail the client has no visibility into. A client sees `{"display_name":...}` in responses and must request `?include=[display_name]`; `?include=[DisplayName]` is treated as an unknown name and the property is not emitted. This matches how the generator currently behaves and is the correct design. Tests in `JsonPropertyNameTests.cs` assert this contract explicitly (both positive: `IncludeMatchesWireName`, and negative: `IncludeByCSharpName_DoesNotMatch`).

### TDD-pending test ledger (18 skipped)
Every in-scope planned feature from `apiDesign.md` has corresponding skipped tests. When implementation lands, remove the `Skip=` attribute and the test becomes active. Files:
- `CustomEnvelopeTests.cs` — **0 skipped, 4 passing** (Tier-1 SHIPPED).
- `ErrorHandlingTests.cs` — **0 skipped, 6 passing** (includes new `SerializationException_ProducesErrorEnvelope`).
- `SubPropertyDefaultTests.cs` (4) — Tier-1 remaining work.
- `TranslatorTests.cs` (3 skipped, 3 passing for computed properties), `BlindHandlerTests.cs` (4), `ExpandFromTests.cs` (4) — Tier-2.
- `PolymorphismTests.cs` (2 skipped), `IncludeParserEdgeTests.cs` (1 skipped — the known dictionary-value parser bug).

**Deleted test files (v2 scope decision):** `SortingTests.cs`, `PaginationTests.cs`, `FilteringTests.cs`, `AuthorizerTests.cs`. Corresponding model files (`SortingModel.cs`, `AuthorizationModel.cs`) deleted. See `migrationAnalysis.md` for the scope rationale.

### AOT smoke test (`PopcornAotExample`)
- `WebApplication.CreateSlimBuilder`, `PublishAot=True`, `PublishTrimmed=True`, Dockerfile.
- Three endpoints exercising nullable, nested, and null-response shapes.
- Validates that generated converters pass the AOT analyzer.

### Benchmarks (`dotnet/benchmarks/`)
- `SerializationPerformance` — BenchmarkDotNet suite: serialization comparison (stock vs Popcorn), include strategies, scalability (flat + deep), circular-ref overhead, attribute processing. 7 test models with seeded `TestDataGenerator`. `MemoryDiagnoser` enabled. Command-line selector (`comparison` | `includes` | `scalability` | `circular` | `attributes` | `all`).
- `ParsingIncludes` — include-string parser micro-benchmarks.

## What's Broken or Incomplete

### Known failing / flaky
- `DictionaryTypes_ComplexValueDictionary_SerializesCorrectly` — root cause is `PropertyReference.ParseIncludeStatement` mishandling nested structures within dictionary value types. Fix belongs in `Popcorn.Shared`.

### Dropped from v2 scope (explicitly not porting)
- Sorting
- Pagination
- Filtering
- Authorization / permissioning

Never used in practice with the legacy engine. Callers that need these behaviors implement them at the endpoint level with standard ASP.NET tools. See `migrationAnalysis.md` > "Scope Decision" for full rationale.

### Not yet ported (still in scope)
- `[SubPropertyDefault]` (Tier-1)
- `[Translator]` methods with DI, `IPopcornBlindHandler<TFrom,TTo>`, `[ExpandFrom]` (Tier-2)

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
1. **Perf parity or better vs System.Text.Json** — benchmark suite exists; numbers have not been captured in a committed report yet.
2. **Native AOT works** — `PopcornAotExample` builds with `PublishAot=True`. End-to-end runtime validation: done locally per recent commits, no CI job yet.
3. **Trimming works** — `PublishTrimmed=True` set alongside AOT. No separate trim-only run documented.

## Merge-to-master Gates (suggested, not formalized)
- [x] Decision on in-scope vs. deferred legacy features. **Resolved:** sorting/pagination/filtering/authorizers dropped; custom envelope + `[SubPropertyDefault]` remain as Tier-1.
- [x] Custom envelope + exception middleware (Tier-1).
- [ ] `[SubPropertyDefault]` (Tier-1 remaining).
- [ ] Dictionary/nested parser fix.
- [ ] Published benchmark report comparing legacy reflection vs. source-generated vs. raw `System.Text.Json`.
- [ ] CI job that publishes the AOT example and runs it in a container to prove end-to-end.
- [ ] NuGet packaging story for `Popcorn.SourceGenerator` + `Popcorn.Shared`.
