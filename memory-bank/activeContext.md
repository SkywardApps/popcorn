# Active Context: Popcorn

## Current Focus

### Primary Objectives
1. **.NET Source Generation Migration**
   - Move from runtime reflection to build-time generation
   - Support AOT compilation and trimming
   - Optimize performance
   - Maintain feature parity

2. **Source Generator Improvements**
   - Implement comprehensive test coverage
   - Add circular reference detection
   - Fix thread safety in PopcornAccessor
   - Implement property reference validation
   - Add attribute conflict detection
   - Optimize property reference parsing
   - Optimize generated code
   - Add error state support
   - Implement deserialization support
   - Improve XML documentation
   - Enhance diagnostic messages

3. **Feature Parity Goals**
   - Implement sorting support
   - Add pagination capabilities
   - Support filtering
   - Re-implement authorization system
   - Add response inspectors
   - Support contexts
   - Enable lazy loading
   - Implement blind expansion

## Recent Changes

### Completed Work
- Core protocol specification
- .NET Core implementation
- Basic documentation
- CI/CD pipeline
- NuGet package deployment
- Basic source generator implementation
- AOT support
- Basic field serialization
- Attribute-based control (Always, Never, Default)
- JsonPropertyName support
- Null handling
- Collection support
- Nested type handling
- Fixed hardcoded JsonSerializerContext reference in ExpanderGenerator
- Created comprehensive test plan for serialization functionality
- Added test for selective property inclusion
- Implemented tests for include parameter variations (case sensitivity, property negation)
- Updated documentation to clarify include parameter syntax, special references (!all, !default), and property negation

### In Progress
- Source generator improvements (see Plan.md)
- Schema generation design
- Documentation improvements
- Performance optimizations
- Additional provider planning

## Active Decisions

### Technical Considerations
1. **Source Generator Improvement Prioritization**
   - Comprehensive test coverage should be the first priority
   - Circular reference detection is needed to prevent infinite recursion
   - Thread safety in PopcornAccessor is critical for high-concurrency scenarios
   - Property reference validation is needed to prevent malformed inputs

2. **Schema Format**
   - OpenAPI/Swagger integration
   - Custom schema format
   - Documentation generation approach

3. **Provider Architecture**
   - Common code sharing
   - Platform-specific optimizations
   - Integration patterns

4. **Feature Priorities**
   - Pagination implementation
   - Filtering design
   - Authorization improvements

### Open Questions
- Schema generation approach
- PHP provider architecture
- Client library design
- Filtering syntax

## Next Steps

### Immediate Actions
1. Implement comprehensive test coverage for source generator
2. Add circular reference detection to prevent infinite recursion
3. Fix thread safety in PopcornAccessor
4. Implement property reference validation
5. Add attribute conflict detection

### Short-term Goals
1. Complete schema generation
2. Release documentation generator
3. Implement payload containers
4. Add pagination support

### Medium-term Goals
1. Release PHP provider
2. Develop client libraries
3. Add filtering capabilities
4. Enhance authorization system

## Current Challenges

### Technical Challenges
- Schema complexity
- Cross-platform compatibility
- Performance optimization
- Authorization edge cases
- Circular reference detection
- Thread safety in high-concurrency scenarios
- Property reference validation
- Attribute conflict detection

### Project Challenges
- Documentation maintenance
- Community growth
- Provider implementation time
- Resource allocation

## Development Guidelines

### Problem Resolution Approach
- When encountering repeated issues, stop and analyze the problem
- Do not continue trying the same approach multiple times
- Present multiple potential solutions with confidence levels
- Seek clarification when facing unclear errors or unexpected behavior
- Document issues and their resolutions for future reference

### Testing Approach
- Start with the simplest possible test case
- Verify each step works before proceeding to the next
- Use incremental development to minimize potential issues
- Thoroughly document test assumptions and requirements
- Ensure tests are reliable and deterministic

## Team Focus
- Schema design and implementation
- Documentation improvements
- Provider development
- Community support
- Source generator improvements

## Project Maintenance

### Core Team
- Maintained by Skyward App Company
- Contact: popcorn@skywardapps.com
- GitHub issue tracking preferred

### Contribution Guidelines
- Discuss changes via issues first
- Follow code of conduct
- Update documentation
- Include tests
- Follow semantic versioning
- Submit pull requests for review

### Review Process
1. Remove build dependencies
2. Update documentation
3. Update version numbers
4. Obtain maintainer sign-off
