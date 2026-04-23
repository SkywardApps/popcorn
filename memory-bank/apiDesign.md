# API Design: Popcorn v2 (source-generator era)

## Design Philosophy
1. **Attributes over fluent config.** Model + endpoint declare their contract in code. The legacy fluent-lambda builder (`popcornConfig.Authorize<Car>(...).Translate(...).SetContext(...)`) is fundamentally incompatible with source generation — lambdas live at runtime, the generator needs inputs at build time. Not a technical roadblock; a mandatory API rewrite.
2. **DI over static context dictionaries.** Where the legacy used `.SetContext(Dictionary<string,object>)` to pass ambient data into lambdas, v2 injects DI services into attribute-tagged methods. This is both AOT-safe and idiomatic modern ASP.NET Core.
3. **Source generator emits all dispatch.** Translator calls, blind-handler conversions, envelope wrapping — all resolved at build time via generated calls. No runtime reflection on the hot path.
4. **Intentional v2 break.** No source compatibility with the legacy `PopcornNetStandard` config API. New NuGet package ID (`Skyward.Api.Popcorn.SourceGen` or similar), parallel shipping until legacy is deprecated.

## Attribute Surface (on model properties)

### Inclusion semantics (existing)
- `[Always]` — emitted regardless of include list, cannot be negated.
- `[Default]` — emitted when include is empty or `!default`; can be negated with `-Name`.
- `[Never]` — never emitted, even if explicitly requested.

### New attributes
- `[SubPropertyDefault("[Make,Model]")]` — when this property's type appears as a sub-property, use this include list as its default. Replaces `[SubPropertyIncludeByDefault]`.
- `[ExpandFrom(typeof(SourceType))]` — on a projection class; generator emits `ProjectionType.From(SourceType)` copy logic. Optional — most users serialize source types directly.

### Envelope marker attributes
- `[PopcornEnvelope]` — marks a type as the application-wide response envelope. One per app.
- `[PopcornPayload]` — marks the property that carries the `Pop<T>` payload. Required on any `[PopcornEnvelope]` type.
- `[PopcornError]` — marks the optional `ApiError?` property used by the exception middleware.
- `[PopcornSuccess]` — marks the optional `bool` property set to `false` on error paths.

## Attribute Surface (on methods — translators)

### Option A: computed property (preferred; already works)
```csharp
public partial record Employee(string First, string Last)
{
    public string FullName => $"{First} {Last}";
}
```

### Option B: `[Translator]` method with DI
```csharp
public partial class Car
{
    public EmployeeRef? Owner { get; init; } // populated by generator-emitted call

    [Translator(nameof(Owner))]
    public static EmployeeRef? ResolveOwner(Car source, IEmployeeLookup lookup)
        => lookup.Find(source.Id);
}
```
Generator inspects the method signature: first param is the source type (matched by position), remaining params come from DI via `IServiceProvider.GetRequiredService`. Emits a call from the `Write` path with DI resolution.

### Option C: partial method
```csharp
public partial record Car
{
    public string DisplayName { get; init; } = "";
    static partial string ComputeDisplayName(Car c);
}
```
Generator wires `DisplayName = ComputeDisplayName(this)` into the write path. User implements `ComputeDisplayName` in a partial file.

## DI Surface (on the service container)

```csharp
services.AddHttpContextAccessor();
services.AddPopcorn(o => o.EnvelopeType = typeof(MyEnvelope<>));   // existing + envelope option
services.AddPopcornBlindHandler<Geometry, string>(                  // NEW — external type → simpler form
    (g, svc) => svc.GetRequiredService<IWktWriter>().Write(g));
```

### IPopcornBlindHandler<TFrom, TTo>
```csharp
public interface IPopcornBlindHandler<TFrom, TTo>
{
    TTo Convert(TFrom source);
}
```
For externally-defined types (e.g. NetTopologySuite `Geometry`) where you can't annotate the type but want a custom projection. User still declares `[JsonSerializable(typeof(ApiResponse<ThingWithGeometry>))]`; generator sees `Geometry` in the walk and emits `writer.WriteValue(handler.Convert(g))` if a handler is registered. If no handler, falls back to System.Text.Json default for the type.

## Query Parameter Surface

| Parameter | Purpose | Example |
|---|---|---|
| `include` | Field selection | `?include=[Id,Name,Items[Name]]` |

v2 has no other query parameters. Sorting, pagination, and filtering were explicitly dropped from v2 scope (never used in practice with the legacy engine; complexity not justified).

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
    [PopcornPayload] public T? Payload { get; init; }
    [PopcornError]   public ApiError? Problem { get; init; }

    // Free-form user fields — passed through as-is
    public List<string> Messages { get; init; } = new();
}

// Register as the app-wide envelope
services.AddPopcorn(options => options.EnvelopeType = typeof(MyEnvelope<>));
```

Rules enforced by the generator:
- Exactly one property carries each marker; multiple markers of the same kind → diagnostic.
- `[PopcornPayload]` is required on any `[PopcornEnvelope]` type; absence → diagnostic.
- `[PopcornError]` property type must be `ApiError?` (or compatible).
- One envelope per application; multi-envelope support is out of scope.

Generator sees `[PopcornEnvelope]` and emits typed `CreateSuccess<T>(Pop<T>)` / `CreateError<T>(ApiError)` factories on a generated `PopcornEnvelopeFactory` class. Middleware uses these factories.

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

| Feature | V1 (reflection) | V2 (source-gen) | How |
|---|---|---|---|
| Include parsing | ✅ | ✅ | Same `PropertyReference` parser in `Popcorn.Shared` |
| `[IncludeByDefault]` / `[IncludeAlways]` | ✅ | ✅ (renamed `[Default]` / `[Always]`) | Existing |
| Blind expansion (own types) | ✅ | ✅ | Automatic — generator walks reachable types |
| Blind expansion (external types) | ✅ runtime reflection | ✅ via `IPopcornBlindHandler<TFrom,TTo>` | Registered DI handler |
| Blind expansion (runtime-unknown polymorphic) | ✅ | ❌ non-starter under AOT | Live with the break |
| `[InternalOnly]` | ✅ | ✅ (as `[Never]`) | Existing |
| `[SubPropertyIncludeByDefault]` | ✅ | ✅ (as `[SubPropertyDefault]`) | New attribute, existing parser |
| Optional property `?` prefix | ✅ | ✅ by construction | Generator silently skips unknown include names |
| Sorting | ✅ runtime reflection | ❌ **Dropped from V2 scope** | Never used in practice; complexity not justified |
| Pagination | ✅ | ❌ **Dropped from V2 scope** | Never used in practice; complexity not justified |
| Filtering | ✅ | ❌ **Dropped from V2 scope** | Never used in practice; complexity not justified |
| Authorizers | ✅ lambda config | ❌ **Dropped from V2 scope** | Never used in practice; complexity not justified |
| Translators / advanced projections | ✅ lambda config | ✅ via `[Translator]` method + DI | Attribute-tagged static method |
| Factories | ✅ lambda config | ⏸ moot until deserialization | Write path doesn't instantiate |
| Contexts (dictionary) | ✅ | ❌ superseded by DI | Drop the dictionary concept entirely |
| Inspectors | ✅ lambda config | ✅ via envelope type + middleware | Split: type for shape, middleware for exceptions |
| Lazy loading | ✅ | ✅ by construction | Generator never touches excluded props |
| `ExpandFrom` | ✅ | ✅ via `[ExpandFrom]` | Generator emits copy logic |
| Custom envelope + exception middleware | ✅ lambda config | ✅ via `[PopcornEnvelope]` markers + `UsePopcornExceptionHandler` | Generator emits factories; middleware dispatches |
| Deserialization | ❌ | ⏸ deferred | Out of scope for v2.0 |

## Breaking Changes from V1
- Fluent-lambda config surface removed entirely.
- `[IncludeByDefault]` renamed `[Default]`, `[IncludeAlways]` renamed `[Always]`, `[InternalOnly]` renamed `[Never]`.
- `SetContext(Dictionary<string,object>)` removed — use DI.
- `SetInspector(lambda)` removed — use envelope type + middleware.
- `MapEntityFramework<TSource,TProjection,TContext>` removed — projections are now either direct-serialize-the-source or `[ExpandFrom]` on a projection class.
- Sorting, pagination, filtering, authorization: **dropped entirely**. Callers that depended on `?sort=`, `?page=`, `?filter=`, or `.Authorize<T>(...)` must implement these themselves at the endpoint level.
- Package ID change (TBD) to allow side-by-side install with the legacy package during transition.

## Out of Scope for V2.0
- **Sorting, pagination, filtering, authorizers** — dropped from v2 scope permanently.
- Deserialization (generator emits read-only converters).
- Polymorphic unknown-at-build-time types (requires reflection, incompatible with AOT).
- Multi-envelope support (one envelope per app).
- Cross-language providers (PHP, JS client) — protocol only; no shared code.
