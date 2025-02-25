# Projects

There are a number of projects under this solution. This is a quick summary of the relationships.

* Core Implementation
  * Popcorn.Shared - Core attributes and models shared across all projects
  * Popcorn.SourceGenerator - Source generator implementation for build-time code generation
  * PopcornNetStandard - Legacy implementation code shared between all .NET versions (being migrated to source generation)
  * PopcornNetStandard.WebApiCore - Middleware for projects using WebApiCore (being migrated to source generation)

* Examples
  * PopcornAotExample - Example implementation using source generation with AOT/trimming support
  * PopcornNet5Example - Example implementation using .NET 5
  * ExampleModel - Data models shared across projects

* Test
  * Popcorn.SourceGenerator.Test - Tests for the source generator implementation using snapshot testing
  * PopcornSpecTests - Tests for core protocol specification compliance
  * CommonIntegrationTests - Integration tests written to be largely technology independent
  * PopcornNetCoreExampleIntegrationTests - Integration tests for .NET Core examples

* Benchmarks
  * ParsingIncludes - Performance benchmarks for include parsing implementations
