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
    public class IncludeParameterVariationTests
    {
        [Fact]
        public async Task CaseSensitivity_PropertyNames_OnlyMatchesExactCase()
        {
            // Create test model
            var testModel = new IncludeParameterTestModel 
            { 
                Id = 1, 
                Name = "Test Name", 
                UPPERCASEPROP = "UPPERCASE VALUE",
                camelCaseProp = "camelCase value",
                DefaultProperty1 = 42,
                DefaultProperty2 = "Default Value",
                AlwaysProperty = 100
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
            
            // Test with exact case match
            var response1 = await client.GetAsync("/test?include=[Name]");
            response1.EnsureSuccessStatusCode();
            
            var json1 = await response1.Content.ReadAsStringAsync();
            var result1 = JsonDocument.Parse(json1);
            
            // Assert exact case match is included
            Assert.True(result1.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Exact case match 'Name' is included");
            // Always property should be included regardless
            Assert.True(result1.RootElement.GetProperty("Data").TryGetProperty("AlwaysProperty", out _), "AlwaysProperty is always included");
            
            // Test with different case
            var response2 = await client.GetAsync("/test?include=[name]");
            response2.EnsureSuccessStatusCode();
            
            var json2 = await response2.Content.ReadAsStringAsync();
            var result2 = JsonDocument.Parse(json2);
            
            // Assert different case is NOT included
            Assert.False(result2.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Different case 'name' does NOT match 'Name'");
            // Always property should be included regardless
            Assert.True(result2.RootElement.GetProperty("Data").TryGetProperty("AlwaysProperty", out _), "AlwaysProperty is always included");
            
            // Test with uppercase property
            var response3 = await client.GetAsync("/test?include=[UPPERCASEPROP]");
            response3.EnsureSuccessStatusCode();
            
            var json3 = await response3.Content.ReadAsStringAsync();
            var result3 = JsonDocument.Parse(json3);
            
            // Assert uppercase property is included
            Assert.True(result3.RootElement.GetProperty("Data").TryGetProperty("UPPERCASEPROP", out _), "Exact case match 'UPPERCASEPROP' is included");
            // Assert lowercase version is NOT included
            Assert.False(result3.RootElement.GetProperty("Data").TryGetProperty("uppercaseprop", out var _), "Different case 'uppercaseprop' is NOT included");
        }

        [Fact]
        public async Task NegatingSpecificProperties_ExcludesNegatedProperties()
        {
            // Create test model
            var testModel = new IncludeParameterTestModel 
            { 
                Id = 1, 
                Name = "Test Name", 
                UPPERCASEPROP = "UPPERCASE VALUE",
                camelCaseProp = "camelCase value",
                DefaultProperty1 = 42,
                DefaultProperty2 = "Default Value",
                AlwaysProperty = 100
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
            
            // Test with negation in specific properties list
            var response = await client.GetAsync("/test?include=[Id,Name,-UPPERCASEPROP]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert included properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Included property 'Id' is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Included property 'Name' is present");
            // Always property should be included regardless
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysProperty", out _), "AlwaysProperty is always included");
            
            // Assert negated property is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("UPPERCASEPROP", out _), "Negated property 'UPPERCASEPROP' is NOT present");
            
            // Assert non-mentioned properties are NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("camelCaseProp", out _), "Non-mentioned property 'camelCaseProp' is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty1", out _), "Non-mentioned property 'DefaultProperty1' is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty2", out _), "Non-mentioned property 'DefaultProperty2' is NOT present");
        }

        [Fact]
        public async Task NegatingPropertiesWithAll_IncludesAllExceptNegated()
        {
            // Create test model
            var testModel = new IncludeParameterTestModel 
            { 
                Id = 1, 
                Name = "Test Name", 
                UPPERCASEPROP = "UPPERCASE VALUE",
                camelCaseProp = "camelCase value",
                DefaultProperty1 = 42,
                DefaultProperty2 = "Default Value",
                AlwaysProperty = 100
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
            
            // Test with !all and negated properties
            var response = await client.GetAsync("/test?include=[!all,-Name]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert non-negated properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Non-negated property 'Id' is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UPPERCASEPROP", out _), "Non-negated property 'UPPERCASEPROP' is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("camelCaseProp", out _), "Non-negated property 'camelCaseProp' is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty1", out _), "Non-negated property 'DefaultProperty1' is present");
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty2", out _), "Non-negated property 'DefaultProperty2' is present");
            // Always property should be included regardless
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysProperty", out _), "AlwaysProperty is always included");
            
            // Assert negated property is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Negated property 'Name' is NOT present");
        }

        [Fact]
        public async Task NegatingPropertiesWithDefault_IncludesDefaultExceptNegated()
        {
            // Create test model
            var testModel = new IncludeParameterTestModel 
            { 
                Id = 1, 
                Name = "Test Name", 
                UPPERCASEPROP = "UPPERCASE VALUE",
                camelCaseProp = "camelCase value",
                DefaultProperty1 = 42,
                DefaultProperty2 = "Default Value",
                AlwaysProperty = 100
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
            
            // Test with !default and negated properties
            var response = await client.GetAsync("/test?include=[!default,-DefaultProperty1]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert non-negated default property is present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty2", out _), "Non-negated default property 'DefaultProperty2' is present");
            // Always property should be included regardless
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("AlwaysProperty", out _), "AlwaysProperty is always included");
            
            // Assert negated default property is NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("DefaultProperty1", out _), "Negated default property 'DefaultProperty1' is NOT present");
            
            // Assert non-default properties are NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Id", out _), "Non-default property 'Id' is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("Name", out _), "Non-default property 'Name' is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("UPPERCASEPROP", out _), "Non-default property 'UPPERCASEPROP' is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("camelCaseProp", out _), "Non-default property 'camelCaseProp' is NOT present");
        }
    }
}
