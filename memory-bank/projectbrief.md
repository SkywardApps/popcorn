# Project Brief: Popcorn

## Core Purpose
Popcorn is a communication protocol that extends RESTful APIs to enable selective field inclusion in API responses. It allows clients to specify exactly which fields they want to receive, including nested relationships, reducing unnecessary data transfer and API calls.

## Key Requirements

### Protocol Requirements
- Support selective field inclusion via query parameters
- Enable recursive field selection for nested entities
- Allow collection handling with field selection
- Maintain RESTful principles
- Support standard HTTP methods (GET, POST, PUT, etc.)

### Technical Requirements
- Platform-agnostic protocol specification
- Extensible architecture for multiple implementations
- Consistent field naming conventions
- Default field handling
- Performance optimization capabilities
- AOT compilation support
- Source generation for build-time optimization

### Implementation Requirements
- Provider implementations must be fully documented
- Backward compatibility maintenance
- Comprehensive test coverage
- Clear error handling and validation

## Project Goals

### Short Term
1. Improve source generator implementation
   - Add comprehensive test coverage
   - Implement circular reference detection
   - Fix thread safety issues
   - Add property reference validation
   - Implement attribute conflict detection
2. Enhance documentation and examples
3. Implement automatic schema generation
4. Add automatic documentation generation

### Medium Term
1. Develop additional platform providers (PHP, .NET Framework)
2. Implement client-side libraries (TypeScript, JavaScript)
3. Add payload containers and pagination support
4. Enhance filtering capabilities

### Long Term
1. Build comprehensive tooling ecosystem
2. Support calculated properties
3. Establish broad platform support
4. Create developer community

## Current Focus
The current focus is on improving the source generator implementation to support AOT compilation and enhance performance. This includes:

1. **Source Generator Improvements**
   - Implementing comprehensive test coverage
   - Adding circular reference detection
   - Fixing thread safety in PopcornAccessor
   - Implementing property reference validation
   - Adding attribute conflict detection
   - Optimizing property reference parsing
   - Optimizing generated code
   - Adding error state support
   - Implementing deserialization support
   - Improving XML documentation
   - Enhancing diagnostic messages

2. **Feature Parity Goals**
   - Implementing sorting support
   - Adding pagination capabilities
   - Supporting filtering
   - Re-implementing authorization system
   - Adding response inspectors
   - Supporting contexts
   - Enabling lazy loading
   - Implementing blind expansion

## Success Metrics
- Reduced API bandwidth usage
- Decreased number of API calls needed
- Improved developer experience
- Growing community adoption
- Comprehensive platform support
- AOT compatibility
- Performance optimization
