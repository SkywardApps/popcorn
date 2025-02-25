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
    public class ConflictingAttributesTests
    {
        [Fact]
        public async Task ConflictingAttributes_AlwaysVsNever_NeverTakesPrecedence()
        {
            // Create test model
            var testModel = new ConflictingAttributesTestModel 
            { 
                AlwaysNeverProperty = 1, 
                AlwaysDefaultProperty = 42 
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
            
            // Assert Never takes precedence over Always
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysNeverProperty", out _), "Never takes precedence over Always");
        }

        [Fact]
        public async Task ConflictingAttributes_AlwaysVsDefault_BothAttributesWork()
        {
            // Create test model
            var testModel = new ConflictingAttributesTestModel 
            { 
                AlwaysNeverProperty = 1, 
                AlwaysDefaultProperty = 42 
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
            
            // Assert Always+Default property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysDefaultProperty", out _), "Always+Default property is present");
        }

        [Fact]
        public async Task ConflictingAttributes_AlwaysVsDefault_CannotBeExcluded()
        {
            // Create test model
            var testModel = new ConflictingAttributesTestModel 
            { 
                AlwaysNeverProperty = 1, 
                AlwaysDefaultProperty = 42 
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
            var response = await client.GetAsync("/test?include=[!AlwaysDefaultProperty]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert Always+Default property is still present despite negation
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysDefaultProperty", out _), "Always+Default property is still present despite negation");
        }

        [Fact]
        public async Task ConflictingAttributes_NeverVsAll_NeverTakesPrecedence()
        {
            // Create test model
            var testModel = new ConflictingAttributesTestModel 
            { 
                AlwaysNeverProperty = 1, 
                AlwaysDefaultProperty = 42 
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
            
            // Make request with !all include (which normally includes everything)
            var response = await client.GetAsync("/test?include=[!all]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert property with [Never] attribute is NOT present even with !all
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysNeverProperty", out _), "Property with [Never] attribute is NOT present even with !all");
            
            // Assert other properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysDefaultProperty", out _), "Property without [Never] attribute is present");
        }

        [Fact]
        public async Task ConflictingAttributes_NeverVsExplicitInclude_NeverTakesPrecedence()
        {
            // Create test model
            var testModel = new ConflictingAttributesTestModel 
            { 
                AlwaysNeverProperty = 1, 
                AlwaysDefaultProperty = 42 
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
            
            // Make request with explicit include of the property with [Never] attribute
            var response = await client.GetAsync("/test?include=[AlwaysNeverProperty]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert property with [Never] attribute is NOT present even when explicitly requested
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysNeverProperty", out _), "Property with [Never] attribute is NOT present even when explicitly requested");
        }
    }
}
