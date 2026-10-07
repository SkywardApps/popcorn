# Project Brief: Popcorn

## Core Purpose
Popcorn is a communication protocol on top of RESTful APIs that lets clients specify — via an `include=[...]` query parameter — exactly which fields (including nested relationships and collections) to return. It reduces over-fetching, under-fetching, and round-trip counts while remaining plain REST/JSON.

## Current State (v8, on `master`)
The source-generator rewrite (formerly branch `spike/source-generator`) **merged to `master`** and shipped as **`8.0.0-preview.1`** (tag `release/v8.0.0-preview.1`, 2026-04-24). Two NuGet packages: `Skyward.Api.Popcorn.SourceGen` (analyzer) + `Skyward.Api.Popcorn.SourceGen.Shared` (runtime).

v8 replaces the legacy **runtime reflection** engine (v7: `PopcornNetStandard`, `PopcornNetStandard.WebApiCore`, NuGet `Skyward.Api.Popcorn`) with a **Roslyn source generator** (`Popcorn.SourceGenerator` + `Popcorn.Shared`) that emits `JsonConverter<T>` classes at build time. The v7 projects remain in the tree and on NuGet during a deprecation window; the two lines are side-by-side installable and wire-compatible.

### Why the rewrite
1. **Performance** — no runtime reflection on the hot serialization path; generated converters are straight code.
2. **AOT compilation** — reflection breaks `PublishAot=true`. Generated converters work under Native AOT (validated by `PopcornAotExample` + `aot-ci.yml`).
3. **Trimming** — `PublishTrimmed=True` strips reflection-discovered members. Generated code preserves the call graph.

## Protocol Requirements (stable across implementations)
- Selective field inclusion via `?include=[...]` query parameter.
- Recursive nested selection, collections.
- Special tokens: `!all`, `!default`.
- Negation via `-PropertyName` prefix.
- Attribute-driven default behavior: `[Always]`, `[Default]`, `[Never]`.
- Platform-agnostic spec; multiple provider implementations allowed.

## Success Metrics (all validated for 8.0.0-preview.1)
- Feature parity with the legacy engine for in-scope features — done; see `apiDesign.md` feature ledger for scope decisions.
- AOT + trimmed publish succeeds with no reflection warnings — enforced by `aot-ci.yml`.
- Throughput/allocation parity-or-better vs. standard `System.Text.Json` — confirmed and gated by `benchmarks.yml`; see `docs/Performance.md`.
- Generated converter output is readable and debuggable — inspectable under `$(BaseIntermediateOutputPath)Generated`.
