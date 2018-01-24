# Projects

There are a number of projects under this solution.  This is a quick summary of the relationships

* Popcorn - All implementation code
  * PopcornNetStandard - Implementation code that can be shared between all .NET versions
  * PopcornNetStandard.EntityFrameworkCore - Lazy loading and helper utilities for projects using EntityFrameworkCore
  * PopcornNetStandard.WebApiCore - Middleware for projects using WebApiCore
  * PopcornNetFramework.WebApi - Specific utilities and hooks for Net Framework projects using Web Api 2 (ASP.NET).

* Examples - References that integrate popcorn into example projects
  * ExampleModel - Data models that are shared across projects (to avoid copy-paste, and enable quicker testing)
  * PopcornNetCoreExample - An example implementation using EntityFrameworkCore (sqlite) and WebApiCore on DotNet Core
  * PopcornNetFrameworkExample - An example implementation using Web Api (ASP.NET) on .Net Framework

* Test - All tests for the projects
  * PopcornNetStandardTests - Tests that target only the base implementation in .NET standard that is common.
  * CommonIntegrationTests - Integration tests written to be largely technology independent, but that need to be used in a technology-dependent environment.
  * PopcornNetCoreExampleIntegrationTests - Thin wrapper applying the CommonIntegrationTests to the PopcornNetCoreExample project.
  * PopcornNetFrameworkExampleIntegrationTests - Thin wrapper applying the CommonIntegrationTests to the PopcornNetFrameworkExample project.