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
    public class CollectionAlwaysAttributeTests
    {
        [Fact]
        public async Task CollectionAlwaysAttribute_WithCollectionInclude_IncludesCollectionItemAlwaysProperties()
        {
            // Create test model
            var testModel = new CollectionAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Items = new List<ItemModel> 
                { 
                    new ItemModel { AlwaysIncludedId = 42, RegularProperty = "Item1" },
                    new ItemModel { AlwaysIncludedId = 43, RegularProperty = "Item2" }
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
            
            // Make request with collection include
            var response = await client.GetAsync("/test?include=[Items]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert collection is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Items", out var items), "Collection is present");
            
            // Assert collection has items
            Assert.True(items.GetArrayLength() > 0, "Collection has items");
            
            // Assert Always property in first item is present
            Assert.True(items[0].TryGetProperty("AlwaysIncludedId", out _), "Always property in first item is present");
            
            // Assert regular property in first item is NOT present
            Assert.False(items[0].TryGetProperty("RegularProperty", out _), "Regular property in first item is NOT present");
        }

        [Fact]
        public async Task CollectionAlwaysAttribute_WithCollectionItemPropertyInclude_IncludesCollectionItemAlwaysAndRequestedProperties()
        {
            // Create test model
            var testModel = new CollectionAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Items = new List<ItemModel> 
                { 
                    new ItemModel { AlwaysIncludedId = 42, RegularProperty = "Item1" },
                    new ItemModel { AlwaysIncludedId = 43, RegularProperty = "Item2" }
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
            
            // Make request with collection item property include
            var response = await client.GetAsync("/test?include=[Items[RegularProperty]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert collection is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Items", out var items), "Collection is present");
            
            // Assert collection has items
            Assert.True(items.GetArrayLength() > 0, "Collection has items");
            
            // Assert Always property in first item is present
            Assert.True(items[0].TryGetProperty("AlwaysIncludedId", out _), "Always property in first item is present");
            
            // Assert requested property in first item is present
            Assert.True(items[0].TryGetProperty("RegularProperty", out _), "Requested property in first item is present");
        }

        [Fact]
        public async Task CollectionAlwaysAttribute_WithCollectionItemAlwaysPropertyNegation_StillIncludesCollectionItemAlwaysProperties()
        {
            // Create test model
            var testModel = new CollectionAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Items = new List<ItemModel> 
                { 
                    new ItemModel { AlwaysIncludedId = 42, RegularProperty = "Item1" },
                    new ItemModel { AlwaysIncludedId = 43, RegularProperty = "Item2" }
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
            
            // Make request with collection item Always property negation
            var response = await client.GetAsync("/test?include=[Items[!AlwaysIncludedId]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert collection is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Items", out var items), "Collection is present");
            
            // Assert collection has items
            Assert.True(items.GetArrayLength() > 0, "Collection has items");
            
            // Assert Always property in first item is still present despite negation
            Assert.True(items[0].TryGetProperty("AlwaysIncludedId", out _), "Always property in first item is still present despite negation");
        }
    }
}
