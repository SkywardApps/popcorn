# Active Context: Popcorn

## Current State
On `master`. The source-generator spike merged and shipped as **`8.0.0-preview.1`** (tag `release/v8.0.0-preview.1`, 2026-04-24, commit `8249905`). Both v8 packages are on NuGet. Docs under `docs/` were refreshed for v8 in the same release commit (v7-only tutorials carry deprecation banners).

**Mission (achieved, now in maintenance)**: Popcorn runs under Native AOT (`PublishAot=True`) and IL trimming with no runtime reflection on the hot path, at parity-or-better performance vs. raw `System.Text.Json`.

## Guardrails (keep these green)
- `tests.yml` — `dotnet test` on `Popcorn.FunctionalTests` (182 passing / 2 skipped) + `Popcorn.SourceGenerator.Tests` (19 passing), PR + push.
- `aot-ci.yml` — builds `PopcornAotExample` Dockerfile, runs the container, asserts 4 endpoints (incl. the exception-middleware `/boom` path). The canary for the AOT thesis.
- `benchmarks.yml` — relative-performance gate; 3 Popcorn/STJ ratios vs `benchmarks/results/ci-baseline.json`, fails on >25% regression. When a real optimization or intentional trade ships, update the baseline in the same PR.
- `main.yml` — release: tag `release/vX.Y.Z` → pack with `-p:Version` → NuGet push → GitHub release → best-effort csproj version sync back to master.

## Open Work (post-8.0.0-preview.1)
Tracked in [roadmap.md](../roadmap.md). Highlights:
1. **v7 deprecation window** — decide when to add a `<PackageReleaseNotes>` banner to v7 and later remove `PopcornNetStandard*` from the tree.
2. **Polymorphism dispatch via `[JsonDerivedType]`** — 2 skipped tests in `PolymorphismTests.cs`; defer unless a consumer blocks on it.
3. **Header-based include (`POPCORN-INCLUDE`)** — spec'd in `apiDesign.md`, ~1 day; ship opportunistically.
4. **Deferred-quality items + perf levers** — see roadmap sections "Deferred-quality items" and "Remaining performance levers".

## Known Issues
None open. Historical generator bugs and their fixes: `progress.md` § "Generator pitfall ledger"; the load-bearing conventions that prevent their recurrence: `systemPatterns.md` § "Load-bearing generator conventions".

## Milestones (most recent first)
- `8249905` (2026-04-24) Release workflow + v8 docs refresh; tagged `release/v8.0.0-preview.1`. **Spike merged to master.**
- `9db29a4`/`85f965f` Matrix benchmark tool (.NET 8/9/10 × JIT/AOT local investigation) under `benchmarks/matrix/`.
- `07bb01d` Relative-performance CI gate (`benchmarks.yml`).
- `4c5587d` / `2853a5d` Tests CI + AOT CI workflows.
- `6c437cb`/`ad4650c` v8 NuGet packaging (two-package design, SourceLink, snupkg).
- `be55b65`/`efb05b9` Tier-2 scope cleared: `[Translator]`+DI, `IPopcornBlindHandler`, `[ExpandFrom]` all dropped with documented replacements (`docs/MigrationV7toV8.md` §5/§7/§8).
- `a1c6078` JSG008 diagnostic + v7→v8 migration guide.
- `03ff6a5` Three generator serialization optimizations (LINQ→for, flag hoisting, HashSet elision).
- `c2f99e6` `[SubPropertyDefault]` — Tier-1 complete.

## Non-Goals
- Deserialization / two-way serialization.
- Multi-platform providers (PHP, JS client) — protocol-only concern.
- Schema / OpenAPI generation.
