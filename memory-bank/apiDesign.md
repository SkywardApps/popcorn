# API Design: Popcorn v2 (source-generator era)

## Design Philosophy
1. **Attributes over fluent config.** Model + endpoint declare their contract in code. The legacy fluent-lambda builder (`popcornConfig.Authorize<Car>(...).Translate(...).SetContext(...)`) is fundamentally incompatible with source generation — lambdas live at runtime, the generator needs inputs at build time. Not a technical roadblock; a mandatory API rewrite.
2. **DI over static context dictionaries.** Where the legacy used `.SetContext(Dictionary<string,object>)` to pass ambient data into lambdas, v2 injects DI services into attribute-tagged methods. This is both AOT-safe and idiomatic modern ASP.NET Core.
3. **Source generator emits all dispatch.** Sorting, filtering, authorization lookups, translator calls — all resolved at build time via generated switch statements or DI resolution calls. No runtime reflection on the hot path.
4. **Intentional v2 break.** No source compatibility with the legacy `PopcornNetStandard` config API. New NuGet package ID (`Skyward.Api.Popcorn.SourceGen` or similar), parallel shipping until legacy is deprecated.

## Attribute Surface (on model properties)

### Inclusion semantics (existing)
- `[Always]` — emitted regardless of include list, cannot be negated.
- `[Default]` — emitted when include is empty or `!default`; can be negated with `-Name`.
- `[Never]` — never emitted, even if explicitly requested.

### New attributes
- `[SubPropertyDefault("[Make,Model]")]` — when this property's type appears as a sub-property, use this include list as its default. Replaces `[SubPropertyIncludeByDefault]`.
- `[Sortable]` — opts a property into sort-by-name eligibility. Generator emits typed comparator dispatch.
- `[Filterable(FilterOps.Equals | FilterOps.Contains | FilterOps.GreaterThan)]` — opts a property into filter eligibility. Generator emits typed predicate dispatch.
- `[ExpandFrom(typeof(SourceType))]` — on a projection class; generator emits `ProjectionType.From(SourceType)` copy logic. Optional — most users serialize source types directly.

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
services.AddPopcorn();                                       // existing
services.AddPopcornAuthorizer<Car, CarAuthorizer>();         // NEW
services.AddPopcornBlindHandler<Geometry, string>(           // NEW — external type → simpler form
    (g, svc) => svc.GetRequiredService<IWktWriter>().Write(g));
services.AddPopcornInspector<ApiResponse<object>, MyInspector>(); // NEW — post-serialize envelope rewriter
```

### IPopcornAuthorizer<T>
```csharp
public interface IPopcornAuthorizer<T>
{
    bool AuthorizeInclude(T source, string propertyName, object? value);
    bool AuthorizeItem(T source); // collection-item gate
}
```
Registered per type. Generator's emitted converter resolves the authorizer from DI once per request and calls it during property and item emission.

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
| `include` | Field selection (existing) | `?include=[Id,Name,Items[Name]]` |
| `sort` | Sort by a `[Sortable]` property | `?sort=Model` |
| `sortDirection` | `Ascending` \| `Descending` (default Ascending) | `?sortDirection=Descending` |
| `filter` | One or more `[Filterable]` predicates | `?filter=[Year:gt:2000,Make:eq:Ferrari]` |
| `page` | 1-based page index | `?page=2` |
| `pageSize` | Items per page (default 50, max TBD) | `?pageSize=100` |

**Note on `filter` grammar.** Mirror the bracket syntax of `include` for consistency: `[Field:op:value,Field:op:value]`. Operators: `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `contains`, `startsWith`, `endsWith`, `in`. Value parsing is type-driven (int `"2000"` → `2000`, string literal for string props). No free-form expressions (keep AOT-safe).

## Response Envelope Surface

### Default envelope (existing, refined)
```csharp
public record ApiResponse<T>
{
    public bool Success { get; init; } = true;
    public Pop<T> Data { get; init; }
    public ApiError? Error { get; init; }     // NEW
    public PageInfo? Page { get; init; }      // NEW (only present when pagination active)
}
public record ApiError(string Code, string Message, string? Detail);
public record PageInfo(int Page, int PageSize, long TotalItems, long TotalPages);
```

### Custom envelope
```csharp
[PopcornEnvelope]
public record MyEnvelope<T>
{
    public bool Ok { get; init; } = true;
    public T? Payload { get; init; }
    public List<string> Messages { get; init; } = new();
}

// Register as the default
services.AddPopcorn(options => options.EnvelopeType = typeof(MyEnvelope<>));
```
Generator sees `[PopcornEnvelope]`, emits wrapping code around the `Pop<T>` payload. One envelope per application; multi-envelope support is out of scope.

## Middleware Surface

### Exception → envelope conversion
```csharp
app.UsePopcornExceptionHandler(); // NEW — catches unhandled exceptions, writes
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
| Sorting | ✅ runtime reflection | ✅ | Generator emits `switch(sortField)` → typed comparator |
| Pagination | ✅ | ✅ | Middleware concern; applies `Skip/Take`; emits `PageInfo` |
| Filtering | ✅ | ✅ | Generator emits `switch(filterField)` → typed predicate |
| Authorizers | ✅ lambda config | ✅ via `IPopcornAuthorizer<T>` | DI-registered interface |
| Translators / advanced projections | ✅ lambda config | ✅ via `[Translator]` method + DI | Attribute-tagged static method |
| Factories | ✅ lambda config | ⏸ moot until deserialization | Write path doesn't instantiate |
| Contexts (dictionary) | ✅ | ❌ superseded by DI | Drop the dictionary concept entirely |
| Inspectors | ✅ lambda config | ✅ via envelope type + middleware | Split: type for shape, middleware for exceptions |
| Lazy loading | ✅ | ✅ by construction | Generator never touches excluded props |
| `ExpandFrom` | ✅ | ✅ via `[ExpandFrom]` | Generator emits copy logic |
| Deserialization | ❌ | ⏸ deferred | Out of scope for v2.0 |

## Breaking Changes from V1
- Fluent-lambda config surface removed entirely.
- `[IncludeByDefault]` renamed `[Default]`, `[IncludeAlways]` renamed `[Always]`, `[InternalOnly]` renamed `[Never]`.
- `SetContext(Dictionary<string,object>)` removed — use DI.
- `SetInspector(lambda)` removed — use envelope type + middleware.
- `MapEntityFramework<TSource,TProjection,TContext>` removed — projections are now either direct-serialize-the-source or `[ExpandFrom]` on a projection class.
- Package ID change (TBD) to allow side-by-side install with the legacy package during transition.

## Out of Scope for V2.0
- Deserialization (generator emits read-only converters).
- Polymorphic unknown-at-build-time types (requires reflection, incompatible with AOT).
- Multi-envelope support (one envelope per app).
- Free-form filter expression grammar (stick to typed operators).
- Cross-language providers (PHP, JS client) — protocol only; no shared code.
