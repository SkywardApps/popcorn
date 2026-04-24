# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Documentation Updates

[Table Of Contents](../TableOfContents.md)

> Historical planning document from the v7 → v8 documentation overhaul. The work it describes
> has shipped: core tutorials now target v8 source-generator patterns, dropped-feature
> tutorials carry top-of-file deprecation banners pointing at the v8 replacement, and the
> migration guide ([MigrationV7toV8.md](../MigrationV7toV8.md)) is the primary upgrade path.
>
> For the current documentation state, start from the [Table of Contents](../TableOfContents.md).
> For what's coming next, see the [Roadmap](../../roadmap.md).

## What shipped

- New v8-focused tutorials: [DotNet Quick Start](DotNetQuickStart.md),
  [Getting Started](DotNetTutorialGettingStarted.md),
  [Default Includes](DotNetTutorialDefaultIncludes.md),
  [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md),
  [Wildcard Includes](DotNetTutorialWildcardIncludes.md),
  [Internal Only](DotNetTutorialInternalOnly.md),
  [Lazy Loading](DotNetTutorialLazyLoading.md),
  [Advanced Projections](DotNetTutorialAdvancedProjections.md).
- [Migration guide](../MigrationV7toV8.md) covering every breaking change.
- [Performance page](../Performance.md) with benchmarked ratios vs raw STJ and v7 reflection.
- v7-only tutorials carry deprecation banners and survive as reference material for users still
  on the reflection line.
