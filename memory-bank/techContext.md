# Technical Context: Popcorn

## Development Environment

### Primary Technologies
- .NET Core / C# (targeting .NET 8.0+)
- Source Generators (Roslyn)
- System.Text.Json
- AOT Compilation Support
- Visual Studio / VS Code

### Build & CI/CD
- AppVeyor CI
- NuGet package deployment
- GitHub repository
- Gitter community chat

## Project Structure

### Core Components
```
dotnet/
├── Popcorn.sln
├── Popcorn.SourceGenerator/       # Source generator implementation
│   ├── ExpanderGenerator.cs       # Main generator logic
│   └── Plan.md                    # Improvement plan
├── Popcorn.Shared/                # Shared attributes and models
│   ├── ApiResponse.cs             # Core response type
│   ├── Pop.cs                     # Generic wrapper for data
│   ├── PopAttribute.cs            # Attribute definitions
│   ├── PopcornAccessor.cs         # HTTP context accessor
│   ├── PropertyReference.cs       # Property reference parsing
│   └── HttpContextExtensions.cs   # Extension methods
├── Popcorn.SourceGenerator.Test/  # Generator tests
│   ├── PopcornGeneratorSnapshotTests.cs  # Snapshot tests
│   └── TestHelper.cs              # Test utilities
└── PopcornAotExample/             # AOT-compatible example project
```

### Key Dependencies
1. **Runtime**
   - .NET 8.0+ (for AOT support)
   - System.Text.Json 9.0.1
   - Microsoft.CodeAnalysis (for source generation)
   - Microsoft.AspNetCore.Http.Abstractions 2.3.0
   - Microsoft.Extensions.DependencyInjection.Abstractions 9.0.1

2. **Development**
   - C# Latest (for source generator features)
   - Microsoft.CodeAnalysis.CSharp 4.12.0
   - Verify.SourceGenerators 2.2.0
   - Verify.Xunit 22.8.0
   - xUnit 2.6.6

## Technical Constraints

### Platform Requirements
- .NET Standard 2.0 compatibility
- HTTP/REST architecture
- JSON serialization
- Standard middleware pipeline

### Performance Targets
- Minimal overhead on API calls
- Efficient query generation
- Optimized memory usage
- Fast field parsing

## Source Generator Implementation

### Core Components
1. **ExpanderGenerator**
   - Implements IIncrementalGenerator
   - Finds classes with JsonSerializable attributes
   - Extracts target types
   - Generates JsonConverter classes

2. **Type Handling**
   - Primitive types (numbers, strings, booleans)
   - Complex objects
   - Collections
   - Nested types
   - Nullable types

3. **Attribute System**
   - AlwaysAttribute - Always include property
   - NeverAttribute - Never include property
   - DefaultAttribute - Include by default

4. **Property Reference System**
   - Parses include statements
   - Supports nested properties
   - Handles collections
   - Supports negation

### Improvement Plan
1. **Critical Issues**
   - Comprehensive test coverage
   - Circular reference detection
   - Thread safety in PopcornAccessor
   - Property reference validation
   - Attribute conflict detection

2. **Performance Improvements**
   - Property reference parsing optimization
   - Generated code optimization

3. **API Improvements**
   - Error state support
   - Deserialization support
   - XML documentation
   - Diagnostic message improvements

## Development Practices

### Code Standards
- C# coding conventions
- XML documentation
- Unit test coverage
- Performance benchmarking
- Code of conduct adherence
- Semantic versioning

### Testing Approach
- Snapshot testing with Verify
- Unit tests with xUnit
- Test helper utilities
- Test output helpers
- Diagnostic verification

### Contribution Process
1. **Initial Discussion**
   - Open issue for discussion
   - Get maintainer feedback
   - Align with roadmap

2. **Development**
   - Fork repository
   - Create feature branch
   - Follow coding standards
   - Add/update tests
   - Update documentation

3. **Pull Request**
   - Remove build dependencies
   - Update documentation
   - Update version numbers
   - Get maintainer sign-off

4. **Review**
   - Code review feedback
   - CI/CD checks
   - Documentation review
   - Test coverage verification

### Version Control
- Git workflow
- Semantic versioning
- Pull request reviews
- CI/CD integration

### Documentation
- XML API documentation
- Markdown for guides
- Example projects
- Integration tutorials

## Deployment

### Package Distribution
- NuGet packages
- Versioned releases
- Release notes
- Migration guides

### Integration
- Middleware configuration
- DI container setup
- Options pattern usage
- Logging integration

## Monitoring & Maintenance

### Diagnostics
- Performance metrics
- Error logging
- Usage statistics
- Debug information

### Support
- GitHub issues
- Gitter community
- Documentation updates
- Security patches
