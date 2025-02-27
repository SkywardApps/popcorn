# Progress Tracking: Popcorn

## Version History

### Major Release: 2.0.0
✅ Feature Additions
- Condensed .NET offering into one .NET Standard project
- Enhanced mapping for multiple destination types
- Added default response inspector
- Added authorizers for access control

✅ Bug Fixes
- Fixed polymorphism in DefaultIncludes
- Improved MapEntityFramework customization

### Minor Releases
- 1.3.0: Added sorting capabilities
- 1.2.0: Added IncludeByDefault property option
- 1.1.0: Added initial documentation
- 1.0.0: Project inception

## Current Status

### What Works
✅ Source Generator Core
- Basic field serialization
- Attribute-based control (Always, Never, Default)
- JsonPropertyName support
- Null handling
- Collection support
- Nested type handling

✅ AOT Support
- Native compilation
- Trimming support
- Docker containerization
- Performance optimization

✅ Basic Features
- Field selection syntax
- Property references
- Default field behavior
- Basic error handling

### Missing Features
❌ Source Generator Improvements
- Comprehensive test coverage
- Circular reference detection
- Thread safety in PopcornAccessor
- Property reference validation
- Attribute conflict detection
- Property reference parsing optimization
- Generated code optimization
- Error state support
- Deserialization support
- XML documentation
- Diagnostic message improvements

❌ Advanced Features
- Sorting support
- Pagination
- Filtering
- Authorization system
- Response inspectors
- Contexts
- Lazy loading
- Blind expansion

❌ Technical Features
- Circular reference handling
- Complex polymorphic types
- Advanced error messages
- Performance optimizations

### In Development
🔨 Source Generator Improvements
- Detailed improvement plan created (see Plan.md)
- Prioritization of improvements established
- Test infrastructure in place
- Comprehensive test plan created for serialization functionality
- Fixed hardcoded JsonSerializerContext reference in ExpanderGenerator
- Added test for selective property inclusion
- Implemented tests for include parameter variations (case sensitivity, property negation)
- Added test plan for ASP.NET Core Web API return types

🔨 Documentation Improvements
- Updated documentation to clarify include parameter syntax
- Added detailed explanation of special references (!all, !default)
- Added documentation for property negation using the "-" prefix

🔨 Schema Generation
- Design phase
- Prototype implementation
- Integration planning

🔨 Documentation Generator
- Requirements gathering
- Architecture design
- Integration research

🔨 Performance Optimization
- Query analysis
- Benchmarking
- Optimization strategies

## Remaining Work

### Short Term
📋 Source Generator Improvements
- Implement comprehensive test coverage
- Add circular reference detection
- Fix thread safety in PopcornAccessor
- Implement property reference validation
- Add attribute conflict detection
- Implement tests for ASP.NET Core result types

📋 Schema & Documentation
- Complete schema generation system
- Implement documentation generator
- Integrate with existing tools
- Create usage examples

📋 Core Features
- Implement payload containers
- Add pagination support
- Design filtering system
- Enhance error handling

### Medium Term
📋 Platform Support
- PHP provider implementation
- .NET Framework provider
- TypeScript client library
- JavaScript client library

📋 Advanced Features
- Calculated properties
- Advanced filtering
- Batch operations
- Caching system

## Known Issues

### Technical Debt
⚠️ Source Generator
- Circular reference detection missing
- Thread safety issues in PopcornAccessor
- Property reference validation needed
- Attribute conflict detection missing
- Performance optimizations needed

⚠️ Query Optimization
- Complex nested queries need optimization
- Memory usage in large result sets
- Cache implementation needed

⚠️ Error Handling
- Improve error messages
- Add validation details
- Enhance debugging info

### Limitations
⚠️ Current
- No header-based field selection
- URL length limitations
- Basic authorization system
- Limited filtering capabilities

⚠️ Platform
- .NET Core only
- No client libraries
- Limited tooling
- Basic documentation

## Next Steps

### Immediate Priority
1. Implement comprehensive test coverage for source generator
2. Add circular reference detection
3. Fix thread safety in PopcornAccessor
4. Implement property reference validation
5. Add attribute conflict detection

### Future Considerations
1. Client library development
2. Advanced filtering system
3. Caching implementation
4. Additional platform support

## Success Metrics
📊 **Performance**
- Query execution time
- Memory usage
- Response size
- Cache hit rate

📊 **Adoption**
- GitHub stars
- NuGet downloads
- Community engagement
- Provider implementations
