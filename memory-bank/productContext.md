# Product Context: Popcorn

## Problem Space

### Current API Challenges
1. **Multiple API Calls**
   - Retrieving related data requires multiple requests
   - Increased latency and bandwidth usage
   - Complex client-side orchestration

2. **Over-fetching**
   - APIs return more data than needed
   - Wasted bandwidth
   - Unnecessary processing

3. **Under-fetching**
   - Missing related data requires additional requests
   - Poor performance
   - Complex client implementations

### User Pain Points
- Mobile apps struggle with multiple API calls
- High latency in low-bandwidth scenarios
- Complex data relationships require many requests
- Difficult to optimize API responses for different clients

## Solution

### Core Functionality
1. **Field Selection**
   - Clients specify exactly what fields they need
   - Support for nested data structures
   - Collection handling

2. **Single Request Resolution**
   - Related data retrieved in one call
   - Recursive field selection
   - Optimized server-side processing

3. **Flexible Implementation**
   - Platform-specific providers
   - Consistent protocol across implementations
   - Extensible architecture

### Implementation Approaches

#### Runtime Reflection (Original)
- Dynamic property resolution
- Flexible but performance-intensive
- Not compatible with AOT compilation

#### Source Generation (Current)
- Build-time code generation
- AOT-compatible
- Performance optimized
- Type-safe serialization

### Key Features
1. **Selective Field Inclusion**
   - Include only needed fields
   - Support for nested objects
   - Collection handling

2. **Default Field Handling**
   - Entity-specific default fields
   - Implicit field inclusion rules
   - Empty selection handling

3. **Attribute-Based Control**
   - Always include specific fields
   - Never include specific fields
   - Default include behavior

4. **Performance Optimization**
   - Reduced bandwidth usage
   - Fewer API calls
   - Efficient serialization

## User Experience Goals

### API Consumers
- Intuitive field selection syntax
- Reduced implementation complexity
- Improved application performance
- Bandwidth optimization
- Clear documentation and examples

### API Providers
- Easy integration with existing APIs
- Minimal performance overhead
- Clear implementation guidelines
- Robust error handling
- Flexible configuration options

## Success Criteria
1. **Performance**
   - Reduced number of API calls
   - Decreased bandwidth usage
   - Improved response times

2. **Developer Experience**
   - Reduced implementation time
   - Clear documentation
   - Intuitive API design
   - Strong community support

3. **Platform Support**
   - Multiple language implementations
   - Framework integrations
   - Tool ecosystem

## Target Audience

### Primary Users
- API developers
- Mobile application developers
- Web application developers
- Microservice architects

### Secondary Users
- DevOps engineers
- System architects
- Technical leads
- Open source contributors

## Market Positioning
- Open source protocol
- Complementary to GraphQL and OData
- Focus on simplicity and performance
- RESTful API enhancement
