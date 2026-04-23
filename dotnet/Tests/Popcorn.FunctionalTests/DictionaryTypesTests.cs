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
    public class DictionaryTypesTests
    {
        [Fact]
        public async Task DictionaryTypes_BasicDictionaries_SerializeCorrectly()
        {
            // Create test model with basic dictionaries
            var testModel = new DictionaryTypesModel
            {
                StringIntDictionary = new Dictionary<string, int>
                {
                    { "one", 1 },
                    { "two", 2 },
                    { "three", 3 }
                },
                StringStringDictionary = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                    { "key3", "value3" }
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

            // Test basic dictionaries
            var response = await client.GetAsync("/test?include=[StringIntDictionary,StringStringDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert StringIntDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringIntDictionary", out var stringIntDictionary), "StringIntDictionary property is present");
            Assert.True(stringIntDictionary.TryGetProperty("one", out var oneValue), "Key 'one' is present");
            Assert.Equal(1, oneValue.GetInt32());
            Assert.True(stringIntDictionary.TryGetProperty("two", out var twoValue), "Key 'two' is present");
            Assert.Equal(2, twoValue.GetInt32());
            Assert.True(stringIntDictionary.TryGetProperty("three", out var threeValue), "Key 'three' is present");
            Assert.Equal(3, threeValue.GetInt32());

            // Assert StringStringDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringStringDictionary", out var stringStringDictionary), "StringStringDictionary property is present");
            Assert.True(stringStringDictionary.TryGetProperty("key1", out var key1Value), "Key 'key1' is present");
            Assert.Equal("value1", key1Value.GetString());
            Assert.True(stringStringDictionary.TryGetProperty("key2", out var key2Value), "Key 'key2' is present");
            Assert.Equal("value2", key2Value.GetString());
            Assert.True(stringStringDictionary.TryGetProperty("key3", out var key3Value), "Key 'key3' is present");
            Assert.Equal("value3", key3Value.GetString());
        }

        [Fact]
        public async Task DictionaryTypes_InterfaceDictionaries_SerializeCorrectly()
        {
            // Create test model with interface dictionaries
            var testModel = new DictionaryTypesModel
            {
                StringIntIDictionary = new Dictionary<string, int>
                {
                    { "one", 1 },
                    { "two", 2 },
                    { "three", 3 }
                },
                StringStringIDictionary = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                    { "key3", "value3" }
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

            // Test interface dictionaries
            var response = await client.GetAsync("/test?include=[StringIntIDictionary,StringStringIDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert StringIntIDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringIntIDictionary", out var stringIntIDictionary), "StringIntIDictionary property is present");
            Assert.True(stringIntIDictionary.TryGetProperty("one", out var oneValue), "Key 'one' is present");
            Assert.Equal(1, oneValue.GetInt32());
            Assert.True(stringIntIDictionary.TryGetProperty("two", out var twoValue), "Key 'two' is present");
            Assert.Equal(2, twoValue.GetInt32());
            Assert.True(stringIntIDictionary.TryGetProperty("three", out var threeValue), "Key 'three' is present");
            Assert.Equal(3, threeValue.GetInt32());

            // Assert StringStringIDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringStringIDictionary", out var stringStringIDictionary), "StringStringIDictionary property is present");
            Assert.True(stringStringIDictionary.TryGetProperty("key1", out var key1Value), "Key 'key1' is present");
            Assert.Equal("value1", key1Value.GetString());
            Assert.True(stringStringIDictionary.TryGetProperty("key2", out var key2Value), "Key 'key2' is present");
            Assert.Equal("value2", key2Value.GetString());
            Assert.True(stringStringIDictionary.TryGetProperty("key3", out var key3Value), "Key 'key3' is present");
            Assert.Equal("value3", key3Value.GetString());
        }

        [Fact]
        public async Task DictionaryTypes_ReadOnlyDictionaries_SerializeCorrectly()
        {
            // Create test model with read-only dictionaries
            var stringIntDictionary = new Dictionary<string, int>
            {
                { "one", 1 },
                { "two", 2 },
                { "three", 3 }
            };
            
            var stringStringDictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };
            
            var testModel = new DictionaryTypesModel
            {
                ReadOnlyStringIntDictionary = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(stringIntDictionary),
                ReadOnlyStringStringDictionary = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(stringStringDictionary)
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

            // Test read-only dictionaries
            var response = await client.GetAsync("/test?include=[ReadOnlyStringIntDictionary,ReadOnlyStringStringDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ReadOnlyStringIntDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ReadOnlyStringIntDictionary", out var readOnlyStringIntDictionary), "ReadOnlyStringIntDictionary property is present");
            Assert.True(readOnlyStringIntDictionary.TryGetProperty("one", out var oneValue), "Key 'one' is present");
            Assert.Equal(1, oneValue.GetInt32());
            Assert.True(readOnlyStringIntDictionary.TryGetProperty("two", out var twoValue), "Key 'two' is present");
            Assert.Equal(2, twoValue.GetInt32());
            Assert.True(readOnlyStringIntDictionary.TryGetProperty("three", out var threeValue), "Key 'three' is present");
            Assert.Equal(3, threeValue.GetInt32());

            // Assert ReadOnlyStringStringDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ReadOnlyStringStringDictionary", out var readOnlyStringStringDictionary), "ReadOnlyStringStringDictionary property is present");
            Assert.True(readOnlyStringStringDictionary.TryGetProperty("key1", out var key1Value), "Key 'key1' is present");
            Assert.Equal("value1", key1Value.GetString());
            Assert.True(readOnlyStringStringDictionary.TryGetProperty("key2", out var key2Value), "Key 'key2' is present");
            Assert.Equal("value2", key2Value.GetString());
            Assert.True(readOnlyStringStringDictionary.TryGetProperty("key3", out var key3Value), "Key 'key3' is present");
            Assert.Equal("value3", key3Value.GetString());
        }

        [Fact]
        public async Task DictionaryTypes_ComplexValueDictionary_SerializesCorrectly()
        {
            // Create test model with complex value dictionary
            var testModel = new DictionaryTypesModel
            {
                StringComplexItemDictionary = new Dictionary<string, ComplexItem>
                {
                    { "item1", new ComplexItem { Id = 1, Name = "Item 1", Description = "Description 1", CreatedDate = new DateTime(2025, 1, 1) } },
                    { "item2", new ComplexItem { Id = 2, Name = "Item 2", Description = "Description 2", CreatedDate = new DateTime(2025, 1, 2) } },
                    { "item3", new ComplexItem { Id = 4, Name = "Item 3", Description = "Description 3", CreatedDate = new DateTime(2025, 1, 3) } }
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

            // Test complex value dictionary with no specific properties requested
            var response = await client.GetAsync("/test?include=[StringComplexItemDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert StringComplexItemDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringComplexItemDictionary", out var stringComplexItemDictionary), "StringComplexItemDictionary property is present");
            
            // Check that only Default and Always properties are included
            Assert.True(stringComplexItemDictionary.TryGetProperty("item1", out var item1), "Key 'item1' is present");
            Assert.True(item1.TryGetProperty("Description", out _), "Default property 'Description' is included");
            Assert.True(item1.TryGetProperty("CreatedDate", out _), "Always property 'CreatedDate' is included");
            Assert.False(item1.TryGetProperty("Id", out _), "Non-default property 'Id' is not included");
            Assert.False(item1.TryGetProperty("Name", out _), "Non-default property 'Name' is not included");

            // Test complex value dictionary with specific properties requested
            response = await client.GetAsync("/test?include=[StringComplexItemDictionary[Id,Name,Description,CreatedDate]]");
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);

            // Assert StringComplexItemDictionary with specific properties
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringComplexItemDictionary", out stringComplexItemDictionary), "StringComplexItemDictionary property is present");
            Assert.True(stringComplexItemDictionary.TryGetProperty("item1", out item1));
            Assert.True(item1.TryGetProperty("Id", out _), "Requested property 'Id' must be included");
            Assert.True(item1.TryGetProperty("Name", out _), "Requested property 'Name' must be included");
            Assert.True(item1.TryGetProperty("Description", out var description), "Requested property 'Description' must be included");
            Assert.True(item1.TryGetProperty("CreatedDate", out _), "[Always] property 'CreatedDate' must be included");
            Assert.Equal("Description 1", description.GetString());
        }

        // Regression: the generator used to look at firstRef.Children instead of value.PropertyReferences
        // when descending into a dictionary value type, which silently dropped every requested sibling and
        // fell back to the Default include list. These tests exercise the subset / wildcard / negation paths
        // through a complex-valued dictionary to prevent that regression.
        [Fact]
        public async Task DictionaryTypes_ComplexValueDictionary_ExplicitSubsetInclude_DropsNonRequestedProperties()
        {
            var model = new DictionaryTypesModel
            {
                StringComplexItemDictionary = new Dictionary<string, ComplexItem>
                {
                    { "a", new ComplexItem { Id = 1, Name = "n1", Description = "d1", CreatedDate = new DateTime(2025,1,1) } }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[StringComplexItemDictionary[Id,Name]]");

            var dict = doc.GetData().GetProperty("StringComplexItemDictionary");
            var item = dict.GetProperty("a");
            Assert.True(item.HasProperty("Id"));
            Assert.True(item.HasProperty("Name"));
            Assert.False(item.HasProperty("Description"), "Description is [Default] but not in the explicit subset, so it must be excluded");
            // [Always] still wins
            Assert.True(item.HasProperty("CreatedDate"));
        }

        [Fact]
        public async Task DictionaryTypes_ComplexValueDictionary_WildcardInclude_EmitsAllProperties()
        {
            var model = new DictionaryTypesModel
            {
                StringComplexItemDictionary = new Dictionary<string, ComplexItem>
                {
                    { "a", new ComplexItem { Id = 7, Name = "n", Description = "d", CreatedDate = new DateTime(2025,1,1) } }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[StringComplexItemDictionary[!all]]");

            var item = doc.GetData().GetProperty("StringComplexItemDictionary").GetProperty("a");
            Assert.True(item.HasProperty("Id"));
            Assert.True(item.HasProperty("Name"));
            Assert.True(item.HasProperty("Description"));
            Assert.True(item.HasProperty("CreatedDate"));
        }

        [Fact]
        public async Task DictionaryTypes_ComplexValueDictionary_NegatedInclude_ExcludesNamedProperty()
        {
            var model = new DictionaryTypesModel
            {
                StringComplexItemDictionary = new Dictionary<string, ComplexItem>
                {
                    { "a", new ComplexItem { Id = 1, Name = "n", Description = "d", CreatedDate = new DateTime(2025,1,1) } }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[StringComplexItemDictionary[!all,-Name]]");

            var item = doc.GetData().GetProperty("StringComplexItemDictionary").GetProperty("a");
            Assert.True(item.HasProperty("Id"));
            Assert.False(item.HasProperty("Name"), "Name was negated and must be excluded");
            Assert.True(item.HasProperty("Description"));
            Assert.True(item.HasProperty("CreatedDate"));
        }

        [Fact]
        public async Task DictionaryTypes_NestedDictionary_PropagatesIncludeTreeDownEachLevel()
        {
            var model = new DictionaryTypesModel
            {
                NestedStringIntDictionary = new Dictionary<string, Dictionary<string, int>>
                {
                    { "outer1", new Dictionary<string, int> { { "i", 1 }, { "j", 2 } } }
                }
            };
            using var server = TestServerHelper.CreateServer(model);
            var doc = await TestServerHelper.GetJsonAsync(server.CreateClient(), "/test?include=[NestedStringIntDictionary]");

            var outer = doc.GetData().GetProperty("NestedStringIntDictionary");
            Assert.True(outer.TryGetProperty("outer1", out var inner));
            Assert.Equal(1, inner.GetProperty("i").GetInt32());
            Assert.Equal(2, inner.GetProperty("j").GetInt32());
        }

        [Fact]
        public async Task DictionaryTypes_CollectionValueDictionary_SerializesCorrectly()
        {
            // Create test model with collection value dictionary
            var testModel = new DictionaryTypesModel
            {
                StringIntListDictionary = new Dictionary<string, List<int>>
                {
                    { "list1", new List<int> { 1, 2, 3 } },
                    { "list2", new List<int> { 4, 5, 6 } },
                    { "list3", new List<int> { 7, 8, 9 } }
                },
                StringStringArrayDictionary = new Dictionary<string, string[]>
                {
                    { "array1", new[] { "a", "b", "c" } },
                    { "array2", new[] { "d", "e", "f" } },
                    { "array3", new[] { "g", "h", "i" } }
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

            // Test collection value dictionary
            var response = await client.GetAsync("/test?include=[StringIntListDictionary,StringStringArrayDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert StringIntListDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringIntListDictionary", out var stringIntListDictionary), "StringIntListDictionary property is present");
            
            // Check list1
            Assert.True(stringIntListDictionary.TryGetProperty("list1", out var list1), "Key 'list1' is present");
            Assert.Equal(3, list1.GetArrayLength());
            Assert.Equal(1, list1[0].GetInt32());
            Assert.Equal(2, list1[1].GetInt32());
            Assert.Equal(3, list1[2].GetInt32());
            
            // Check list2
            Assert.True(stringIntListDictionary.TryGetProperty("list2", out var list2), "Key 'list2' is present");
            Assert.Equal(3, list2.GetArrayLength());
            Assert.Equal(4, list2[0].GetInt32());
            Assert.Equal(5, list2[1].GetInt32());
            Assert.Equal(6, list2[2].GetInt32());
            
            // Check list3
            Assert.True(stringIntListDictionary.TryGetProperty("list3", out var list3), "Key 'list3' is present");
            Assert.Equal(3, list3.GetArrayLength());
            Assert.Equal(7, list3[0].GetInt32());
            Assert.Equal(8, list3[1].GetInt32());
            Assert.Equal(9, list3[2].GetInt32());

            // Assert StringStringArrayDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringStringArrayDictionary", out var stringStringArrayDictionary), "StringStringArrayDictionary property is present");
            
            // Check array1
            Assert.True(stringStringArrayDictionary.TryGetProperty("array1", out var array1), "Key 'array1' is present");
            Assert.Equal(3, array1.GetArrayLength());
            Assert.Equal("a", array1[0].GetString());
            Assert.Equal("b", array1[1].GetString());
            Assert.Equal("c", array1[2].GetString());
            
            // Check array2
            Assert.True(stringStringArrayDictionary.TryGetProperty("array2", out var array2), "Key 'array2' is present");
            Assert.Equal(3, array2.GetArrayLength());
            Assert.Equal("d", array2[0].GetString());
            Assert.Equal("e", array2[1].GetString());
            Assert.Equal("f", array2[2].GetString());
            
            // Check array3
            Assert.True(stringStringArrayDictionary.TryGetProperty("array3", out var array3), "Key 'array3' is present");
            Assert.Equal(3, array3.GetArrayLength());
            Assert.Equal("g", array3[0].GetString());
            Assert.Equal("h", array3[1].GetString());
            Assert.Equal("i", array3[2].GetString());
        }

        [Fact]
        public async Task DictionaryTypes_NestedDictionary_SerializesCorrectly()
        {
            // Create test model with nested dictionary
            var testModel = new DictionaryTypesModel
            {
                NestedStringIntDictionary = new Dictionary<string, Dictionary<string, int>>
                {
                    { 
                        "dict1", 
                        new Dictionary<string, int>
                        {
                            { "one", 1 },
                            { "two", 2 },
                            { "three", 3 }
                        }
                    },
                    { 
                        "dict2", 
                        new Dictionary<string, int>
                        {
                            { "four", 4 },
                            { "five", 5 },
                            { "six", 6 }
                        }
                    },
                    { 
                        "dict3", 
                        new Dictionary<string, int>
                        {
                            { "seven", 7 },
                            { "eight", 8 },
                            { "nine", 9 }
                        }
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

            // Test nested dictionary
            var response = await client.GetAsync("/test?include=[NestedStringIntDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert NestedStringIntDictionary
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedStringIntDictionary", out var nestedStringIntDictionary), "NestedStringIntDictionary property is present");
            
            // Check dict1
            Assert.True(nestedStringIntDictionary.TryGetProperty("dict1", out var dict1), "Key 'dict1' is present");
            Assert.True(dict1.TryGetProperty("one", out var oneValue), "Key 'one' is present in dict1");
            Assert.Equal(1, oneValue.GetInt32());
            Assert.True(dict1.TryGetProperty("two", out var twoValue), "Key 'two' is present in dict1");
            Assert.Equal(2, twoValue.GetInt32());
            Assert.True(dict1.TryGetProperty("three", out var threeValue), "Key 'three' is present in dict1");
            Assert.Equal(3, threeValue.GetInt32());
            
            // Check dict2
            Assert.True(nestedStringIntDictionary.TryGetProperty("dict2", out var dict2), "Key 'dict2' is present");
            Assert.True(dict2.TryGetProperty("four", out var fourValue), "Key 'four' is present in dict2");
            Assert.Equal(4, fourValue.GetInt32());
            Assert.True(dict2.TryGetProperty("five", out var fiveValue), "Key 'five' is present in dict2");
            Assert.Equal(5, fiveValue.GetInt32());
            Assert.True(dict2.TryGetProperty("six", out var sixValue), "Key 'six' is present in dict2");
            Assert.Equal(6, sixValue.GetInt32());
            
            // Check dict3
            Assert.True(nestedStringIntDictionary.TryGetProperty("dict3", out var dict3), "Key 'dict3' is present");
            Assert.True(dict3.TryGetProperty("seven", out var sevenValue), "Key 'seven' is present in dict3");
            Assert.Equal(7, sevenValue.GetInt32());
            Assert.True(dict3.TryGetProperty("eight", out var eightValue), "Key 'eight' is present in dict3");
            Assert.Equal(8, eightValue.GetInt32());
            Assert.True(dict3.TryGetProperty("nine", out var nineValue), "Key 'nine' is present in dict3");
            Assert.Equal(9, nineValue.GetInt32());
        }

        [Fact]
        public async Task DictionaryTypes_EmptyDictionaries_SerializeCorrectly()
        {
            // Create test model with empty dictionaries
            var testModel = new DictionaryTypesModel
            {
                // All dictionaries are already empty by default
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

            // Test empty dictionaries
            var response = await client.GetAsync("/test?include=[StringIntDictionary,StringStringDictionary,StringComplexItemDictionary,StringIntListDictionary,NestedStringIntDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert empty dictionaries
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringIntDictionary", out var stringIntDictionary), "StringIntDictionary property is present");
            Assert.Equal(0, stringIntDictionary.EnumerateObject().Count());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringStringDictionary", out var stringStringDictionary), "StringStringDictionary property is present");
            Assert.Equal(0, stringStringDictionary.EnumerateObject().Count());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringComplexItemDictionary", out var stringComplexItemDictionary), "StringComplexItemDictionary property is present");
            Assert.Equal(0, stringComplexItemDictionary.EnumerateObject().Count());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringIntListDictionary", out var stringIntListDictionary), "StringIntListDictionary property is present");
            Assert.Equal(0, stringIntListDictionary.EnumerateObject().Count());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedStringIntDictionary", out var nestedStringIntDictionary), "NestedStringIntDictionary property is present");
            Assert.Equal(0, nestedStringIntDictionary.EnumerateObject().Count());
        }

        [Fact]
        public async Task DictionaryTypes_DictionaryWithSpecialKeys_SerializesCorrectly()
        {
            // Create test model with dictionary having special keys
            var testModel = new DictionaryTypesModel
            {
                StringIntDictionary = new Dictionary<string, int>
                {
                    { "key with spaces", 1 },
                    { "key-with-hyphens", 2 },
                    { "key_with_underscores", 3 },
                    { "key.with.dots", 4 },
                    { "key@with@special@chars", 5 },
                    { "123-numeric-prefix", 6 },
                    { "very-long-key-name-that-exceeds-normal-length-limits-and-tests-serialization-of-lengthy-dictionary-keys", 7 }
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

            // Test dictionary with special keys
            var response = await client.GetAsync("/test?include=[StringIntDictionary]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert StringIntDictionary with special keys
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringIntDictionary", out var stringIntDictionary), "StringIntDictionary property is present");
            
            // Check special keys
            Assert.True(stringIntDictionary.TryGetProperty("key with spaces", out var keyWithSpacesValue), "Key 'key with spaces' is present");
            Assert.Equal(1, keyWithSpacesValue.GetInt32());
            
            Assert.True(stringIntDictionary.TryGetProperty("key-with-hyphens", out var keyWithHyphensValue), "Key 'key-with-hyphens' is present");
            Assert.Equal(2, keyWithHyphensValue.GetInt32());
            
            Assert.True(stringIntDictionary.TryGetProperty("key_with_underscores", out var keyWithUnderscoresValue), "Key 'key_with_underscores' is present");
            Assert.Equal(3, keyWithUnderscoresValue.GetInt32());
            
            Assert.True(stringIntDictionary.TryGetProperty("key.with.dots", out var keyWithDotsValue), "Key 'key.with.dots' is present");
            Assert.Equal(4, keyWithDotsValue.GetInt32());
            
            Assert.True(stringIntDictionary.TryGetProperty("key@with@special@chars", out var keyWithSpecialCharsValue), "Key 'key@with@special@chars' is present");
            Assert.Equal(5, keyWithSpecialCharsValue.GetInt32());
            
            Assert.True(stringIntDictionary.TryGetProperty("123-numeric-prefix", out var numericPrefixValue), "Key '123-numeric-prefix' is present");
            Assert.Equal(6, numericPrefixValue.GetInt32());
            
            Assert.True(stringIntDictionary.TryGetProperty("very-long-key-name-that-exceeds-normal-length-limits-and-tests-serialization-of-lengthy-dictionary-keys", out var longKeyValue), "Long key is present");
            Assert.Equal(7, longKeyValue.GetInt32());
        }
    }
}
