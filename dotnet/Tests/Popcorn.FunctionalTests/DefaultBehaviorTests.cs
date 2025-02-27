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
    public class DefaultBehaviorTests
    {
        [Fact]
        public async Task NoAttributes_AllPropertiesIncludedByDefault()
        {
            // Create test model
            var testModel = new NoAttributesModel 
            { 
                Id = 1, 
                Name = "Test", 
                Value = 42,
                Description = "Description"
            };

            // Create test server and make request with no include parameter
            var response = await GetModelResponse(testModel);
            
            // Verify all properties are included by default
            var result = JsonDocument.Parse(response);
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Name property is included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Value", out _), "Value property is included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Description", out _), "Description property is included by default");
        }

        [Fact]
        public async Task SingleDefault_OnlyDefaultPropertyIncludedByDefault()
        {
            // Create test model
            var testModel = new SingleDefaultModel 
            { 
                Id = 1, 
                Name = "Test", 
                DefaultValue = 42,
                Description = "Description"
            };

            // Create test server and make request with no include parameter
            var response = await GetModelResponse(testModel);
            
            // Verify only the default property is included
            var result = JsonDocument.Parse(response);
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is NOT included by default");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Name property is NOT included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultValue", out _), "DefaultValue property is included by default");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Description", out _), "Description property is NOT included by default");
        }

        [Fact]
        public async Task SingleAlways_OnlyAlwaysPropertyIncludedByDefault()
        {
            // Create test model
            var testModel = new SingleAlwaysModel 
            { 
                Id = 1, 
                Name = "Test", 
                AlwaysValue = 42,
                Description = "Description"
            };

            // Create test server and make request with no include parameter
            var response = await GetModelResponse(testModel);
            
            // Verify only the always property is included
            var result = JsonDocument.Parse(response);
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is NOT included by default");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Name property is NOT included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysValue", out _), "AlwaysValue property is included by default");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Description", out _), "Description property is NOT included by default");
        }

        [Fact]
        public async Task SingleNever_AllPropertiesExceptNeverIncludedByDefault()
        {
            // Create test model
            var testModel = new SingleNeverModel 
            { 
                Id = 1, 
                Name = "Test", 
                NeverValue = 42,
                Description = "Description"
            };

            // Create test server and make request with no include parameter
            var response = await GetModelResponse(testModel);
            
            // Verify all properties except the never property are included
            var result = JsonDocument.Parse(response);
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Name property is included by default");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("NeverValue", out _), "NeverValue property is NOT included");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Description", out _), "Description property is included by default");
        }

        [Fact]
        public async Task MixedAttributes_OnlyDefaultAndAlwaysPropertiesIncludedByDefault()
        {
            // Create test model
            var testModel = new MixedAttributesModel 
            { 
                Id = 1, 
                DefaultName = "Test", 
                AlwaysValue = 42,
                NeverDescription = "Never",
                IsActive = true
            };

            // Create test server and make request with no include parameter
            var response = await GetModelResponse(testModel);
            
            // Verify only default and always properties are included
            var result = JsonDocument.Parse(response);
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is NOT included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultName", out _), "DefaultName property is included by default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysValue", out _), "AlwaysValue property is included by default");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("NeverDescription", out _), "NeverDescription property is NOT included");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("IsActive", out _), "IsActive property is NOT included by default");
        }

        [Fact]
        public async Task NoAttributes_WithDefaultInclude_AllPropertiesIncluded()
        {
            // Create test model
            var testModel = new NoAttributesModel 
            { 
                Id = 1, 
                Name = "Test", 
                Value = 42,
                Description = "Description"
            };

            // Create test server and make request with !default include parameter
            var response = await GetModelResponse(testModel, "?include=[!default]");
            
            // Verify all properties are included with !default
            var result = JsonDocument.Parse(response);
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Id property is included with !default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Name property is included with !default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Value", out _), "Value property is included with !default");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Description", out _), "Description property is included with !default");
        }

        private async Task<string> GetModelResponse<T>(T model, string queryString = "")
        {
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
                            var response = context.Respond(model);
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
            
            // Make request
            var response = await client.GetAsync($"/test{queryString}");
            response.EnsureSuccessStatusCode();
            
            // Return response content
            return await response.Content.ReadAsStringAsync();
        }
    }
}
