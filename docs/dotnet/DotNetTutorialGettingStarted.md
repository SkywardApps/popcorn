# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Getting Started

[Table Of Contents](../../docs/TableOfContents.md)

This walkthrough builds a minimal ASP.NET Core web API with Popcorn v8 end-to-end: models,
endpoints, and include-aware responses. If you already have an app and just need the wire-up
steps, see the short [Quick Start](DotNetQuickStart.md) instead.

> **A brief note on the two Popcorn versions.** Popcorn v1 through v7 (on NuGet as
> `Skyward.Api.Popcorn` and `.DotNetCore`) used **runtime reflection** to walk response
> objects and filter fields. That approach is incompatible with .NET's AOT compilation and IL
> trimming — two deployment features that are increasingly common in newer .NET stacks.
>
> **Popcorn v8** (this tutorial) is a rewrite on top of a **Roslyn source generator**. At build
> time the generator reads your `[JsonSerializable(typeof(ApiResponse<T>))]` declarations,
> walks the type graph, and emits a straight-line `JsonConverter<T>` per reachable type — no
> reflection at runtime, no metadata the trimmer can strip. The URL grammar, attribute
> semantics, and response envelope shape are unchanged; only the internals and extension-point
> API moved.
>
> Coming from v7? See the [v7 → v8 migration guide](../MigrationV7toV8.md) — most of the
> changes are find-and-replace.

## 1. Create a new ASP.NET Core project

```bash
dotnet new web -n PopcornDemo
cd PopcornDemo
```

We'll use minimal APIs — they compose cleanly with AOT publishing and reduce boilerplate. If
you prefer controllers, the same setup applies; `IPopcornAccessor` is injected the same way.

## 2. Install the Popcorn packages

```bash
dotnet add package Skyward.Api.Popcorn.SourceGen.Shared --version 8.0.0-preview.1
dotnet add package Skyward.Api.Popcorn.SourceGen        --version 8.0.0-preview.1
```

The csproj will look like:

```xml
<ItemGroup>
  <PackageReference Include="Skyward.Api.Popcorn.SourceGen.Shared" Version="8.0.0-preview.1" />
  <PackageReference Include="Skyward.Api.Popcorn.SourceGen"        Version="8.0.0-preview.1" PrivateAssets="all" />
</ItemGroup>
```

`SourceGen` is marked `developmentDependency` — it only contributes the Roslyn analyzer, never
a runtime DLL. `SourceGen.Shared` carries the attributes, envelopes, and middleware.

## 3. Define your models

Create a `Models` folder and add `Employee.cs` and `Car.cs`:

```csharp
namespace PopcornDemo.Models;

public class Employee
{
    public string FirstName { get; set; } = "";
    public string LastName  { get; set; } = "";

    public DateTimeOffset Birthday { get; set; }
    public int VacationDays { get; set; }

    public List<Car> Vehicles { get; set; } = new();
}

public class Car
{
    public string Make  { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }

    public Colors Color { get; set; }
}

public enum Colors { Black, Red, Blue, Gray, White, Yellow }
```

Unlike v7, there is no separate "projection class" step — Popcorn serializes your model
directly. You control what's exposed through attributes on the model itself (next step).

## 4. Add some test data

Add `ExampleContext.cs` in the same folder:

```csharp
namespace PopcornDemo.Models;

public class ExampleContext
{
    public List<Employee> Employees { get; }
    public List<Car>      Cars      { get; }

    public ExampleContext()
    {
        var firebird = new Car { Make = "Pontiac", Model = "Firebird", Year = 1981, Color = Colors.Blue };
        var ferrari  = new Car { Make = "Ferrari N.V.", Model = "250 GTO", Year = 1962, Color = Colors.Red };
        var cayman   = new Car { Make = "Porsche", Model = "Cayman", Year = 2005, Color = Colors.Yellow };

        var liz  = new Employee { FirstName = "Liz",  LastName = "Lemon",   Birthday = new DateTimeOffset(1981,5,1,0,0,0,TimeSpan.Zero), VacationDays = 0,   Vehicles = [firebird] };
        var jack = new Employee { FirstName = "Jack", LastName = "Donaghy", Birthday = new DateTimeOffset(1957,7,12,0,0,0,TimeSpan.Zero), VacationDays = 300, Vehicles = [ferrari, cayman] };

        Employees = [liz, jack];
        Cars = [firebird, ferrari, cayman];
    }
}
```

## 5. Tell Popcorn which types are response-shaped

Popcorn's generator discovers types through standard `System.Text.Json` `[JsonSerializable]`
attributes. Create `AppJsonSerializerContext.cs`:

```csharp
using System.Text.Json.Serialization;
using Popcorn.Shared;
using PopcornDemo.Models;

namespace PopcornDemo;

[JsonSerializable(typeof(ApiResponse<List<Employee>>))]
[JsonSerializable(typeof(ApiResponse<List<Car>>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
```

List top-level response types — the generator walks nested types (like `Car` inside
`Employee.Vehicles`) automatically.

## 6. Wire up `Program.cs`

Replace the generated `Program.cs` with:

```csharp
using System.Text.Json;
using Popcorn.Shared;
using PopcornDemo;
using PopcornDemo.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ExampleContext>();
builder.Services.AddPopcorn();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    o.SerializerOptions.AddPopcornOptions(); // installs generator-emitted converters
});

var app = builder.Build();
app.UsePopcornExceptionHandler(); // unhandled exceptions → ApiError envelope

app.MapGet("/employees", (IPopcornAccessor access, ExampleContext db) =>
    access.CreateResponse(db.Employees));

app.MapGet("/cars", (IPopcornAccessor access, ExampleContext db) =>
    access.CreateResponse(db.Cars));

app.Run();
```

Three things worth noting:

- **`AddPopcorn()`** registers the per-request `IPopcornAccessor` that parses `?include=`.
- **`AddPopcornOptions()`** is an extension emitted by the source generator; it installs one
  `JsonConverter<T>` per type reachable from your `JsonSerializerContext`.
- **`UsePopcornExceptionHandler()`** wraps unhandled exceptions in an `ApiResponse<T>` envelope
  with `Success = false` and a populated `ApiError`. See
  [the migration guide §6](../MigrationV7toV8.md#6-custom-response-envelope--error-handling)
  for custom envelope shapes.

`WebApplication.CreateSlimBuilder(args)` is the AOT-friendly host; if you don't plan to publish
as AOT you can use `WebApplication.CreateBuilder(args)` instead.

## 7. Run it

```bash
dotnet run
```

Call the endpoint with no `?include=`:

```
GET /employees
```

```json
{
  "Success": true,
  "Data": [
    {
      "FirstName": "Liz",
      "LastName": "Lemon",
      "Birthday": "1981-05-01T00:00:00+00:00",
      "VacationDays": 0,
      "Vehicles": [
        { "Make": "Pontiac", "Model": "Firebird", "Year": 1981, "Color": 2 }
      ]
    },
    { "FirstName": "Jack", "LastName": "Donaghy", ... }
  ]
}
```

The default is "everything" because we haven't applied any `[Default]` / `[Always]` attributes
yet — see the [Default Includes tutorial](DotNetTutorialDefaultIncludes.md) for how to change
that.

## 8. Start asking for specific fields

```
GET /employees?include=[FirstName,LastName]
```

```json
{
  "Success": true,
  "Data": [
    { "FirstName": "Liz",  "LastName": "Lemon"   },
    { "FirstName": "Jack", "LastName": "Donaghy" }
  ]
}
```

Nested includes work recursively:

```
GET /employees?include=[FirstName,Vehicles[Make]]
```

```json
{
  "Success": true,
  "Data": [
    { "FirstName": "Liz",  "Vehicles": [{ "Make": "Pontiac" }] },
    { "FirstName": "Jack", "Vehicles": [{ "Make": "Ferrari N.V." }, { "Make": "Porsche" }] }
  ]
}
```

This is the point of Popcorn: the client decides exactly which fields to transfer, the server
never materializes the rest.

## Where to go next

- [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md) — the full `?include=`
  grammar, including `!all`, `!default`, and negation via `-Field`.
- [Default Includes](DotNetTutorialDefaultIncludes.md) — `[Default]`, `[Always]`,
  `[SubPropertyDefault("[Make,Model]")]` — control what a bare `?include=` request returns.
- [Internal-Only Fields](DotNetTutorialInternalOnly.md) — `[Never]` for fields that must not
  leave the server regardless of what the client asks for.
- [Computed Fields](DotNetTutorialAdvancedProjections.md) — C# computed properties as the
  v8 replacement for v7's `Translate<T>(...)` lambdas.
- [Performance](../Performance.md) — benchmarked ratios vs raw `System.Text.Json` and v7.
- [`PopcornAotExample`](../../dotnet/PopcornAotExample/) — reference project that publishes
  with `PublishAot=True` + `PublishTrimmed=True`.
