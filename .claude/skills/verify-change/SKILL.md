---
name: verify-change
description: Verify a Popcorn code change end-to-end - build, run both test suites, inspect generated converter output, and (for generator/runtime changes) exercise the AOT example. Use after any change to Popcorn.SourceGenerator, Popcorn.Shared, or test models.
---

# Verify a Popcorn change

Run from the repo root. All commands are plain `dotnet` CLI — usable by any agent.

## 1. Build

```bash
cd dotnet && dotnet build Popcorn.sln
```

Zero errors required. Also watch for **new warnings in generated code** (CS86xx nullability warnings in `*.g.cs`): the baseline is zero; any new one is a generator regression even if the build succeeds.

## 2. Tests

```bash
dotnet test dotnet/Tests/Popcorn.FunctionalTests
dotnet test dotnet/Tests/Popcorn.SourceGenerator.Tests
```

Expected: **182 passed / 2 skipped / 0 failed** and **19 passed**. The 2 skips are `[JsonDerivedType]` polymorphism dispatch — any other skip or failure is a regression. If you added tests, update the expected counts here, in `AGENTS.md`, and in `memory-bank/progress.md`.

## 3. Inspect generated output (generator changes only)

Generated converters land under the consuming project's intermediate output, e.g.
`dotnet/Tests/Popcorn.FunctionalTests/obj/Generated/Popcorn.SourceGenerator/...`.
Read the emitted `*JsonConverter.g.cs` / `RegisterConverters.g.cs` and confirm the change appears as intended. Before emitting `Pop<T>` type names or touching `allTypeNames`, re-read `memory-bank/systemPatterns.md` § "Load-bearing generator conventions".

## 4. AOT smoke (generator or runtime-library changes)

```bash
dotnet publish dotnet/PopcornAotExample -c Release
```

Must publish with no AOT/trim analyzer warnings. For full end-to-end (what `aot-ci.yml` does): build `dotnet/PopcornAotExample/Dockerfile` with context `dotnet/`, run it on port 8080, and check `/todos`, `/null`, `/sub`, and `/boom` (expects 500 + `Ok:false` + populated `Problem`).

## 5. Performance-sensitive changes

If the change touches emitted serialization code, run the benchmark gate locally — see the `benchmark` skill (`.claude/skills/benchmark/SKILL.md`).
