# Migrating from Popcorn v7 (and earlier) to v8

[Table Of Contents](TableOfContents.md)

Popcorn v8 replaces the runtime-reflection expander (`Skyward.Api.Popcorn` /
`Skyward.Api.Popcorn.DotNetCore`, shipped as v7 and earlier on NuGet) with a Roslyn source
generator. The surface area is incompatible by design: every v7 extension point that depended on
runtime lambdas or reflection-scanned types has been redesigned to work at build time, so Popcorn
can run under Native AOT (`PublishAot=True`) and IL trimming (`PublishTrimmed=True`).

This guide walks the breaks you will hit, what to change, and what has been dropped outright.
If a feature you rely on is in the **"Dropped from v8"** section, you will need to handle it
yourself at the endpoint level — v8 will not ship a replacement.

> **Packages**: v8 ships as new package IDs (finalizing — see the
> [Roadmap](Roadmap.md)). The legacy `Skyward.Api.Popcorn` / `Skyward.Api.Popcorn.DotNetCore`
> packages (v7) will continue to ship from `master` for at least one release after v8 cuts, so
> you can install side-by-side during migration.

## At a glance

| Concept | v7 (reflection) | v8 (source generator) |
|---|---|---|
| Expansion engine | Runtime reflection via `Skyward.Popcorn.Expander` | Roslyn source generator emits `JsonConverter<T>` per type at build time |
| Serializer | Newtonsoft or `JsonSerializer` via a projection-to-`Dictionary<string, object?>` | `System.Text.Json` with generator-emitted converters; writes directly to `Utf8JsonWriter` |
| Type discovery | `config.Map<Car>()` lambda at startup | `[JsonSerializable(typeof(ApiResponse<Car>))]` on a `JsonSerializerContext` subclass |
| Default inclusion | `[IncludeByDefault]` | `[Default]` |
| Always-emit | `[IncludeAlways]` | `[Always]` |
| Never-emit | `[InternalOnly]` | `[Never]` |
| Sub-default includes | `[SubPropertyIncludeByDefault]` | `[SubPropertyDefault("[Make,Model]")]` |
| Computed field | `.Translate<Car>(c => c.First + " " + c.Last)` lambda | C# computed property (preferred) or `[Translator]` method with DI *(not yet shipped — see [Roadmap](Roadmap.md))* |
| Projection class | `MapEntityFramework<TSource, TProjection, TContext>` | `[ExpandFrom(typeof(Source))]` *(not yet shipped)* |
| External-type conversion | `BlindHandler` via runtime reflection | `IPopcornBlindHandler<TFrom, TTo>` DI service *(not yet shipped)* |
| Ambient data | `.SetContext(Dictionary<string, object>)` | Standard ASP.NET Core DI — translator methods receive services as parameters |
| Exception wrapping | `.SetInspector((data, ctx, exc) => wrapper)` | `UsePopcornExceptionHandler()` middleware + `[PopcornEnvelope]` marker attributes |
| Authorization | `.Authorize<T>((src, ctx, val) => …)` | **Dropped.** Use ASP.NET Core authorization middleware and endpoint-level checks |
| Sorting / Pagination / Filtering | `?sort=…`, `?skip/take=…`, `?filter=…` | **Dropped.** Implement at the endpoint level |
| AOT / Trim | Not supported (reflection-heavy) | First-class: `PublishAot=True` + `PublishTrimmed=True` validated |

## 1. Packages and `using` directives

### v7

```xml
<PackageReference Include="Skyward.Api.Popcorn.DotNetCore" Version="7.*" />
```

```csharp
using Skyward.Popcorn;
using Skyward.Popcorn.Expanders;
```

### v8

```xml
<!-- Final package IDs are being finalized; see roadmap.md for the current plan. -->
<PackageReference Include="Popcorn.Shared" Version="8.*" />
<PackageReference Include="Popcorn.SourceGenerator" Version="8.*" PrivateAssets="all" />
```

```csharp
using Popcorn;         // attributes live here
using Popcorn.Shared;  // ApiResponse<T>, Pop<T>, ApiError, IPopcornAccessor, middleware extensions
```

The v7 `Skyward.Popcorn*` namespaces are gone. There is no compatibility shim — update your
`using`s.

## 2. Startup configuration

### v7

```csharp
services.UsePopcorn((config) =>
{
    config
        .UseDefaultConfiguration()
        .Map<Car>()
        .Map<Employee>()
        .Translate<Employee>(e => e.FullName, e => $"{e.First} {e.Last}")
        .Authorize<Car>((source, ctx, value) => (bool)ctx["IsAdmin"] || value.OwnerId == (int)ctx["UserId"])
        .SetContext(new Dictionary<string, object> { ["UserId"] = 42, ["IsAdmin"] = false })
        .SetInspector((data, ctx, exception) =>
        {
            if (exception != null)
                return new { success = false, error = exception.Message };
            return new { success = true, data };
        });
});

services.AddControllers(c => c.Filters.Add<ExpandServiceFilter>());
```

### v8

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddPopcorn(o =>
{
    // Optional: use a custom envelope shape. Omit for the default ApiResponse<T>.
    o.EnvelopeType = typeof(MyEnvelope<>);
    o.DefaultNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddPopcornEnvelopes(); // only needed when EnvelopeType is a custom [PopcornEnvelope]

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    o.SerializerOptions.AddPopcornOptions(); // installs generator-emitted converters
});

var app = builder.Build();
app.UsePopcornExceptionHandler(); // catches exceptions → envelope with ApiError

// Endpoints receive IPopcornAccessor to parse the ?include= query and wrap responses.
app.MapGet("/cars", (IPopcornAccessor access) => access.CreateResponse(GetCars()));
app.Run();

[JsonSerializable(typeof(ApiResponse<List<Car>>))]
[JsonSerializable(typeof(ApiResponse<Employee>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
```

The shape of the change:
- Type registration moved from `config.Map<Car>()` calls to `[JsonSerializable(typeof(ApiResponse<Car>))]`
  attributes on a `JsonSerializerContext` subclass. The generator walks the nested type graph from
  those registrations, so you generally only list top-level response types.
- The v7 `ExpandServiceFilter` and `ExpandResultAttribute` are gone. Endpoints receive
  `IPopcornAccessor` via DI and call `access.CreateResponse(data)`.
- `SetContext(Dictionary<string, object>)` is gone. Use standard ASP.NET Core DI — register
  services with `AddScoped` / `AddSingleton` and inject them where needed.

## 3. Attribute renames

Every Popcorn attribute changed names. Do a project-wide find-and-replace:

| v7 | v8 |
|---|---|
| `[IncludeByDefault]` | `[Default]` |
| `[IncludeAlways]` | `[Always]` |
| `[InternalOnly]` | `[Never]` |
| `[SubPropertyIncludeByDefault("...")]` | `[SubPropertyDefault("...")]` |

Semantics are otherwise unchanged: `[Always]` emits regardless of include list, `[Default]` emits
when the include list is empty or contains `!default`, `[Never]` never emits.

## 4. Include-parameter names now use **wire names**, not C# names

In v7, `?include=` matched C# property identifiers. In v8 it matches the wire name (the value
passed to `[JsonPropertyName("…")]`, or the C# name normalized by your `JsonNamingPolicy`).

This is a hard break for any client code that relied on the C# identifier. A response body
`{"display_name": "..."}` must be requested as `?include=[display_name]`; `?include=[DisplayName]`
is a no-op silently and will not emit the field.

This is intentional: the `?include=` list is part of the public API contract that clients see.
They have no visibility into C# identifier casing.

## 5. Translators (computed fields)

### v7

```csharp
config.Translate<Employee>(e => e.FullName, e => $"{e.First} {e.Last}");
```

### v8

**Preferred — regular C# computed property.** Works today, zero framework:

```csharp
public partial record Employee(string First, string Last)
{
    public string FullName => $"{First} {Last}";
}
```

**Planned (not yet shipped) — `[Translator]` method with DI.** For cases where the computation
needs an injected service. Track via the [Roadmap](Roadmap.md):

```csharp
public partial class Car
{
    public EmployeeRef? Owner { get; init; }

    [Translator(nameof(Owner))]
    public static EmployeeRef? ResolveOwner(Car source, IEmployeeLookup lookup)
        => lookup.Find(source.Id);
}
```

If you rely on translators-with-services today, stay on v7 until the `[Translator]` ship item
closes, or hand-roll the translation at the endpoint.

## 6. Custom response envelope + error handling

### v7

```csharp
config.SetInspector((data, ctx, exception) =>
{
    if (exception != null) return new { success = false, error = exception.Message };
    return new MyEnvelope<object> { Ok = true, Payload = data };
});
```

### v8 — two clean pieces

**Declare the envelope with marker attributes.** The generator emits a typed, reflection-free
error writer for it:

```csharp
[PopcornEnvelope]
public record MyEnvelope<T>
{
    [PopcornSuccess] public bool Ok       { get; init; } = true;
    [PopcornPayload] public Pop<T>?  Payload  { get; init; }
    [PopcornError]   public ApiError?    Problem  { get; init; }

    // Free-form fields pass through as-is.
    public List<string> Messages { get; init; } = new();
}
```

**Register and wire the middleware:**

```csharp
builder.Services.AddPopcorn(o => o.EnvelopeType = typeof(MyEnvelope<>));
builder.Services.AddPopcornEnvelopes();
app.UsePopcornExceptionHandler();
```

**Return it from endpoints:**

```csharp
app.MapGet("/cars", (IPopcornAccessor access) =>
    new MyEnvelope<List<Car>>
    {
        Payload = new Pop<List<Car>> { Data = GetCars(), PropertyReferences = access.PropertyReferences }
    });
```

The middleware wraps unhandled exceptions in your envelope shape with `Problem` populated
(`ApiError(Code, Message, Detail?)`) and status 500.

## 7. Projections (`ExpandFrom` / `MapEntityFramework`)

### v7

```csharp
config.MapEntityFramework<CarEntity, CarDto, AppDbContext>();
// Or projection lambdas per property.
```

### v8 — **not yet shipped** ([Roadmap Tier 2](Roadmap.md))

Planned design: decorate the projection class and let the generator emit a copy method:

```csharp
[ExpandFrom(typeof(CarEntity))]
public record CarDto(int Id, string Make, string Model);

// Generator emits:
// public static CarDto From(CarEntity source) => new(source.Id, source.Make, source.Model);
```

Until this ships, project manually at the endpoint (`return entities.Select(e => new CarDto(e.Id, e.Make, e.Model))`)
or stay on v7 for projection-heavy endpoints.

## 8. External-type handlers (`BlindHandler`)

### v7

The v7 reflection expander handled externally-defined types (e.g. `NetTopologySuite.Geometry`)
by walking them at runtime.

### v8 — **not yet shipped** ([Roadmap Tier 2](Roadmap.md))

Planned: `IPopcornBlindHandler<TFrom, TTo>` DI service. Generator sees `TFrom` during the type
walk and, if a handler is registered, emits a conversion call.

```csharp
services.AddPopcornBlindHandler<Geometry, string>(
    (g, svc) => svc.GetRequiredService<IWktWriter>().Write(g));
```

Until this ships, either add a `[JsonConverter]` attribute directly on the external type's
property (standard STJ) or introduce a typed wrapper you control.

## 9. Dropped from v8 (these will not return)

| Feature | What to do instead |
|---|---|
| `.Authorize<T>(…)` | Use ASP.NET Core authorization — `[Authorize]` attributes, policy handlers, endpoint-level checks |
| `?sort=…` (built-in sorting) | Accept a sort parameter in your endpoint and apply it to your query (`IQueryable.OrderBy`, etc.) |
| `?skip=…&take=…` (built-in pagination) | Accept paging parameters and apply at the query layer |
| `?filter=…` (built-in filtering) | Parse the filter grammar you want at the endpoint (OData, custom, etc.) and apply in your query |
| `SetContext(Dictionary<string, object>)` | Standard DI — register services and inject them |
| `SetInspector` for success-shape rewriting | Use a `[PopcornEnvelope]` type as the shape; the middleware covers the error case |
| Legacy `PopcornFactory.CreatePopcorn()` manual expansion | Call `IPopcornAccessor.CreateResponse(data)` and let the generator-emitted converter do the work |
| `ExpandServiceFilter` / `ExpandResultAttribute` | Return `access.CreateResponse(data)` (or your `[PopcornEnvelope]` type) directly from the endpoint |

Rationale: the dropped features were either very rarely used in practice, or duplicated what
modern ASP.NET Core already provides cleanly. See
[`migrationAnalysis.md`](../memory-bank/migrationAnalysis.md) for the full scope decision.

## 10. Runtime polymorphism limits (AOT non-starter)

v7 happily serialized properties typed as `object`, abstract classes, or interfaces — reflection
walked whatever runtime type happened to be in the slot. Under AOT and trimming, the trimmer
removes that metadata, so the generator can't emit anything useful for those shapes.

v8 emits **diagnostic JSG008** when it sees a member whose static type is `object`, an abstract
class, or an interface:

```
JSG008 Member 'Bag.Tag' is typed as 'object', whose concrete runtime type cannot be resolved
       at build time. Popcorn's source generator cannot emit a converter for this shape under
       Native AOT or IL trimming.
```

Fix options, in order of preference:
1. Expose the concrete type directly (`public Car Vehicle` instead of `public object Vehicle`).
2. Register every expected derived type via `[JsonDerivedType]` on the base, and a
   `[JsonSerializable]` attribute for each concrete type.
3. If the value genuinely can't be typed, handle it outside Popcorn — return a pre-serialized
   `JsonElement` or emit the endpoint response manually.

## 11. Generator diagnostics reference

Warnings you might see from the v8 generator, with the shape of the fix:

| ID | Trigger | Fix |
|---|---|---|
| `JSG001` | Generator threw during emission | Report a bug — include the triggering type |
| `JSG002` | Informational log from generator | None — informational only |
| `JSG003` | `[PopcornEnvelope]` type has no `[PopcornPayload]` property | Add a `Pop<T>` property marked with `[PopcornPayload]` |
| `JSG004` | Multiple properties share the same envelope marker | Remove the duplicate marker |
| `JSG005` | `[PopcornPayload]` property is not typed `Pop<T>` | Change the property type to `Pop<T>` |
| `JSG006` | `[PopcornError]` property is not `ApiError` or `ApiError?` | Change the type to `ApiError?` |
| `JSG007` | Envelope declared inside a generic outer type | Move the envelope to the top level or a non-generic container |
| `JSG008` | Property typed as `object` / abstract class / interface | See §10 above |

## 12. Checklist

Before flipping the switch, verify:

- [ ] All `Skyward.Popcorn*` `using`s replaced with `Popcorn` / `Popcorn.Shared`.
- [ ] Every v7 attribute renamed per §3.
- [ ] Every `config.Map<T>()` call replaced with a `[JsonSerializable(typeof(ApiResponse<T>))]`
      attribute on a `JsonSerializerContext` subclass.
- [ ] `config.Translate(...)` lambdas converted to computed properties (or stay on v7 until
      `[Translator]` ships).
- [ ] `config.Authorize(...)` removed; authorization moved to endpoint / policy layer.
- [ ] `config.SetContext(dict)` replaced with DI registrations.
- [ ] `config.SetInspector(...)` split: shape moves to a `[PopcornEnvelope]` type; error-wrapping
      moves to `UsePopcornExceptionHandler()`.
- [ ] Endpoints return `access.CreateResponse(data)` (or your `[PopcornEnvelope]` wrapper), not
      raw domain objects decorated with `ExpandResultAttribute`.
- [ ] Clients updated: `?include=` uses wire names (`display_name`), not C# names (`DisplayName`).
- [ ] `?sort=` / `?skip=` / `?take=` / `?filter=` handled at the endpoint, not via query-param
      framework features.
- [ ] `dotnet build` emits no `JSG003`–`JSG008` diagnostics you didn't expect.
- [ ] If targeting AOT: `dotnet publish -c Release -p:PublishAot=True` succeeds and the resulting
      binary runs the endpoints end-to-end.

## 13. Rollback

If you hit a blocker:
- v7 (`Skyward.Api.Popcorn` / `Skyward.Api.Popcorn.DotNetCore`) will remain on NuGet for at least
  one release after v8 ships. Revert the package references and the namespace changes.
- The v7 and v8 packages are designed to install side-by-side. A single project cannot reasonably
  use both, but a solution can have some projects on v7 and others on v8 during a rolling
  migration.

## See also

- [Performance](Performance.md) — why v8 is faster, with benchmarked ratios vs v7 and raw `System.Text.Json`.
- [Roadmap](Roadmap.md) — Tier-2 feature ship status (translators with DI, blind handlers, `[ExpandFrom]`).
- [migrationAnalysis.md](../memory-bank/migrationAnalysis.md) — the full feature-by-feature
  feasibility ledger used to decide what survived the v8 cut.
- [apiDesign.md](../memory-bank/apiDesign.md) — v8 API design philosophy and surface.
