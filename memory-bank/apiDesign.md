# API Design: Popcorn v8 (source-generator era)

Shipped surface as of `8.0.0-preview.1`. ("v8" here = the source-gen line; older memory-bank/benchmark material sometimes calls it "v2" — same thing.)

## Design Philosophy
1. **Attributes over fluent config.** Model + endpoint declare their contract in code. The legacy fluent-lambda builder (`popcornConfig.Authorize<Car>(...).Translate(...).SetContext(...)`) is fundamentally incompatible with source generation — lambdas live at runtime, the generator needs inputs at build time. Not a technical roadblock; a mandatory API rewrite.
2. **DI over static context dictionaries.** Where the legacy used `.SetContext(Dictionary<string,object>)` to pass ambient data into lambdas, v8 uses standard ASP.NET Core DI. AOT-safe and idiomatic.
3. **Source generator emits all dispatch.** Envelope wrapping, error writers — all resolved at build time via generated calls. No runtime reflection on the hot path.
4. **Intentional v8 break.** No source compatibility with the legacy `PopcornNetStandard` config API. New NuGet package IDs (`Skyward.Api.Popcorn.SourceGen` + `.SourceGen.Shared`), shipping in parallel with legacy v7 until it is deprecated.

## Attribute Surface (on model properties)

### Inclusion semantics
- `[Always]` — emitted regardless of include list, cannot be negated.
- `[Default]` — emitted when include is empty or `!default`; can be negated with `-Name`.
- `[Never]` — never emitted, even if explicitly requested.
- `[SubPropertyDefault("[Make,Model]")]` — when this property is included without explicit sub-children, use this include list as its default. Replaces v7's `[SubPropertyIncludeByDefault]`. Pre-parsed once per process into a generator-emitted static readonly field and substituted at the two nested-`Pop<T>` callsites (complex member, complex-array element). Explicit sub-children override the attribute; `[Always]` / `[Never]` on the sub-type still win.
- **(Considered and dropped, 2026-04-23)** `[ExpandFrom(typeof(SourceType))]` — the three real use cases have cleaner answers: `[Never]` on internal source properties, a three-line hand-written factory, or `Mapster.SourceGenerator`. See `docs/MigrationV7toV8.md` §7.

### Envelope marker attributes
- `[PopcornEnvelope]` — marks a type as the application-wide response envelope. One per app.
- `[PopcornPayload]` — marks the property that carries the `Pop<T>` payload. Required on any `[PopcornEnvelope]` type.
- `[PopcornError]` — marks the optional `ApiError?` property used by the exception middleware.
- `[PopcornSuccess]` — marks the optional `bool` property set to `false` on error paths.

## Translators

### Computed property (the one v8 pattern for pure transforms)
```csharp
public partial record Employee(string First, string Last)
{
    public string FullName => $"{First} {Last}";
}
```

**Dropped 2026-04-23: `[Translator]` method with DI.** The DI-during-serialization pattern fires N+1 queries on collections, moves I/O into the response-writing path, and requires threading `IServiceProvider` into `JsonSerializerOptions` which STJ doesn't natively support. The v8 answer is endpoint-side resolution: compute where the data lives, then serialize. See `docs/MigrationV7toV8.md` §5.

## DI Surface (on the service container)

```csharp
services.AddHttpContextAccessor();
services.AddPopcorn(o => o.EnvelopeType = typeof(MyEnvelope<>));
services.AddPopcornEnvelopes();
```

**Dropped 2026-04-23: `IPopcornBlindHandler<TFrom,TTo>`.** Standard `System.Text.Json` `JsonConverter<T>` registered on `JsonSerializerOptions.Converters` covers the full external-type case and composes with Popcorn transparently (Popcorn's generator falls through to `JsonSerializer.Serialize` for unknown types, STJ picks up the user's registered converter). See `docs/MigrationV7toV8.md` §8.

## Query Parameter Surface

| Parameter | Purpose | Example |
|---|---|---|
| `include` | Field selection | `?include=[Id,Name,Items[Name]]` |

v8 has no other query parameters. Sorting, pagination, and filtering were explicitly dropped (never used in practice with the legacy engine; complexity not justified).

## Response Envelope Surface

### Default envelope
```csharp
public record ApiResponse<T>
{
    public bool Success { get; init; } = true;
    public Pop<T> Data { get; init; }
    public ApiError? Error { get; init; }     // set by UsePopcornExceptionHandler on error paths
}

public record ApiError(string Code, string Message, string? Detail = null);
```

### Custom envelope (marker-attribute design)
```csharp
[PopcornEnvelope]
public record MyEnvelope<T>
{
    [PopcornSuccess] public bool Ok { get; init; } = true;
    [PopcornPayload] public Pop<T> Payload { get; init; }
    [PopcornError]   public ApiError? Problem { get; init; }

    // Free-form user fields — passed through as-is
    public List<string> Messages { get; init; } = new();
}

// Register as the app-wide envelope
services.AddPopcorn(options => options.EnvelopeType = typeof(MyEnvelope<>));
```

Rules enforced by the generator (diagnostics JSG003–JSG007):
- `[PopcornPayload]` is required (JSG003) and each marker must be unique (JSG004).
- Payload should be `Pop<T>` (JSG005 warning); error slot should be `ApiError` (JSG006 warning).
- Envelope must not be nested inside a generic outer type (JSG007).
- One envelope per application; multi-envelope support is out of scope.

Error-path dispatch: the generator emits a `WriteCustomErrorEnvelope` writer per envelope and registers it in `PopcornErrorWriterRegistry` (via `AddPopcornOptions()` or `AddPopcornEnvelopes()`); `UsePopcornExceptionHandler` calls `TryWrite` with the configured envelope type. Full flow in `systemPatterns.md` § "Envelope dispatch architecture".

## Middleware Surface

### Exception → envelope conversion
```csharp
app.UsePopcornExceptionHandler(); // catches unhandled exceptions, writes
                                  // the configured envelope with Success=false / Error populated
```
Replaces the legacy `SetInspector((data, ctx, exception) => wrapper)` pattern. Exception wrapping is a middleware concern; the type-level envelope is a source-gen concern. Clean separation.

### Include-parameter transport
- Current: query string `?include=[...]`.
- Planned: `POPCORN-INCLUDE` header as alternative. `PopcornAccessor` checks header first, falls back to query. No breaking change.

## Feature Feasibility Ledger

| Feature | v7 (reflection) | v8 (source-gen) | How |
|---|---|---|---|
| Include parsing | ✅ | ✅ | Same `PropertyReference` parser in `Popcorn.Shared` |
| `[IncludeByDefault]` / `[IncludeAlways]` | ✅ | ✅ (renamed `[Default]` / `[Always]`) | Existing |
| Blind expansion (own types) | ✅ | ✅ | Automatic — generator walks reachable types |
| Blind expansion (external types) | ✅ runtime reflection | ❌ **Dropped from v8 scope** | Use a standard `JsonConverter<T>` on `JsonSerializerOptions.Converters` (see `docs/MigrationV7toV8.md` §8) |
| Blind expansion (runtime-unknown polymorphic) | ✅ | ❌ non-starter under AOT | JSG008 diagnostic warns; live with the break |
| `[InternalOnly]` | ✅ | ✅ (as `[Never]`) | Existing |
| `[SubPropertyIncludeByDefault]` | ✅ | ✅ (as `[SubPropertyDefault]`, shipped) | New attribute, existing parser, pre-parsed-once static field |
| Optional property `?` prefix | ✅ | ✅ by construction | Generator silently skips unknown include names |
| Sorting | ✅ runtime reflection | ❌ **Dropped from v8 scope** | Never used in practice; complexity not justified |
| Pagination | ✅ | ❌ **Dropped from v8 scope** | Never used in practice; complexity not justified |
| Filtering | ✅ | ❌ **Dropped from v8 scope** | Never used in practice; complexity not justified |
| Authorizers | ✅ lambda config | ❌ **Dropped from v8 scope** | Never used in practice; complexity not justified |
| Translators (pure transforms) | ✅ lambda config | ✅ C# computed properties | Works today; `TranslatorTests` has 3 passing |
| Translators (DI-needing) | ✅ lambda config | ❌ **Dropped from v8 scope** | Resolve at endpoint or via standard `JsonConverter<T>` (see `docs/MigrationV7toV8.md` §5) |
| Factories | ✅ lambda config | ⏸ moot until deserialization | Write path doesn't instantiate |
| Contexts (dictionary) | ✅ | ❌ superseded by DI | Drop the dictionary concept entirely |
| Inspectors | ✅ lambda config | ✅ via envelope type + middleware | Split: type for shape, middleware for exceptions |
| Lazy loading | ✅ | ✅ by construction | Generator never touches excluded props |
| `ExpandFrom` / projections | ✅ `MapEntityFramework<S,P,Ctx>` | ❌ **Dropped from v8 scope** | Use `[Never]` on source, a hand-written factory, or `Mapster.SourceGenerator` (see `docs/MigrationV7toV8.md` §7) |
| Custom envelope + exception middleware | ✅ lambda config | ✅ via `[PopcornEnvelope]` markers + `UsePopcornExceptionHandler` | Generator emits error writers; middleware dispatches via registry |
| Polymorphism dispatch (`[JsonDerivedType]`) | ✅ | ⏸ deferred (2 skipped tests) | Implement if a consumer blocks on it |
| Deserialization | ❌ | ⏸ deferred | Out of scope for v8.0 |

## Breaking Changes from v7
- Fluent-lambda config surface removed entirely.
- `[IncludeByDefault]` renamed `[Default]`, `[IncludeAlways]` renamed `[Always]`, `[InternalOnly]` renamed `[Never]`.
- `SetContext(Dictionary<string,object>)` removed — use DI.
- `SetInspector(lambda)` removed — use envelope type + middleware.
- `MapEntityFramework<TSource,TProjection,TContext>` removed — projections are now direct-serialize-the-source with `[Never]` on internal properties, a hand-written `From(TSource)` factory, or `Mapster.SourceGenerator` for complex mapping.
- Sorting, pagination, filtering, authorization: **dropped entirely**. Callers implement these at the endpoint level.
- `?include=` matches the **wire name** (`[JsonPropertyName]` / naming policy), not the C# identifier.
- New package IDs `Skyward.Api.Popcorn.SourceGen` + `.SourceGen.Shared` — side-by-side installable with legacy `Skyward.Api.Popcorn` v7.

## Out of Scope for v8.0
- Sorting, pagination, filtering, authorizers — dropped permanently.
- Deserialization (generator emits write-only converters).
- Polymorphic unknown-at-build-time types (requires reflection, incompatible with AOT).
- Multi-envelope support (one envelope per app).
- Cross-language providers (PHP, JS client) — protocol only; no shared code.
