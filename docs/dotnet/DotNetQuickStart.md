# [Popcorn](../../README.md) > [Quick Start](../QuickStart.md) > DotNet

[Table Of Contents](../../docs/TableOfContents.md)

Drop Popcorn v8 into a minimal-API app in five minutes. For a full walkthrough (models, test
data, multiple endpoints) see [Getting Started](DotNetTutorialGettingStarted.md).

> **v7 vs v8.** This is the v8 (source-generator) path — it works under `PublishAot=True` and
> `PublishTrimmed=True`. If you're on the v7 `Skyward.Api.Popcorn` / `.DotNetCore` packages and
> aren't ready to migrate, the v7 quick start is in the [migration guide](../MigrationV7toV8.md).

## 1. Install the packages

```xml
<PackageReference Include="Skyward.Api.Popcorn.SourceGen.Shared" Version="8.0.0-preview.1" />
<PackageReference Include="Skyward.Api.Popcorn.SourceGen"        Version="8.0.0-preview.1" PrivateAssets="all" />
```

`SourceGen` is marked `developmentDependency` — it only contributes the Roslyn analyzer, never a
runtime DLL. `SourceGen.Shared` is the runtime library that carries attributes, envelopes, and
middleware.

## 2. Declare a `JsonSerializerContext`

Popcorn's generator discovers types through standard `System.Text.Json`
`[JsonSerializable]` attributes. Register each top-level response type — the generator walks
nested types automatically.

```csharp
using System.Text.Json.Serialization;
using Popcorn.Shared;

[JsonSerializable(typeof(ApiResponse<List<Car>>))]
[JsonSerializable(typeof(ApiResponse<Car>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
```

## 3. Wire up `Program.cs`

```csharp
using System.Text.Json;
using Popcorn.Shared;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddPopcorn();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    o.SerializerOptions.AddPopcornOptions(); // installs generator-emitted converters
});

var app = builder.Build();
app.UsePopcornExceptionHandler(); // unhandled exceptions → ApiError envelope

app.MapGet("/cars", (IPopcornAccessor access) =>
    access.CreateResponse(new List<Car>
    {
        new(1, "Pontiac", "Firebird", 1981),
        new(2, "Porsche", "Cayman",    2005),
    }));

app.Run();

public record Car(int Id, string Make, string Model, int Year);
```

`CreateSlimBuilder` is the AOT-friendly host builder; if you don't plan to publish as AOT you
can use `WebApplication.CreateBuilder(args)` instead.

## 4. Make a request

```
GET /cars
→ { "Success": true, "Data": [{ "Id":1, "Make":"Pontiac", "Model":"Firebird", "Year":1981 }, ...] }

GET /cars?include=[Make,Model]
→ { "Success": true, "Data": [{ "Make":"Pontiac", "Model":"Firebird" }, ...] }
```

## Where to go next

- [Getting Started](DotNetTutorialGettingStarted.md) for a fuller walkthrough with multiple
  models, default-include attributes, and nested sub-entity queries.
- [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md) for the full `?include=`
  grammar (negation, wildcards, nesting).
- [Default Includes](DotNetTutorialDefaultIncludes.md) for `[Default]` / `[Always]` /
  `[SubPropertyDefault]` attribute usage.
- [Performance](../Performance.md) for benchmarked ratios vs raw `System.Text.Json` and v7.
- [`dotnet/PopcornAotExample/`](../../dotnet/PopcornAotExample/) for a reference project that
  publishes with `PublishAot=True` + `PublishTrimmed=True`.
