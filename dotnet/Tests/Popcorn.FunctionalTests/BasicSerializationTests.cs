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
    public class BasicSerializationTests
    {
        [Fact]
        public async Task BasicModel_WithAllInclude_SerializesAllProperties()
        {
            // Create test model
            var testModel = new TestModel 
            { 
                Id = 1, 
                Name = "Test", 
                Value = 42 
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
            
            // Make request with include=[!all]
            var response = await client.GetAsync("/test?include=[!all]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert all properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Name property is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Value", out _), "Value property is present");
        }

        [Fact]
        public async Task BasicModel_WithSelectiveInclude_SerializesOnlyRequestedProperties()
        {
            // Create test model
            var testModel = new TestModel 
            { 
                Id = 1, 
                Name = "Test", 
                Value = 42 
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
            
            // Make request with include=[Id,Value]
            var response = await client.GetAsync("/test?include=[Id,Value]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert requested properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Requested Id property is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Value", out _), "Requested Value property is present");
            
            // Assert non-requested property is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Non-requested Name property is NOT present");
        }
    }
}
