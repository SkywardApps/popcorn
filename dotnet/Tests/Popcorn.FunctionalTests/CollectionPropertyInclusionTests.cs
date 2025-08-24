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
    public class CollectionPropertyInclusionTests
    {
        [Fact]
        public async Task CollectionPropertyInclusion_WithNoSpecificProperties_IncludesDefaultAndAlwaysProperties()
        {
            // Create test model with items having various property attributes
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithAttributes = new List<ItemWithAttributes>
                {
                    new ItemWithAttributes 
                    { 
                        Id = 1, 
                        AlwaysIncludedProperty = "Always 1", 
                        DefaultIncludedProperty = "Default 1", 
                        NeverIncludedProperty = "Never 1", 
                        RegularProperty = "Regular 1" 
                    },
                    new ItemWithAttributes 
                    { 
                        Id = 2, 
                        AlwaysIncludedProperty = "Always 2", 
                        DefaultIncludedProperty = "Default 2", 
                        NeverIncludedProperty = "Never 2", 
                        RegularProperty = "Regular 2" 
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

            // Test collection with no specific properties requested
            var response = await client.GetAsync("/test?include=[ItemsWithAttributes]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithAttributes
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithAttributes", out var itemsWithAttributes), "ItemsWithAttributes property is present");
            Assert.Equal(2, itemsWithAttributes.GetArrayLength());
            
            // Check that only Default and Always properties are included
            Assert.True(itemsWithAttributes[0].TryGetProperty("AlwaysIncludedProperty", out _), "Always property is included");
            Assert.True(itemsWithAttributes[0].TryGetProperty("DefaultIncludedProperty", out _), "Default property is included");
            Assert.False(itemsWithAttributes[0].TryGetProperty("NeverIncludedProperty", out _), "Never property is not included");
            Assert.False(itemsWithAttributes[0].TryGetProperty("RegularProperty", out _), "Regular property is not included");
            Assert.False(itemsWithAttributes[0].TryGetProperty("Id", out _), "Id property is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithSpecificProperties_IncludesRequestedProperties()
        {
            // Create test model with items having various property attributes
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithAttributes = new List<ItemWithAttributes>
                {
                    new ItemWithAttributes 
                    { 
                        Id = 1, 
                        AlwaysIncludedProperty = "Always 1", 
                        DefaultIncludedProperty = "Default 1", 
                        NeverIncludedProperty = "Never 1", 
                        RegularProperty = "Regular 1" 
                    },
                    new ItemWithAttributes 
                    { 
                        Id = 2, 
                        AlwaysIncludedProperty = "Always 2", 
                        DefaultIncludedProperty = "Default 2", 
                        NeverIncludedProperty = "Never 2", 
                        RegularProperty = "Regular 2" 
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

            // Test collection with specific properties requested
            var response = await client.GetAsync("/test?include=[ItemsWithAttributes[Id,RegularProperty]]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithAttributes
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithAttributes", out var itemsWithAttributes), "ItemsWithAttributes property is present");
            Assert.Equal(2, itemsWithAttributes.GetArrayLength());
            
            // Check that requested properties are included
            Assert.True(itemsWithAttributes[0].TryGetProperty("Id", out var id), "Id property is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("RegularProperty", out var regularProperty), "RegularProperty is included");
            Assert.Equal("Regular 1", regularProperty.GetString());
            
            // Always properties should still be included even if not requested
            Assert.True(itemsWithAttributes[0].TryGetProperty("AlwaysIncludedProperty", out _), "Always property is still included");
            
            // Default properties should not be included if not requested
            Assert.False(itemsWithAttributes[0].TryGetProperty("DefaultIncludedProperty", out _), "Default property is not included");
            
            // Never properties should not be included even if requested
            Assert.False(itemsWithAttributes[0].TryGetProperty("NeverIncludedProperty", out _), "Never property is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithNestedObjects_IncludesNestedProperties()
        {
            // Create test model with items having nested objects
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithNestedObjects = new List<ItemWithNestedObject>
                {
                    new ItemWithNestedObject 
                    { 
                        Id = 1, 
                        Name = "Item 1", 
                        NestedObject = new NestedObject 
                        { 
                            NestedId = 101, 
                            AlwaysIncludedNestedProperty = "Always Nested 1", 
                            DefaultIncludedNestedProperty = "Default Nested 1", 
                            RegularNestedProperty = "Regular Nested 1" 
                        } 
                    },
                    new ItemWithNestedObject 
                    { 
                        Id = 2, 
                        Name = "Item 2", 
                        NestedObject = new NestedObject 
                        { 
                            NestedId = 102, 
                            AlwaysIncludedNestedProperty = "Always Nested 2", 
                            DefaultIncludedNestedProperty = "Default Nested 2", 
                            RegularNestedProperty = "Regular Nested 2" 
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

            // Test collection with nested objects and specific properties requested
            var response = await client.GetAsync("/test?include=[ItemsWithNestedObjects[Id,Name,NestedObject[NestedId,RegularNestedProperty]]]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithNestedObjects
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithNestedObjects", out var itemsWithNestedObjects), "ItemsWithNestedObjects property is present");
            Assert.Equal(2, itemsWithNestedObjects.GetArrayLength());
            
            // Check that requested properties are included
            Assert.True(itemsWithNestedObjects[0].TryGetProperty("Id", out var id), "Id property is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(itemsWithNestedObjects[0].TryGetProperty("Name", out var name), "Name property is included");
            Assert.Equal("Item 1", name.GetString());
            
            // Check nested object
            Assert.True(itemsWithNestedObjects[0].TryGetProperty("NestedObject", out var nestedObject), "NestedObject property is included");
            
            // Check nested object properties
            Assert.True(nestedObject.TryGetProperty("NestedId", out var nestedId), "NestedId property is included");
            Assert.Equal(101, nestedId.GetInt32());
            
            Assert.True(nestedObject.TryGetProperty("RegularNestedProperty", out var regularNestedProperty), "RegularNestedProperty is included");
            Assert.Equal("Regular Nested 1", regularNestedProperty.GetString());
            
            // Always properties should still be included even if not requested
            Assert.True(nestedObject.TryGetProperty("AlwaysIncludedNestedProperty", out _), "Always nested property is still included");
            
            // Default properties should not be included if not requested
            Assert.False(nestedObject.TryGetProperty("DefaultIncludedNestedProperty", out _), "Default nested property is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithNestedCollections_IncludesNestedCollectionProperties()
        {
            // Create test model with items having nested collections
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithNestedCollections = new List<ItemWithNestedCollection>
                {
                    new ItemWithNestedCollection 
                    { 
                        Id = 1, 
                        Name = "Item 1", 
                        NestedItems = new List<NestedItem> 
                        { 
                            new NestedItem 
                            { 
                                NestedItemId = 101, 
                                AlwaysIncludedNestedItemProperty = "Always Nested 101", 
                                DefaultIncludedNestedItemProperty = "Default Nested 101", 
                                RegularNestedItemProperty = "Regular Nested 101" 
                            },
                            new NestedItem 
                            { 
                                NestedItemId = 102, 
                                AlwaysIncludedNestedItemProperty = "Always Nested 102", 
                                DefaultIncludedNestedItemProperty = "Default Nested 102", 
                                RegularNestedItemProperty = "Regular Nested 102" 
                            }
                        } 
                    },
                    new ItemWithNestedCollection 
                    { 
                        Id = 2, 
                        Name = "Item 2", 
                        NestedItems = new List<NestedItem> 
                        { 
                            new NestedItem 
                            { 
                                NestedItemId = 201, 
                                AlwaysIncludedNestedItemProperty = "Always Nested 201", 
                                DefaultIncludedNestedItemProperty = "Default Nested 201", 
                                RegularNestedItemProperty = "Regular Nested 201" 
                            },
                            new NestedItem 
                            { 
                                NestedItemId = 202, 
                                AlwaysIncludedNestedItemProperty = "Always Nested 202", 
                                DefaultIncludedNestedItemProperty = "Default Nested 202", 
                                RegularNestedItemProperty = "Regular Nested 202" 
                            }
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

            // Test collection with nested collections and specific properties requested
            var response = await client.GetAsync("/test?include=[ItemsWithNestedCollections[Id,Name,NestedItems[NestedItemId,RegularNestedItemProperty]]]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithNestedCollections
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithNestedCollections", out var itemsWithNestedCollections), "ItemsWithNestedCollections property is present");
            Assert.Equal(2, itemsWithNestedCollections.GetArrayLength());
            
            // Check that requested properties are included
            Assert.True(itemsWithNestedCollections[0].TryGetProperty("Id", out var id), "Id property is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(itemsWithNestedCollections[0].TryGetProperty("Name", out var name), "Name property is included");
            Assert.Equal("Item 1", name.GetString());
            
            // Check nested collection
            Assert.True(itemsWithNestedCollections[0].TryGetProperty("NestedItems", out var nestedItems), "NestedItems property is included");
            Assert.Equal(2, nestedItems.GetArrayLength());
            
            // Check nested collection item properties
            Assert.True(nestedItems[0].TryGetProperty("NestedItemId", out var nestedItemId), "NestedItemId property is included");
            Assert.Equal(101, nestedItemId.GetInt32());
            
            Assert.True(nestedItems[0].TryGetProperty("RegularNestedItemProperty", out var regularNestedItemProperty), "RegularNestedItemProperty is included");
            Assert.Equal("Regular Nested 101", regularNestedItemProperty.GetString());
            
            // Always properties should still be included even if not requested
            Assert.True(nestedItems[0].TryGetProperty("AlwaysIncludedNestedItemProperty", out _), "Always nested item property is still included");
            
            // Default properties should not be included if not requested
            Assert.False(nestedItems[0].TryGetProperty("DefaultIncludedNestedItemProperty", out _), "Default nested item property is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithDefaultProperties_IncludesDefaultPropertiesWhenNoSpecificPropertiesRequested()
        {
            // Create test model with items having default properties
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithDefaultProperties = new List<ItemWithDefaultProperties>
                {
                    new ItemWithDefaultProperties 
                    { 
                        Id = 1, 
                        DefaultProperty1 = "Default 1-1", 
                        DefaultProperty2 = "Default 1-2", 
                        NonDefaultProperty1 = "Non-Default 1-1", 
                        NonDefaultProperty2 = "Non-Default 1-2" 
                    },
                    new ItemWithDefaultProperties 
                    { 
                        Id = 2, 
                        DefaultProperty1 = "Default 2-1", 
                        DefaultProperty2 = "Default 2-2", 
                        NonDefaultProperty1 = "Non-Default 2-1", 
                        NonDefaultProperty2 = "Non-Default 2-2" 
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

            // Test collection with default properties and no specific properties requested
            var response = await client.GetAsync("/test?include=[ItemsWithDefaultProperties]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithDefaultProperties
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithDefaultProperties", out var itemsWithDefaultProperties), "ItemsWithDefaultProperties property is present");
            Assert.Equal(2, itemsWithDefaultProperties.GetArrayLength());
            
            // Check that only Default properties are included
            Assert.True(itemsWithDefaultProperties[0].TryGetProperty("DefaultProperty1", out var defaultProperty1), "DefaultProperty1 is included");
            Assert.Equal("Default 1-1", defaultProperty1.GetString());
            
            Assert.True(itemsWithDefaultProperties[0].TryGetProperty("DefaultProperty2", out var defaultProperty2), "DefaultProperty2 is included");
            Assert.Equal("Default 1-2", defaultProperty2.GetString());
            
            // Non-default properties should not be included
            Assert.False(itemsWithDefaultProperties[0].TryGetProperty("NonDefaultProperty1", out _), "NonDefaultProperty1 is not included");
            Assert.False(itemsWithDefaultProperties[0].TryGetProperty("NonDefaultProperty2", out _), "NonDefaultProperty2 is not included");
            Assert.False(itemsWithDefaultProperties[0].TryGetProperty("Id", out _), "Id property is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithWildcardAll_IncludesAllPropertiesExceptNever()
        {
            // Create test model with items having various property attributes
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithAttributes = new List<ItemWithAttributes>
                {
                    new ItemWithAttributes 
                    { 
                        Id = 1, 
                        AlwaysIncludedProperty = "Always 1", 
                        DefaultIncludedProperty = "Default 1", 
                        NeverIncludedProperty = "Never 1", 
                        RegularProperty = "Regular 1" 
                    },
                    new ItemWithAttributes 
                    { 
                        Id = 2, 
                        AlwaysIncludedProperty = "Always 2", 
                        DefaultIncludedProperty = "Default 2", 
                        NeverIncludedProperty = "Never 2", 
                        RegularProperty = "Regular 2" 
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

            // Test collection with wildcard all
            var response = await client.GetAsync("/test?include=[ItemsWithAttributes[!all]]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithAttributes
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithAttributes", out var itemsWithAttributes), "ItemsWithAttributes property is present");
            Assert.Equal(2, itemsWithAttributes.GetArrayLength());
            
            // Check that all properties except Never are included
            Assert.True(itemsWithAttributes[0].TryGetProperty("Id", out var id), "Id property is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("AlwaysIncludedProperty", out var alwaysIncludedProperty), "AlwaysIncludedProperty is included");
            Assert.Equal("Always 1", alwaysIncludedProperty.GetString());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("DefaultIncludedProperty", out var defaultIncludedProperty), "DefaultIncludedProperty is included");
            Assert.Equal("Default 1", defaultIncludedProperty.GetString());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("RegularProperty", out var regularProperty), "RegularProperty is included");
            Assert.Equal("Regular 1", regularProperty.GetString());
            
            // Never properties should not be included even with wildcard all
            Assert.False(itemsWithAttributes[0].TryGetProperty("NeverIncludedProperty", out _), "NeverIncludedProperty is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithPropertyNegation_ExcludesNegatedProperties()
        {
            // Create test model with items having various property attributes
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithAttributes = new List<ItemWithAttributes>
                {
                    new ItemWithAttributes 
                    { 
                        Id = 1, 
                        AlwaysIncludedProperty = "Always 1", 
                        DefaultIncludedProperty = "Default 1", 
                        NeverIncludedProperty = "Never 1", 
                        RegularProperty = "Regular 1" 
                    },
                    new ItemWithAttributes 
                    { 
                        Id = 2, 
                        AlwaysIncludedProperty = "Always 2", 
                        DefaultIncludedProperty = "Default 2", 
                        NeverIncludedProperty = "Never 2", 
                        RegularProperty = "Regular 2" 
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

            // Test collection with property negation
            var response = await client.GetAsync("/test?include=[ItemsWithAttributes[!all,-RegularProperty]]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithAttributes
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithAttributes", out var itemsWithAttributes), "ItemsWithAttributes property is present");
            Assert.Equal(2, itemsWithAttributes.GetArrayLength());
            
            // Check that all properties except Never and negated properties are included
            Assert.True(itemsWithAttributes[0].TryGetProperty("Id", out var id), "Id property is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("AlwaysIncludedProperty", out var alwaysIncludedProperty), "AlwaysIncludedProperty is included");
            Assert.Equal("Always 1", alwaysIncludedProperty.GetString());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("DefaultIncludedProperty", out var defaultIncludedProperty), "DefaultIncludedProperty is included");
            Assert.Equal("Default 1", defaultIncludedProperty.GetString());
            
            // Negated properties should not be included
            Assert.False(itemsWithAttributes[0].TryGetProperty("RegularProperty", out _), "RegularProperty is not included");
            
            // Never properties should not be included
            Assert.False(itemsWithAttributes[0].TryGetProperty("NeverIncludedProperty", out _), "NeverIncludedProperty is not included");
        }

        [Fact]
        public async Task CollectionPropertyInclusion_WithAlwaysPropertyNegation_StillIncludesAlwaysProperties()
        {
            // Create test model with items having various property attributes
            var testModel = new CollectionPropertyInclusionModel
            {
                Id = 1,
                Name = "Test Model",
                ItemsWithAttributes = new List<ItemWithAttributes>
                {
                    new ItemWithAttributes 
                    { 
                        Id = 1, 
                        AlwaysIncludedProperty = "Always 1", 
                        DefaultIncludedProperty = "Default 1", 
                        NeverIncludedProperty = "Never 1", 
                        RegularProperty = "Regular 1" 
                    },
                    new ItemWithAttributes 
                    { 
                        Id = 2, 
                        AlwaysIncludedProperty = "Always 2", 
                        DefaultIncludedProperty = "Default 2", 
                        NeverIncludedProperty = "Never 2", 
                        RegularProperty = "Regular 2" 
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

            // Test collection with Always property negation
            var response = await client.GetAsync("/test?include=[ItemsWithAttributes[Id,RegularProperty,-AlwaysIncludedProperty]]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ItemsWithAttributes
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ItemsWithAttributes", out var itemsWithAttributes), "ItemsWithAttributes property is present");
            Assert.Equal(2, itemsWithAttributes.GetArrayLength());
            
            // Check that requested properties are included
            Assert.True(itemsWithAttributes[0].TryGetProperty("Id", out var id), "Id property is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(itemsWithAttributes[0].TryGetProperty("RegularProperty", out var regularProperty), "RegularProperty is included");
            Assert.Equal("Regular 1", regularProperty.GetString());
            
            // Always properties should still be included even if negated
            Assert.True(itemsWithAttributes[0].TryGetProperty("AlwaysIncludedProperty", out var alwaysIncludedProperty), "AlwaysIncludedProperty is still included despite negation");
            Assert.Equal("Always 1", alwaysIncludedProperty.GetString());
            
            // Default properties should not be included if not requested
            Assert.False(itemsWithAttributes[0].TryGetProperty("DefaultIncludedProperty", out _), "DefaultIncludedProperty is not included");
            
            // Never properties should not be included
            Assert.False(itemsWithAttributes[0].TryGetProperty("NeverIncludedProperty", out _), "NeverIncludedProperty is not included");
        }
    }
}
