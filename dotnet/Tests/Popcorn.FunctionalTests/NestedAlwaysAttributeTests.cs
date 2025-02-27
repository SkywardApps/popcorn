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
    public class NestedAlwaysAttributeTests
    {
        [Fact]
        public async Task NestedAlwaysAttribute_WithEmptyInclude_DoesNotIncludeNestedObject()
        {
            // Create test model
            var testModel = new NestedAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Name = "Test", 
                NestedObject = new NestedModel 
                { 
                    AlwaysIncludedId = 42, 
                    RegularProperty = "Nested" 
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
            
            // Make request with empty include parameter
            var response = await client.GetAsync("/test?include=[]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Top-level Id property is present");

            // Assert other top-level properties are NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Top-level Name property is NOT present");
            
            // Assert nested object is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("NestedObject", out _), "Nested object is NOT present");
        }

        [Fact]
        public async Task NestedAlwaysAttribute_WithParentPropertyInclude_IncludesNestedAlwaysProperties()
        {
            // Create test model
            var testModel = new NestedAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Name = "Test", 
                NestedObject = new NestedModel 
                { 
                    AlwaysIncludedId = 42, 
                    RegularProperty = "Nested" 
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
            
            // Make request with parent property include
            var response = await client.GetAsync("/test?include=[NestedObject]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert nested object is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedObject", out var nestedObject), "Nested object is present");
            
            // Assert Always property in nested object is present
            Assert.True(nestedObject.TryGetProperty("AlwaysIncludedId", out _), "Always property in nested object is present");
            
            // Assert regular property in nested object is NOT present
            Assert.False(nestedObject.TryGetProperty("RegularProperty", out _), "Regular property in nested object is NOT present");
        }

        [Fact]
        public async Task NestedAlwaysAttribute_WithNestedPropertyInclude_IncludesNestedAlwaysAndRequestedProperties()
        {
            // Create test model
            var testModel = new NestedAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Name = "Test", 
                NestedObject = new NestedModel 
                { 
                    AlwaysIncludedId = 42, 
                    RegularProperty = "Nested" 
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
            
            // Make request with nested property include
            var response = await client.GetAsync("/test?include=[NestedObject[RegularProperty]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert nested object is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedObject", out var nestedObject), "Nested object is present");
            
            // Assert Always property in nested object is present
            Assert.True(nestedObject.TryGetProperty("AlwaysIncludedId", out _), "Always property in nested object is present");
            
            // Assert requested property in nested object is present
            Assert.True(nestedObject.TryGetProperty("RegularProperty", out _), "Requested property in nested object is present");
        }

        [Fact]
        public async Task NestedAlwaysAttribute_WithNestedAlwaysPropertyNegation_StillIncludesNestedAlwaysProperties()
        {
            // Create test model
            var testModel = new NestedAlwaysAttributeTestModel 
            { 
                Id = 1, 
                Name = "Test", 
                NestedObject = new NestedModel 
                { 
                    AlwaysIncludedId = 42, 
                    RegularProperty = "Nested" 
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
            
            // Make request with nested Always property negation
            var response = await client.GetAsync("/test?include=[NestedObject[!AlwaysIncludedId]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert nested object is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedObject", out var nestedObject), "Nested object is present");
            
            // Assert Always property in nested object is still present despite negation
            Assert.True(nestedObject.TryGetProperty("AlwaysIncludedId", out _), "Always property in nested object is still present despite negation");
        }
    }
}
