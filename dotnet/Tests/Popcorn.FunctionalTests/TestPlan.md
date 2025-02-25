# Popcorn Serialization Test Plan

This document outlines a comprehensive test plan for the Popcorn serialization functionality, covering various scenarios and edge cases.

## 1. Property Inclusion/Exclusion Tests

### 1.1 Attribute-Based Inclusion
- **Always Attribute**
  - Test that properties with `[Always]` attribute are included regardless of include parameters
  - Test that properties with `[Always]` attribute cannot be excluded even with negation
  
- **Default Attribute**
  - Test that properties with `[Default]` attribute are included when no specific properties are requested
  - Test that properties with `[Default]` attribute are included when `!default` is specified
  - Test that properties with `[Default]` attribute can be excluded with negation

- **Never Attribute**
  - Test that properties with `[Never]` attribute are never included regardless of include parameters
  - Test that properties with `[Never]` attribute cannot be included even with explicit inclusion

### 1.2 Include Parameter Variations
- **Wildcard Includes**
  - Test `!all` includes all properties
  - Test `!default` includes only default properties
  
- **Specific Property Includes**
  - Test including specific properties by name
  - Test including multiple properties
  - Test case sensitivity of property names
  
- **Property Negation**
  - Test negating specific properties (e.g., `[prop1,prop2,!prop3]`)
  - Test negating properties with `!all` (e.g., `[!all,!prop1]`)
  - Test negating properties with `!default` (e.g., `[!default,!prop1]`)

## 2. Data Type Tests

### 2.1 Primitive Types
- Test serialization of numeric types (int, long, float, double, decimal)
- Test serialization of string types
- Test serialization of boolean types
- Test serialization of DateTime/DateTimeOffset
- Test serialization of Guid

### 2.2 Complex Types
- Test serialization of arrays and collections
- Test serialization of dictionaries
- Test serialization of custom objects
- Test serialization of anonymous types

## 3. Nested Object Tests

### 3.1 Simple Nesting
- Test serialization of objects with nested objects
- Test selective inclusion of nested object properties

### 3.2 Deep Nesting
- Test serialization of deeply nested objects (3+ levels)
- Test nested include statements (e.g., `prop1.nestedProp1`)

## 4. Reference Handling Tests

### 4.1 Circular References
- Test serialization of objects with circular references
- Test handling of circular references with different inclusion patterns

### 4.2 Shared References
- Test serialization of objects that share references to the same object
- Test that reference equality is preserved when appropriate

## 5. Null Value Tests

### 5.1 Null Objects
- Test serialization of null object references
- Test serialization of objects with null properties

### 5.2 Null Primitives
- Test serialization of null strings
- Test serialization of nullable value types (int?, bool?, etc.)

## 6. Error Handling Tests

### 6.1 Invalid Property Names
- Test requesting non-existent properties
- Test malformed include parameters

### 6.2 Type Mismatch
- Test serialization when property types don't match expected types

## 7. Polymorphism Tests

### 7.1 Basic Polymorphism
- Test serialization of derived types when base type is expected
- Test property inclusion for properties only in derived types

### 7.2 Interface Implementation
- Test serialization of objects implementing interfaces
- Test property inclusion for interface vs. implementation properties

## 8. Performance Tests

### 8.1 Large Object Graphs
- Test serialization performance with large object graphs
- Compare performance with and without selective property inclusion

### 8.2 High Volume
- Test serialization performance with high volume of objects
- Test serialization performance with repeated serialization calls

## 9. Integration Tests

### 9.1 ASP.NET Integration
- Test integration with ASP.NET controllers
- Test integration with middleware

### 9.2 Custom JsonConverter Integration
- Test integration with custom JsonConverter implementations
- Test priority handling between Popcorn and other converters

## 10. Edge Cases

### 10.1 Empty Collections
- Test serialization of empty arrays, lists, and dictionaries

### 10.2 Special Characters
- Test property names with special characters
- Test string values with special characters and Unicode

### 10.3 Extremely Large Values
- Test serialization of extremely large strings
- Test serialization of extremely large numeric values

## Implementation Strategy

For each test category, we should:

1. Create appropriate model classes with the necessary properties and attributes
2. Implement test methods that verify the expected behavior
3. Use assertions to validate that the serialized output matches expectations
4. Document any edge cases or unexpected behavior

## Test Models Required

1. **AttributeTestModel** - Model with properties having different attributes (Always, Default, Never)
2. **PrimitiveTypesModel** - Model with properties of various primitive types
3. **ComplexTypesModel** - Model with properties of complex types (arrays, dictionaries, etc.)
4. **NestedObjectModel** - Model with nested object properties
5. **CircularReferenceModel** - Model with circular references
6. **NullablePropertiesModel** - Model with nullable properties
7. **PolymorphicModel** - Base model with derived types for polymorphism testing
8. **LargeObjectModel** - Model with many properties for performance testing

## Priority Order

1. Basic property inclusion/exclusion tests (highest priority)
2. Data type tests
3. Nested object tests
4. Null value tests
5. Error handling tests
6. Reference handling tests
7. Polymorphism tests
8. Integration tests
9. Performance tests
10. Edge cases (lowest priority)
