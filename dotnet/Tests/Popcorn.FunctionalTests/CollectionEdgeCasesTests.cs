using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Popcorn.FunctionalTests.Models;
using Popcorn.Shared;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class CollectionEdgeCasesTests
    {
        [Fact]
        public async Task CollectionEdgeCases_NullCollections_SerializeAsNull()
        {
            // Create test model with null collections
            var testModel = new CollectionEdgeCasesModel
            {
                NullIntList = null,
                NullStringArray = null,
                NullDictionary = null
            };

            // Create test server
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async context =>
                        {
                            var response = context.Respond(testModel);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = null
                            };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(
                                context.Response.Body,
                                response,
                                options);
                        });
                    });
                }));

            var client = server.CreateClient();

            // Test null collections
            var response = await client.GetAsync("/test?include=[NullIntList,NullStringArray,NullDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert null collections
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullIntList", out var nullIntList), "NullIntList property is present");
            Assert.Equal(JsonValueKind.Null, nullIntList.ValueKind);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullStringArray", out var nullStringArray), "NullStringArray property is present");
            Assert.Equal(JsonValueKind.Null, nullStringArray.ValueKind);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullDictionary", out var nullDictionary), "NullDictionary property is present");
            Assert.Equal(JsonValueKind.Null, nullDictionary.ValueKind);
        }

        [Fact]
        public async Task CollectionEdgeCases_EmptyCollections_SerializeAsEmptyArrays()
        {
            // Create test model with empty collections
            var testModel = new CollectionEdgeCasesModel
            {
                // Empty collections are already initialized by default
            };

            // Create test server
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async context =>
                        {
                            var response = context.Respond(testModel);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = null
                            };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(
                                context.Response.Body,
                                response,
                                options);
                        });
                    });
                }));

            var client = server.CreateClient();

            // Test empty collections
            var response = await client.GetAsync("/test?include=[EmptyIntList,EmptyStringArray,EmptyDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert empty collections
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("EmptyIntList", out var emptyIntList), "EmptyIntList property is present");
            Assert.Equal(JsonValueKind.Array, emptyIntList.ValueKind);
            Assert.Equal(0, emptyIntList.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("EmptyStringArray", out var emptyStringArray), "EmptyStringArray property is present");
            Assert.Equal(JsonValueKind.Array, emptyStringArray.ValueKind);
            Assert.Equal(0, emptyStringArray.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("EmptyDictionary", out var emptyDictionary), "EmptyDictionary property is present");
            Assert.Equal(JsonValueKind.Object, emptyDictionary.ValueKind);
            Assert.Equal(0, emptyDictionary.EnumerateObject().Count());
        }

        [Fact]
        public async Task CollectionEdgeCases_CollectionsWithNullItems_SerializeWithNullItems()
        {
            // Create test model with collections containing null items
            var testModel = new CollectionEdgeCasesModel
            {
                ListWithNullItems = new List<string?> { "Item1", null, "Item3" },
                ListWithNullComplexItems = new List<ComplexItem?> 
                { 
                    new ComplexItem { Id = 1, Name = "Item 1", Description = "Description 1", CreatedDate = new DateTime(2025, 1, 1) },
                    null,
                    new ComplexItem { Id = 3, Name = "Item 3", Description = "Description 3", CreatedDate = new DateTime(2025, 1, 3) }
                }
            };

            // Create test server
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async context =>
                        {
                            var response = context.Respond(testModel);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = null
                            };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(
                                context.Response.Body,
                                response,
                                options);
                        });
                    });
                }));

            var client = server.CreateClient();

            // Test collections with null items
            var response = await client.GetAsync("/test?include=[ListWithNullItems,ListWithNullComplexItems]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ListWithNullItems
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ListWithNullItems", out var listWithNullItems), "ListWithNullItems property is present");
            Assert.Equal(3, listWithNullItems.GetArrayLength());
            Assert.Equal("Item1", listWithNullItems[0].GetString());
            Assert.Equal(JsonValueKind.Null, listWithNullItems[1].ValueKind);
            Assert.Equal("Item3", listWithNullItems[2].GetString());
            
            // Assert ListWithNullComplexItems
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ListWithNullComplexItems", out var listWithNullComplexItems), "ListWithNullComplexItems property is present");
            Assert.Equal(3, listWithNullComplexItems.GetArrayLength());
            Assert.Equal(JsonValueKind.Object, listWithNullComplexItems[0].ValueKind);
            Assert.Equal(JsonValueKind.Null, listWithNullComplexItems[1].ValueKind);
            Assert.Equal(JsonValueKind.Object, listWithNullComplexItems[2].ValueKind);
        }

        [Fact]
        public async Task CollectionEdgeCases_CircularReferences_HandlesCircularReferencesCorrectly()
        {
            // Create test model with circular references
            var testModel = new CollectionEdgeCasesModel();
            
            // Create circular reference items
            var item1 = new CircularReferenceItem { Id = 1, Name = "Item 1" };
            var item2 = new CircularReferenceItem { Id = 2, Name = "Item 2" };
            var item3 = new CircularReferenceItem { Id = 3, Name = "Item 3" };
            
            // Create circular references
            item1.Parent = item3; // Item 1's parent is Item 3
            item2.Parent = item1; // Item 2's parent is Item 1
            item3.Parent = item2; // Item 3's parent is Item 2
            
            // Create circular references in children collections
            item1.Children.Add(item2); // Item 1 has Item 2 as child
            item2.Children.Add(item3); // Item 2 has Item 3 as child
            item3.Children.Add(item1); // Item 3 has Item 1 as child
            
            // Add items to the test model
            testModel.CircularReferenceList = new List<CircularReferenceItem> { item1, item2, item3 };

            // Create test server
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async context =>
                        {
                            var response = context.Respond(testModel);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = null,
                                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                            };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(
                                context.Response.Body,
                                response,
                                options);
                        });
                    });
                }));

            var client = server.CreateClient();

            // Test circular references
            var response = await client.GetAsync("/test?include=[CircularReferenceList]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert CircularReferenceList
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("CircularReferenceList", out var circularReferenceList), "CircularReferenceList property is present");
            Assert.Equal(3, circularReferenceList.GetArrayLength());
            
            // Note: The exact behavior with circular references depends on the System.Text.Json configuration.
            // With ReferenceHandler.Preserve, it should use $id and $ref properties to handle circular references.
            // We're just checking that serialization completes without exceptions.
        }

        [Fact]
        public async Task CollectionEdgeCases_VeryLargeCollections_SerializesCorrectly()
        {
            // Create test model with very large collections
            var testModel = new CollectionEdgeCasesModel
            {
                VeryLargeIntList = new List<int>(),
                VeryLargeStringList = new List<string>()
            };
            
            // Add 1000 items to VeryLargeIntList
            for (int i = 0; i < 1000; i++)
            {
                testModel.VeryLargeIntList.Add(i);
            }
            
            // Add 1000 items to VeryLargeStringList
            for (int i = 0; i < 1000; i++)
            {
                testModel.VeryLargeStringList.Add($"Item {i}");
            }

            // Create test server
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async context =>
                        {
                            var response = context.Respond(testModel);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = null
                            };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(
                                context.Response.Body,
                                response,
                                options);
                        });
                    });
                }));

            var client = server.CreateClient();

            // Test very large collections
            var response = await client.GetAsync("/test?include=[VeryLargeIntList,VeryLargeStringList]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert VeryLargeIntList
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("VeryLargeIntList", out var veryLargeIntList), "VeryLargeIntList property is present");
            Assert.Equal(1000, veryLargeIntList.GetArrayLength());
            
            // Check first, middle, and last items
            Assert.Equal(0, veryLargeIntList[0].GetInt32());
            Assert.Equal(500, veryLargeIntList[500].GetInt32());
            Assert.Equal(999, veryLargeIntList[999].GetInt32());
            
            // Assert VeryLargeStringList
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("VeryLargeStringList", out var veryLargeStringList), "VeryLargeStringList property is present");
            Assert.Equal(1000, veryLargeStringList.GetArrayLength());
            
            // Check first, middle, and last items
            Assert.Equal("Item 0", veryLargeStringList[0].GetString());
            Assert.Equal("Item 500", veryLargeStringList[500].GetString());
            Assert.Equal("Item 999", veryLargeStringList[999].GetString());
        }

        [Fact]
        public async Task CollectionEdgeCases_ItemsWithLargeProperties_SerializesCorrectly()
        {
            // Create test model with items having large properties
            var testModel = new CollectionEdgeCasesModel
            {
                ItemsWithLargeProperties = new List<ItemWithLargeProperties>
                {
                    new ItemWithLargeProperties
                    {
                        Id = 1,
                        VeryLargeString = new string('a', 10000), // 10,000 'a' characters
                        VeryLargeArray = Enumerable.Range(0, 1000).ToArray() // Array with 1000 integers
                    },
                    new ItemWithLargeProperties
                    {
                        Id = 2,
                        VeryLargeString = new string('b', 10000), // 10,000 'b' characters
                        VeryLargeArray = Enumerable.Range(1000, 1000).ToArray() // Array with 1000 integers
                    }
                }
            };

            // Create test server
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", async context =>
                        {
                            var response = context.Respond(testModel);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = null
                            };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(
                                context.Response.Body,
                                response,
                                options);
                        });
                    });
                }));

            var client = server.CreateClient();

            // Test items with large properties
            var response = await client.GetAsync("/test?include=[ItemsWithLargeProperties]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithLargeProperties
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithLargeProperties", out var itemsWithLargeProperties), "ItemsWithLargeProperties property is present");
            Assert.Equal(2, itemsWithLargeProperties.GetArrayLength());
            
            // Check first item
            Assert.True(itemsWithLargeProperties[0].TryGetProperty("Id", out var id1), "Id property is present");
            Assert.Equal(1, id1.GetInt32());
            
            Assert.True(itemsWithLargeProperties[0].TryGetProperty("VeryLargeString", out var veryLargeString1), "VeryLargeString property is present");
            Assert.Equal(10000, veryLargeString1.GetString().Length);
            Assert.Equal(new string('a', 10000), veryLargeString1.GetString());
            
            Assert.True(itemsWithLargeProperties[0].TryGetProperty("VeryLargeArray", out var veryLargeArray1), "VeryLargeArray property is present");
            Assert.Equal(1000, veryLargeArray1.GetArrayLength());
            Assert.Equal(0, veryLargeArray1[0].GetInt32());
            Assert.Equal(999, veryLargeArray1[999].GetInt32());
            
            // Check second item
            Assert.True(itemsWithLargeProperties[1].TryGetProperty("Id", out var id2), "Id property is present");
            Assert.Equal(2, id2.GetInt32());
            
            Assert.True(itemsWithLargeProperties[1].TryGetProperty("VeryLargeString", out var veryLargeString2), "VeryLargeString property is present");
            Assert.Equal(10000, veryLargeString2.GetString().Length);
            Assert.Equal(new string('b', 10000), veryLargeString2.GetString());
            
            Assert.True(itemsWithLargeProperties[1].TryGetProperty("VeryLargeArray", out var veryLargeArray2), "VeryLargeArray property is present");
            Assert.Equal(1000, veryLargeArray2.GetArrayLength());
            Assert.Equal(1000, veryLargeArray2[0].GetInt32());
            Assert.Equal(1999, veryLargeArray2[999].GetInt32());
        }
    }
}
