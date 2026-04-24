# [Popcorn](../README.md) > Releases and Release Notes

[Table Of Contents](TableOfContents.md)

## 8.0.0-preview.1 (unreleased)

First public preview of v8 — Popcorn reimplemented as a Roslyn source generator for Native AOT and IL trimming compatibility. See [MigrationV7toV8.md](MigrationV7toV8.md) for the full migration story.

**New packages (side-by-side installable with v7):**
+ `Skyward.Api.Popcorn.SourceGen` — Roslyn analyzer, `developmentDependency`.
+ `Skyward.Api.Popcorn.SourceGen.Shared` — runtime attributes, envelopes (`ApiResponse<T>` / `Pop<T>` / `ApiError`), exception middleware (`UsePopcornExceptionHandler`), DI helpers.

**Highlights:**
+ Works under `PublishAot=True` and `PublishTrimmed=True`. No runtime reflection on the hot path.
+ Performance: beats legacy v7 reflection by 3–8× on "emit everything" shapes; ~5.8× on selective-fetch ComplexModelList. On nested complex data, v8 emitting everything is *faster* than raw `System.Text.Json` (0.87× time / 0.93× alloc). Full 3-way benchmark report under `benchmarks/results/v2-baseline/`.
+ Custom envelope + exception middleware shipped (`[PopcornEnvelope]` + `[PopcornPayload]` / `[PopcornError]` / `[PopcornSuccess]` markers + `UsePopcornExceptionHandler`).
+ `[SubPropertyDefault("[Make,Model]")]` attribute (v7's `[SubPropertyIncludeByDefault]`, renamed and generator-backed).
+ Generator diagnostics JSG003–JSG008 for malformed envelopes and AOT-incompatible polymorphic shapes.

**Breaking changes from v7** (abridged — see [MigrationV7toV8.md](MigrationV7toV8.md)):
+ Attribute renames: `[IncludeByDefault]` → `[Default]`, `[IncludeAlways]` → `[Always]`, `[InternalOnly]` → `[Never]`, `[SubPropertyIncludeByDefault]` → `[SubPropertyDefault]`.
+ Fluent-lambda config surface (`config.Map<T>()`, `.Translate<T>(...)`, `.Authorize<T>(...)`, `.SetContext(...)`, `.SetInspector(...)`) removed entirely. Type discovery moved to `[JsonSerializable(typeof(ApiResponse<T>))]` on a `JsonSerializerContext`. Exception wrapping moves to `UsePopcornExceptionHandler()` + a `[PopcornEnvelope]` type.
+ `?include=[...]` now matches the **wire name** (from `[JsonPropertyName]` or `JsonNamingPolicy`), not the C# identifier. Client-visible contract.
+ **Dropped features (permanent):** sorting, pagination, filtering, authorizers, `SetContext(dict)`, `MapEntityFramework<S,P,Ctx>`, `BlindHandler`, `[Translator]` lambdas. Each has a cleaner replacement documented in the migration guide.

---

## Major Release: 2.0.0
+ **Feature Additions:**
	+ Condensed our entire .NET offering into one .NET Standard project
	+ Enhanced the map ability so a single source type can be mapped to multiple destination types
	+ Added a default response inspector implementation
	+ Authorizers added as a configuration option to restrict access to certain objects as specified
+ **Bug Fixes:**
	+ Enabled the handling of polymorphism  in DefaultIncludes
	+ MapEntityFramework method allows for custom configurations without additional setup
+ **Maintenance**
	+ Documentation added: [Authorizers](dotnet/DotNetTutorialAuthorizers.md), [Factories](dotnet/DotNetTutorialAdvancedProjections.md) in Advanced Projections tutorial, 
	Response [Inspectors](dotnet/DotNetTutorialInspectors.md)
	+ Test additions and added CI to GitHub project

---
### Minor Release: 1.3.0
+ **Feature Additions:**  
    + Query parameter "sort" added to allow the sorting of responses based on a simple comparable property
		+ Query parameter "sortDirection" added to be used in conjunction with "sort" to specify ascending or descending sort order
    + Added [Sorting](dotnet/DotNetTutorialSorting.md) tutorial
+ **Maintenance:**
    + Test additions

---
### Minor Release: 1.2.0
+ **Feature Additions:**  
    + [IncludeByDefault] added as a property option for projections to allow users to set their default return properties in the projection itself.
    + Naming of [SubPropertyIncludeByDefault] updated
    + Added [DefaultIncludes](dotnet/DotNetTutorialDefaultIncludes.md) tutorial
+ **Maintenance:**
    + Test additions

---
#### Patch Release: 1.1.3
+ **Bug Fixes:**
	+ Adding a solution to allow nulls to be passed to an inspector
	+ Allowing spaces to be passed in an include request, i.e ?include=[property1, property2[subproperty1, subproperty2]]

---
#### Patch Release: 1.1.2
+ **Bug Fixes:**
	+ Got reference navigation properties working again.

---
#### Patch Release: 1.1.1
+ **Bug Fixes:**
	+ Allowed blind expansion to be accomplished when specifically designated

--- 
### Minor Release: 1.1.0
+ **Feature Additions:**  
	+ Added all initial documentation

---
## Major Release: 1.0.0
	+ Project inception!