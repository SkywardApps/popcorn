using SerializationPerformance.Models;

namespace SerializationPerformance.Models;

public static class TestDataGenerator
{
    private static readonly Random _random = new(42); // Fixed seed for consistent results
    
    private static readonly string[] _sampleNames = {
        "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Iris", "Jack"
    };
    
    private static readonly string[] _sampleTitles = {
        "Manager", "Developer", "Designer", "Analyst", "Coordinator", "Specialist", "Lead", "Senior", "Junior", "Principal"
    };
    
    private static readonly string[] _sampleDescriptions = {
        "Sample description for testing",
        "Another test description with more content",
        "Brief desc",
        "Very detailed description with multiple sentences and longer content for testing serialization performance.",
        null, // Test null handling
        ""    // Test empty strings
    };
    
    private static readonly string[] _sampleCategories = {
        "A", "B", "C", "D", "E", null
    };

    public static SimpleModel CreateSimpleModel(int id = 0)
    {
        return new SimpleModel
        {
            Id = id,
            Name = _sampleNames[_random.Next(_sampleNames.Length)],
            CreatedAt = DateTime.Now.AddDays(-_random.Next(365)),
            Description = _sampleDescriptions[_random.Next(_sampleDescriptions.Length)],
            IsActive = _random.NextDouble() > 0.5
        };
    }

    public static List<SimpleModel> CreateSimpleModelList(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateSimpleModel(i))
            .ToList();
    }

    public static ComplexNestedModel CreateComplexNestedModel(int id = 0, int maxDepth = 3, int currentDepth = 0)
    {
        var model = new ComplexNestedModel
        {
            Id = id,
            Title = _sampleTitles[_random.Next(_sampleTitles.Length)],
            Timestamp = DateTime.Now.AddHours(-_random.Next(24)),
            Details = _random.NextDouble() > 0.3 ? CreateSimpleModel(id + 1000) : null,
            Priority = _random.Next(1, 6),
            SecretData = "Secret" + id
        };

        // Add some items to the collection
        int itemCount = _random.Next(0, 5);
        for (int i = 0; i < itemCount; i++)
        {
            model.Items.Add(CreateSimpleModel(id * 100 + i));
        }

        // Add dictionary entries
        int lookupCount = _random.Next(0, 4);
        for (int i = 0; i < lookupCount; i++)
        {
            string key = $"key_{id}_{i}";
            model.Lookup[key] = CreateSimpleModel(id * 1000 + i);
        }

        // Add nested child if we haven't reached max depth
        if (currentDepth < maxDepth && _random.NextDouble() > 0.5)
        {
            model.Child = CreateComplexNestedModel(id + 10, maxDepth, currentDepth + 1);
        }

        return model;
    }

    public static List<ComplexNestedModel> CreateComplexNestedModelList(int count, int maxDepth = 2)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateComplexNestedModel(i, maxDepth))
            .ToList();
    }

    public static CircularReferenceModel CreateCircularReferenceModel(int id = 0, bool createCircular = false)
    {
        var model = new CircularReferenceModel
        {
            Id = id,
            Name = $"Item_{id}"
        };

        if (createCircular)
        {
            // Create a simple circular reference
            var child = new CircularReferenceModel
            {
                Id = id + 1,
                Name = $"Child_{id + 1}",
                Parent = model
            };
            
            model.Children.Add(child);
            
            // Create self-reference for more complex circular detection
            if (_random.NextDouble() > 0.7)
            {
                model.Self = model;
            }
        }
        else
        {
            // Create a few children without circular references
            int childCount = _random.Next(0, 4);
            for (int i = 0; i < childCount; i++)
            {
                var child = new CircularReferenceModel
                {
                    Id = id * 10 + i,
                    Name = $"Child_{id}_{i}",
                    Parent = model
                };
                model.Children.Add(child);
            }
        }

        return model;
    }

    public static List<CircularReferenceModel> CreateCircularReferenceModelList(int count, bool includeCircular = false)
    {
        var list = new List<CircularReferenceModel>();
        
        for (int i = 0; i < count; i++)
        {
            // Only create circular references for some items if requested
            bool makeCircular = includeCircular && (_random.NextDouble() > 0.8);
            list.Add(CreateCircularReferenceModel(i, makeCircular));
        }
        
        return list;
    }

    public static List<ScalableModel> CreateScalableModelList(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new ScalableModel
            {
                Id = i,
                Data = $"Data_{i}_{_random.Next(1000)}",
                Value = _random.NextDouble() * 1000,
                Timestamp = DateTime.Now.AddSeconds(-i),
                IsImportant = _random.NextDouble() > 0.7,
                Category = _sampleCategories[_random.Next(_sampleCategories.Length)]
            })
            .ToList();
    }

    public static DeepNestingModel CreateDeepNestingModel(int depth)
    {
        DeepNestingModel? current = null;
        
        // Build from the deepest level up
        for (int i = depth; i >= 0; i--)
        {
            var newModel = new DeepNestingModel
            {
                Level = i,
                Name = $"Level_{i}",
                ProcessedAt = DateTime.Now.AddMinutes(-i),
                Description = i % 3 == 0 ? $"Description for level {i}" : null,
                Next = current
            };
            current = newModel;
        }
        
        return current!;
    }

    public static List<AttributeHeavyModel> CreateAttributeHeavyModelList(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new AttributeHeavyModel
            {
                Id = i,
                Name = _sampleNames[i % _sampleNames.Length],
                Password = $"secret_{i}",
                InternalNotes = $"Internal note {i}",
                Email = $"user{i}@example.com",
                LastLogin = DateTime.Now.AddDays(-_random.Next(30)),
                IsActive = i % 2 == 0,
                PublicData = $"Public data for user {i}",
                Score = _random.Next(0, 1000),
                Rating = _random.NextDouble() * 5.0,
                Tags = Enumerable.Range(0, _random.Next(0, 6))
                    .Select(j => $"tag_{i}_{j}")
                    .ToList(),
                Metadata = new Dictionary<string, object>
                {
                    ["created"] = DateTime.Now.AddDays(-i),
                    ["version"] = i,
                    ["settings"] = new { theme = "dark", lang = "en" }
                }
            })
            .ToList();
    }

    public static List<PropertyMappingModel> CreatePropertyMappingModelList(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new PropertyMappingModel
            {
                UserId = i,
                FullName = $"{_sampleNames[i % _sampleNames.Length]} Smith",
                EmailAddress = $"user{i}@test.com",
                CreatedTimestamp = DateTime.Now.AddDays(-i),
                IsVerified = i % 3 == 0,
                UserPreferences = new Dictionary<string, string>
                {
                    ["theme"] = i % 2 == 0 ? "dark" : "light",
                    ["language"] = "en",
                    ["timezone"] = "UTC"
                }
            })
            .ToList();
    }
}
