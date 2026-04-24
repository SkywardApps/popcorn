# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > DotNet

[Table Of Contents](../../docs/TableOfContents.md)

Popcorn's .NET provider ships as two NuGet packages:

- `Skyward.Api.Popcorn.SourceGen.Shared` — runtime attributes (`[Default]` / `[Always]` / `[Never]`
  / `[SubPropertyDefault]`), envelopes (`ApiResponse<T>`, `Pop<T>`, `ApiError`), DI helpers,
  exception middleware.
- `Skyward.Api.Popcorn.SourceGen` — Roslyn source generator (analyzer-only, no runtime DLL).

Both target .NET 8+ and are compatible with `PublishAot=True` and `PublishTrimmed=True`. Current
version: `8.0.0-preview.1` (see [Releases](../Releases.md)).

> **Coming from v7?** The v7 packages (`Skyward.Api.Popcorn` and `Skyward.Api.Popcorn.DotNetCore`)
> are the runtime-reflection line — still on NuGet, still maintained, but cannot AOT-publish.
> See [Migrating from v7 to v8](../MigrationV7toV8.md) for the upgrade path.

## Tutorials (v8)

+ [Quick Start](DotNetQuickStart.md) — install the packages, wire up DI, make your first request.
+ [Getting Started](DotNetTutorialGettingStarted.md) — full walkthrough of a minimal API, models, and include queries.
+ [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md) — the `?include=[...]` grammar in detail.
+ [Wildcard Includes](DotNetTutorialWildcardIncludes.md) — `!all` / `!default` / negation.
+ [Default Includes](DotNetTutorialDefaultIncludes.md) — `[Default]` / `[Always]` / `[SubPropertyDefault]`.
+ [Internal-Only Fields](DotNetTutorialInternalOnly.md) — `[Never]` for fields that must not leave the server.
+ [Computed Fields](DotNetTutorialAdvancedProjections.md) — calculated properties and endpoint-side resolution.
+ [Lazy Loading](DotNetTutorialLazyLoading.md) — why v8 supports lazy loading by construction.

## v7-only tutorials (reflection line)

Tutorials for v7 features that do not exist in v8 — each carries a deprecation banner pointing
at the v8 replacement. Listed for v7 users still on the runtime-reflection line:

+ [Authorizers](DotNetTutorialAuthorizers.md) → ASP.NET Core authorization middleware in v8.
+ [Sorting](DotNetTutorialSorting.md) → endpoint-layer sort in v8.
+ [Inspectors](DotNetTutorialInspectors.md) → `[PopcornEnvelope]` + `UsePopcornExceptionHandler` in v8.
+ [Contexts](DotNetTutorialContexts.md) → ASP.NET Core DI in v8.
+ [Expand From](DotNetTutorialExpandFrom.md) → `[Never]` / hand factory / Mapster in v8.
+ [Blind Expansion](DotNetTutorialBlindExpansion.md) → automatic in v8 for own types; `JsonConverter<T>` for external types.

## Example projects

+ [`dotnet/PopcornAotExample/`](../../dotnet/PopcornAotExample/) — minimal API published with
  `PublishAot=True` + `PublishTrimmed=True`. Exercises the `[PopcornEnvelope]` + exception middleware path.
+ [`dotnet/Examples/PopcornNet5Example/`](../../dotnet/Examples/PopcornNet5Example/) — legacy v7 example (scheduled for refresh or removal).
