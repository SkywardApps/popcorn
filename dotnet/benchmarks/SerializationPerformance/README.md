# Popcorn Performance Benchmarks

This project provides comprehensive performance benchmarks for the Popcorn selective field serialization library, comparing it against standard System.Text.Json serialization across various scenarios.

## Overview

The benchmark suite covers the following performance aspects:

1. **Serialization Comparison** - Standard JSON vs Popcorn-enhanced serialization
2. **Include Strategy Performance** - Impact of different include patterns
3. **Scalability Analysis** - Big O performance characteristics
4. **Circular Reference Detection** - Overhead of loop detection
5. **Attribute Processing** - Performance impact of various attributes

## Project Structure

```
SerializationPerformance/
├── Models/
│   ├── BenchmarkModels.cs      # Test data models with various complexity
│   └── TestDataGenerator.cs    # Generates consistent test data
├── Benchmarks/
│   ├── SerializationComparisonBenchmarks.cs    # JSON vs Popcorn comparison
│   ├── IncludeStrategyBenchmarks.cs            # Include pattern performance
│   ├── ScalabilityBenchmarks.cs                # Big O analysis
│   ├── CircularReferenceBenchmarks.cs          # Loop detection overhead
│   └── AttributeProcessingBenchmarks.cs        # Attribute processing cost
├── Program.cs              # Main benchmark runner
└── README.md              # This file
```

## Benchmark Categories

### 1. Serialization Comparison (`comparison`)

Compares performance between:
- **Standard System.Text.Json** (baseline)
- **Popcorn with default includes**
- **Popcorn with [!all] includes**
- **Popcorn with custom field selection**

Tests both simple and complex object hierarchies with varying collection sizes.

### 2. Include Strategy Performance (`includes`)

Analyzes the performance impact of different include strategies:
- **Empty includes** (default behavior)
- **[!default]** (default fields only)
- **[!all]** (all fields)
- **Simple custom** includes `[Id,Name,Date]`
- **Complex nested** includes `[Id,Details[Name],Items[Id,Name]]`
- **Negation** includes `[!all,-SecretField]`

### 3. Scalability Analysis (`scalability`)

Measures Big O performance characteristics:

**Flat List Scaling:**
- 10, 100, 1K, 10K, 100K items
- Linear scaling analysis (O(n))

**Deep Nesting Scaling:**
- 1, 2, 5, 10, 20 levels deep
- Depth complexity analysis (O(d))

### 4. Circular Reference Detection (`circular`)

Measures the overhead of circular reference detection:
- **Baseline**: Objects without circular references
- **With Loops**: Objects containing circular references
- **Detection Overhead**: Cost of checking for loops

### 5. Attribute Processing (`attributes`)

Analyzes the performance cost of different attribute types:
- **Minimal Attributes**: Simple models with few attributes
- **Heavy Attributes**: Models with many [Always], [Never], [Default] attributes
- **Property Mapping**: JsonPropertyName mapping overhead

## Running Benchmarks

### Prerequisites
- .NET 8.0 or later
- BenchmarkDotNet package
- Sufficient memory for large dataset tests

### Command Line Usage

```bash
# Build the project
dotnet build -c Release

# Run specific benchmark categories
dotnet run -c Release -- comparison      # Serialization comparison
dotnet run -c Release -- includes        # Include strategy performance
dotnet run -c Release -- scalability     # Big O analysis
dotnet run -c Release -- circular        # Circular reference detection
dotnet run -c Release -- attributes      # Attribute processing

# Run all benchmarks (takes significant time)
dotnet run -c Release -- all

# Show help menu
dotnet run -c Release
```

### Result Persistence

All benchmarks automatically save results in multiple formats to `BenchmarkDotNet.Artifacts/results/`:

- **CSV** - For data analysis and Excel import
- **HTML** - Interactive reports with charts and detailed analysis
- **JSON** - Machine-readable format for programmatic analysis  
- **Markdown** - GitHub-flavored markdown for documentation

The HTML format is particularly useful as it provides:
- Interactive performance charts
- Statistical analysis
- Memory allocation details
- Execution time distributions
- Comparative analysis between benchmark methods

### Visual Studio Usage

Set the project as startup project and configure command line arguments in project properties.

## Test Data Models

### SimpleModel
Basic model with minimal attributes for baseline testing.

### ComplexNestedModel
Hierarchical model with nested objects, collections, and dictionaries.

### CircularReferenceModel
Model designed to create circular reference scenarios.

### ScalableModel
Lightweight model optimized for large collection testing.

### DeepNestingModel
Model that creates deep object hierarchies.

### AttributeHeavyModel
Model with extensive attribute usage for attribute processing tests.

### PropertyMappingModel
Model with JsonPropertyName mappings for property name mapping tests.

## Expected Performance Patterns

### Linear Scaling (O(n))
Flat list serialization should scale linearly with item count.

### Depth Complexity (O(d))
Deep nesting performance should scale with nesting depth.

### Include Overhead
- Empty includes: Fastest (minimal processing)
- [!default]: Moderate overhead (default field resolution)
- [!all]: Higher overhead (all field processing)
- Custom includes: Variable (depends on complexity)

### Circular Detection
- No loops: Baseline performance
- With loops: Additional overhead for detection
- Detection always on: Consistent overhead regardless of actual loops

## Interpreting Results

### Key Metrics
- **Mean**: Average execution time per operation
- **Error**: Statistical error margin
- **StdDev**: Standard deviation of measurements
- **Median**: Middle value of all measurements
- **Ratio**: Performance relative to baseline
- **Gen 0/1/2**: Garbage collection pressure
- **Allocated**: Memory allocated per operation

### Performance Baselines
- **Standard JSON**: Primary baseline for comparison
- **Empty Includes**: Popcorn baseline performance
- **Simple Operations**: Reference point for complex operations

## Current Status

⚠️ **Note**: This benchmark suite is currently set up with placeholder implementations. The actual Popcorn serialization calls need to be integrated to replace the `// TODO:` comments in the benchmark methods.

### Implementation Tasks
1. Integrate actual Popcorn serialization calls
2. Implement proper include parameter handling
3. Add support for different serialization contexts
4. Connect to actual source generator output

### Usage Recommendations
1. Run benchmarks on dedicated hardware for consistent results
2. Close unnecessary applications before benchmarking
3. Run multiple iterations to ensure statistical significance
4. Compare results across different .NET versions if needed

## Integration with Popcorn Source Generator

Once integrated with the actual Popcorn source generator:

1. **Models** will need to be properly configured with JsonSerializerContext
2. **Benchmark methods** will call actual Popcorn serialization instead of placeholders
3. **Include parameters** will be properly parsed and applied
4. **Generated converters** will be used for type-safe serialization

## Future Enhancements

- **Memory profiling**: Detailed memory usage analysis
- **Concurrent testing**: Multi-threaded serialization performance
- **Cache effectiveness**: Measurement of internal caching benefits
- **Compression impact**: Performance with output compression
- **Different data patterns**: Real-world data structure testing
- **Cross-platform testing**: Performance across different operating systems
