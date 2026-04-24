# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Computed Fields

[Table Of Contents](../../docs/TableOfContents.md)

If you're new to Popcorn, start with [Getting Started](DotNetTutorialGettingStarted.md) first.

Often an API wants to expose a value that isn't a database column — a full name derived from
first and last, a formatted birthday, a sum of line items. In Popcorn v8 this is simply a
**C# computed property**. There is no Popcorn-specific configuration; the generator picks up
the property and treats it like any other.

> **v7 → v8 note.** v7 shipped a `.Translate<T>(...)` configuration lambda for this job,
> which could optionally close over ambient data via `SetContext(Dictionary<string, object>)`.
> v8 drops both. Pure transforms become computed properties; anything that needs injected
> services is resolved at the endpoint layer. See
> [MigrationV7toV8.md §5](../MigrationV7toV8.md#5-translators-computed-fields).

## Pattern 1: Pure transforms — C# computed properties

This is almost all the "translator" cases you'll see in practice.

```csharp
public class Employee
{
    [Default] public string FirstName { get; set; } = "";
    [Default] public string LastName  { get; set; } = "";

    public string FullName => $"{FirstName} {LastName}";

    public DateTimeOffset Birthday { get; set; }
    public string BirthdayShort => Birthday.ToString("yyyy-MM-dd");
}
```

```
GET /employees?include=[FullName,BirthdayShort]
```

```json
{
  "Success": true,
  "Data": [
    { "FullName": "Liz Lemon",   "BirthdayShort": "1981-05-01" },
    { "FullName": "Jack Donaghy", "BirthdayShort": "1957-07-12" }
  ]
}
```

The include list drives which computed properties run: if the client doesn't ask for
`FullName`, the getter never executes. Same cost model as any other property.

## Pattern 2: Needs injected services — resolve at the endpoint

If the value depends on an injected service (a database, an external API, current-user
state), compute it where the data lives — in the endpoint — before handing it to Popcorn:

```csharp
app.MapGet("/cars", (IPopcornAccessor access, IEmployeeLookup lookup, ExampleContext db) =>
{
    var ownerById = lookup.FindMany(db.Cars.Select(c => c.OwnerId).Distinct());

    var view = db.Cars.Select(c => new CarDto
    {
        Id    = c.Id,
        Make  = c.Make,
        Model = c.Model,
        Owner = ownerById.GetValueOrDefault(c.OwnerId),   // one lookup, batched
    }).ToList();

    return access.CreateResponse(view);
});
```

Why this is the recommended pattern for DI-needing "translators":

- **Batchable.** One DB query for all owners, not one per car.
- **Clear I/O boundaries.** Database access happens in the endpoint, not mid-serialization.
- **Testable.** The endpoint is the unit; you don't need a running `JsonSerializerOptions` to
  exercise the logic.
- **Composable.** Your DTO is just a type. You can add `[Never]` / `[Default]` / `[Always]`
  attributes on it exactly like any Popcorn-registered model.

## Pattern 3: External types — standard `JsonConverter<T>`

Sometimes a type comes from a library you don't control — `NetTopologySuite.Geometry` is the
canonical example — and you want it rendered a particular way. Popcorn composes transparently
with standard `System.Text.Json` converters. Register the converter once:

```csharp
public class GeometryConverter : JsonConverter<Geometry>
{
    public override void Write(Utf8JsonWriter writer, Geometry value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToText()); // WKT

    public override Geometry Read(…) => throw new NotImplementedException();
}

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new GeometryConverter());
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    o.SerializerOptions.AddPopcornOptions();
});
```

When Popcorn's generator hits a `Geometry` property it doesn't know how to walk, it falls
through to `JsonSerializer.Serialize(…, options)`, which picks up your registered converter.

## Factories (deferred)

v7 had `.AssignFactory<T>(ctx => …)` for controlling how projection instances are constructed.
v8 doesn't need this for the write path — the generator never constructs your types, it just
reads them. Factories become relevant again if deserialization ships in a future v8.x release.
See [the roadmap](../../roadmap.md) for status.

## See also

- [Default Includes](DotNetTutorialDefaultIncludes.md) for shaping what a bare request returns.
- [MigrationV7toV8.md §5](../MigrationV7toV8.md#5-translators-computed-fields) — full v7 → v8
  mapping for translators.
- [MigrationV7toV8.md §7](../MigrationV7toV8.md#7-projections-replacing-mapentityframework) —
  projection replacement patterns.
- [MigrationV7toV8.md §8](../MigrationV7toV8.md#8-external-type-handlers-blindhandler) —
  `JsonConverter<T>` for external types.
