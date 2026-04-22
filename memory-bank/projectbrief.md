# Project Brief: Popcorn

## Core Purpose
Popcorn is a communication protocol on top of RESTful APIs that lets clients specify — via an `include=[...]` query parameter — exactly which fields (including nested relationships and collections) to return. It reduces over-fetching, under-fetching, and round-trip counts while remaining plain REST/JSON.

## Current Branch: `spike/source-generator`
This branch is a **spike**, not yet merged to `master`. Its purpose: migrate Popcorn's .NET implementation from the legacy **runtime reflection** engine (`PopcornNetStandard`, `PopcornNetStandard.WebApiCore`) to a **Roslyn source generator** (`Popcorn.SourceGenerator` + `Popcorn.Shared`) that emits `JsonConverter<T>` classes at build time.

### Why the migration
1. **Performance** — no runtime reflection on the hot serialization path; generated converters are straight code.
2. **AOT compilation** — reflection-heavy code breaks `PublishAot=true`. Source-generated converters work under Native AOT. Validated end-to-end in `PopcornAotExample` (`WebApplication.CreateSlimBuilder`, `PublishAot=True`).
3. **Trimming / stripping** — `PublishTrimmed=True` strips unreferenced members; reflection-based discovery gets cut. Generated code preserves the call graph so the linker keeps what's needed.

### Migration status (spike branch)
- Core generator works: scans `JsonSerializerContext` subclasses for `[JsonSerializable(typeof(ApiResponse<T>))]`, walks referenced types, emits `<Type>JsonConverter.g.cs` + a `PopcornJsonOptionsExtension.AddPopcornOptions()` registration extension.
- Functional test suite exists under `dotnet/Tests/Popcorn.FunctionalTests` covering primitives, value types, collections, dictionaries, nesting, attribute semantics (`[Always]` / `[Default]` / `[Never]`), include-parameter variations, circular references.
- BenchmarkDotNet suite exists under `dotnet/benchmarks/SerializationPerformance` comparing standard `System.Text.Json` vs Popcorn across include strategies, scalability (flat + deep), circular-ref overhead, attribute processing.
- Known gaps vs. legacy reflection engine (not yet ported): sorting, pagination, filtering, authorization, response inspectors, contexts, lazy loading, blind expansion, deserialization.

## Protocol Requirements (stable across implementations)
- Selective field inclusion via `?include=[...]` query parameter.
- Recursive nested selection, collections.
- Special tokens: `!all`, `!default`.
- Negation via `-PropertyName` prefix.
- Attribute-driven default behavior: `[Always]`, `[Default]`, `[Never]`.
- Platform-agnostic spec; multiple provider implementations allowed.

## Success Metrics
- Feature parity with legacy reflection engine for the features that are still in scope.
- AOT + trimmed publish succeeds with no reflection warnings.
- Measured throughput/allocation parity-or-better vs. standard `System.Text.Json`.
- Generated converter output is readable and debuggable.
