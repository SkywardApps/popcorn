# Projects

There are a number of projects under this solution. This is a quick summary of the relationships.

* Core Implementation (v8, source generator)
  * Popcorn.Shared - Runtime library: attributes, `ApiResponse<T>`/`Pop<T>`/`ApiError` envelopes, include parser, `IPopcornAccessor`, `UsePopcornExceptionHandler` middleware. Ships as `Skyward.Api.Popcorn.SourceGen.Shared`.
  * Popcorn.SourceGenerator - Roslyn incremental generator that emits `JsonConverter<T>` classes at build time. Ships as `Skyward.Api.Popcorn.SourceGen` (analyzer-only).

* Legacy Implementation (v7, runtime reflection — kept during the deprecation window, no longer packed by CI)
  * PopcornNetStandard - Legacy reflection-based expander.
  * PopcornNetStandard.WebApiCore - Legacy middleware (`ExpandResultAttribute`, `ExpandServiceFilter`).

* Examples
  * PopcornAotExample - Canonical v8 example: minimal API, `CreateSlimBuilder`, `PublishAot=True`, `PublishTrimmed=True`, Dockerfile. Exercised end-to-end by the `aot-ci.yml` workflow.
  * Examples/PopcornNet5Example - Legacy v7 example (.NET 5). Slated for port-to-v8 or deletion (see roadmap).
  * Examples/ExampleModel - Data models shared across legacy examples.

* Tests
  * Tests/Popcorn.FunctionalTests - Main v8 suite (xUnit, 184 tests): exercises real generated converters via `TestJsonContext` + `TestServer`.
  * Tests/Popcorn.SourceGenerator.Tests - Generator-driver tests for the JSG003–JSG008 diagnostics.
  * Tests/PopcornSpecTests - Core protocol specification compliance.

* Benchmarks
  * benchmarks/SerializationPerformance - 3-way BenchmarkDotNet suite (raw STJ vs Popcorn v8 vs legacy v7); its `ci` subset feeds the `benchmarks.yml` regression gate.
  * benchmarks/ParsingIncludes - Include-string parser micro-benchmarks.
