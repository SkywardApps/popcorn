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
    public class AlwaysAttributeTests
    {
        [Fact]
        public async Task AlwaysAttribute_WithEmptyInclude_IncludesAlwaysProperties()
        {
            // Create test model
            var testModel = new AlwaysAttributeTestModel 
            { 
                AlwaysIncludedId = 1, 
                RegularProperty = "Test", 
                DefaultProperty = 42 
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
            
            // Assert Always property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysIncludedId", out _), "Always property is present");
            
            // Assert Default property is present (as it's a default property)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty", out _), "Default property is present (as it's a default property)");
            
            // Assert regular property is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("RegularProperty", out _), "Regular property is NOT present");
        }

        [Fact]
        public async Task AlwaysAttribute_WithSpecificIncludes_IncludesAlwaysAndRequestedProperties()
        {
            // Create test model
            var testModel = new AlwaysAttributeTestModel 
            { 
                AlwaysIncludedId = 1, 
                RegularProperty = "Test", 
                DefaultProperty = 42 
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
            
            // Make request with specific includes
            var response = await client.GetAsync("/test?include=[RegularProperty]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert Always property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysIncludedId", out _), "Always property is present");
            
            // Assert requested property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("RegularProperty", out _), "Requested property is present");
            
            // Assert Default property is NOT present (as it wasn't requested)
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty", out _), "Default property is NOT present (as it wasn't requested)");
        }

        [Fact]
        public async Task AlwaysAttribute_WithNegation_StillIncludesAlwaysProperties()
        {
            // Create test model
            var testModel = new AlwaysAttributeTestModel 
            { 
                AlwaysIncludedId = 1, 
                RegularProperty = "Test", 
                DefaultProperty = 42 
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
            
            // Make request with negation
            var response = await client.GetAsync("/test?include=[!AlwaysIncludedId]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert Always property is still present despite negation
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysIncludedId", out _), "Always property is still present despite negation");
        }

        [Fact]
        public async Task AlwaysAttribute_WithAllInclude_IncludesAllProperties()
        {
            // Create test model
            var testModel = new AlwaysAttributeTestModel 
            { 
                AlwaysIncludedId = 1, 
                RegularProperty = "Test", 
                DefaultProperty = 42 
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
            
            // Make request with !all include
            var response = await client.GetAsync("/test?include=[!all]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert all properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysIncludedId", out _), "AlwaysIncludedId property is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("RegularProperty", out _), "RegularProperty property is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty", out _), "DefaultProperty property is present");
        }

        [Fact]
        public async Task AlwaysAttribute_WithDefaultInclude_IncludesDefaultAndAlwaysProperties()
        {
            // Create test model
            var testModel = new AlwaysAttributeTestModel 
            { 
                AlwaysIncludedId = 1, 
                RegularProperty = "Test", 
                DefaultProperty = 42 
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
            
            // Make request with !default include
            var response = await client.GetAsync("/test?include=[!default]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert Always property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysIncludedId", out _), "Always property is present");
            
            // Assert Default property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty", out _), "Default property is present");
            
            // Assert regular property is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("RegularProperty", out _), "Regular property is NOT present");
        }
    }
}
