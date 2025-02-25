# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Documentation Updates

[Table Of Contents](../TableOfContents.md)

## Documentation Review Plan

### Outdated Examples to Update
1. Runtime Mapping Configuration
   - DotNetQuickStart.md - Update to show source generator approach
   - DotNetTutorialGettingStarted.md - Revise for compile-time pattern
   - DotNetTutorialAdvancedProjections.md - Update factory patterns
   - DotNetTutorialBlindExpansion.md - Consider deprecation or revision

### New Patterns to Document
1. Source Generator Attributes
   - `[Pop]` attribute usage and configuration
   - Property inclusion/exclusion rules
   - Default value handling
   - Inheritance scenarios

2. Performance Considerations
   - Compile-time vs runtime tradeoffs
   - Memory allocation patterns
   - AOT compilation support
   - Native compilation scenarios

3. Best Practices
   - When to use source generation vs runtime
   - Project organization with generated code
   - Debugging generated expansions
   - Error handling and diagnostics

## Implementation Timeline

### Phase 1: Legacy Documentation Updates
1. Add notices to existing tutorials about runtime vs compile-time approaches
2. Update examples to show both patterns where applicable
3. Mark deprecated patterns and provide migration paths
4. Review and update all code samples for accuracy

### Phase 2: New Documentation Creation
1. Source Generator Core Documentation
   - Architecture overview
   - Implementation details
   - Configuration options
   - Performance characteristics

2. Migration Guide
   - Step-by-step migration process
   - Common pitfalls and solutions
   - Testing strategies
   - Validation approaches

3. New Features Documentation
   - AOT compilation support
   - Native compilation scenarios
   - Performance optimization guides
   - Tooling integration

### Phase 3: Example Updates
1. Create new example projects
   - Basic source generator usage
   - Advanced scenarios
   - Performance benchmarks
   - Migration examples

2. Update existing examples
   - Add source generator versions
   - Include performance comparisons
   - Document tradeoffs

## Priority Updates

1. Quick Start Guide
   - New getting started path focusing on source generation
   - Clear explanation of benefits
   - Simple, complete example
   - Common pitfalls to avoid

2. Core Concepts
   - Shift focus to compile-time expansion
   - Explain attribute-based configuration
   - Document type handling
   - Cover error scenarios

3. Advanced Features
   - Complex type scenarios
   - Custom expansion rules
   - Integration patterns
   - Performance optimization

## Conclusion

This documentation update plan aims to:
1. Preserve valuable existing content while clearly marking legacy approaches
2. Introduce new source generator patterns and best practices
3. Provide clear migration paths for existing users
4. Emphasize performance and maintainability improvements

Progress will be tracked in the repository's issues, with each documentation update tied to specific milestones. Community feedback and contributions will be actively incorporated throughout the process.
