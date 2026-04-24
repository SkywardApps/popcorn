# Table of Contents
## [Popcorn](../README.md)

+ ### [Quick Start](QuickStart.md)

+ ### [Protocol / General Usage](Documentation.md)
  + #### [Declaring Included Fields](Documentation.md#includedFields)
  + #### [Including Fields](Documentation.md#includingFields)
  + #### [Including Sub-Entities](Documentation.md#includingSubEntities)
  + #### [Including Collections](Documentation.md#includingCollections)
  + #### [Default Fields](Documentation.md#defaultFields)
  + #### [Methods](Documentation.md#methods)
  + #### [Moving Forward](Documentation.md#movingForward)

+ ### [.NET implementation](dotnet/DotNetDocumentation.md)
  + #### [Quick Start](dotnet/DotNetQuickStart.md)
  + #### [Getting Started](dotnet/DotNetTutorialGettingStarted.md)
  + #### [Include Parameter Syntax](dotnet/DotNetTutorialIncludeParameterSyntax.md)
  + #### [Wildcard Includes](dotnet/DotNetTutorialWildcardIncludes.md)
  + #### [Default Includes](dotnet/DotNetTutorialDefaultIncludes.md)
  + #### [Internal-Only Fields (`[Never]`)](dotnet/DotNetTutorialInternalOnly.md)
  + #### [Computed Fields & Factories](dotnet/DotNetTutorialAdvancedProjections.md)
  + #### [Lazy Loading](dotnet/DotNetTutorialLazyLoading.md)

+ ### [Performance](Performance.md)

+ ### [Migrating from v7 to v8](MigrationV7toV8.md)

+ ### [Roadmap](../roadmap.md)

+ ### [Releases and Release Notes](Releases.md)

+ ### [Contributing](Contributing.md)

+ ### <a href="https://github.com/SkywardApps/popcorn/blob/master/LICENSE">License</a>

+ ### [Meet the Maintainers](Maintainers.md)

## v7-only tutorials (legacy reflection engine)

These describe features that exist only in the v7 runtime-reflection line (`Skyward.Api.Popcorn`).
They have been replaced in v8 by patterns native to ASP.NET Core + System.Text.Json. The
[migration guide](MigrationV7toV8.md) documents the v8 equivalent for each.

+ [Authorizers](dotnet/DotNetTutorialAuthorizers.md) — v8: ASP.NET Core authorization middleware.
+ [Sorting](dotnet/DotNetTutorialSorting.md) — v8: endpoint-layer sort, not a framework feature.
+ [Inspectors](dotnet/DotNetTutorialInspectors.md) — v8: `[PopcornEnvelope]` type + `UsePopcornExceptionHandler`.
+ [Contexts](dotnet/DotNetTutorialContexts.md) — v8: ASP.NET Core DI.
+ [Expand From](dotnet/DotNetTutorialExpandFrom.md) — v8: `[Never]` on source + hand factory, or Mapster.
+ [Blind Expansion](dotnet/DotNetTutorialBlindExpansion.md) — v8: standard `JsonConverter<T>`.
