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
    public class BasicCollectionTypesTests
    {
        [Fact]
        public async Task BasicCollectionTypes_Arrays_SerializeCorrectly()
        {
            // Create test model with arrays
            var testModel = new BasicCollectionTypesModel
            {
                IntArray = new[] { 1, 2, 3, 4, 5 },
                StringArray = new[] { "one", "two", "three", "four", "five" }
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

            // Test arrays
            var response = await client.GetAsync("/test?include=[IntArray,StringArray]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert IntArray
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntArray", out var intArray), "IntArray property is present");
            Assert.Equal(5, intArray.GetArrayLength());
            Assert.Equal(1, intArray[0].GetInt32());
            Assert.Equal(2, intArray[1].GetInt32());
            Assert.Equal(3, intArray[2].GetInt32());
            Assert.Equal(4, intArray[3].GetInt32());
            Assert.Equal(5, intArray[4].GetInt32());

            // Assert StringArray
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringArray", out var stringArray), "StringArray property is present");
            Assert.Equal(5, stringArray.GetArrayLength());
            Assert.Equal("one", stringArray[0].GetString());
            Assert.Equal("two", stringArray[1].GetString());
            Assert.Equal("three", stringArray[2].GetString());
            Assert.Equal("four", stringArray[3].GetString());
            Assert.Equal("five", stringArray[4].GetString());
        }

        [Fact]
        public async Task BasicCollectionTypes_Lists_SerializeCorrectly()
        {
            // Create test model with lists
            var testModel = new BasicCollectionTypesModel
            {
                IntList = new List<int> { 1, 2, 3, 4, 5 },
                StringList = new List<string> { "one", "two", "three", "four", "five" }
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

            // Test lists
            var response = await client.GetAsync("/test?include=[IntList,StringList]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert IntList
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntList", out var intList), "IntList property is present");
            Assert.Equal(5, intList.GetArrayLength());
            Assert.Equal(1, intList[0].GetInt32());
            Assert.Equal(2, intList[1].GetInt32());
            Assert.Equal(3, intList[2].GetInt32());
            Assert.Equal(4, intList[3].GetInt32());
            Assert.Equal(5, intList[4].GetInt32());

            // Assert StringList
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringList", out var stringList), "StringList property is present");
            Assert.Equal(5, stringList.GetArrayLength());
            Assert.Equal("one", stringList[0].GetString());
            Assert.Equal("two", stringList[1].GetString());
            Assert.Equal("three", stringList[2].GetString());
            Assert.Equal("four", stringList[3].GetString());
            Assert.Equal("five", stringList[4].GetString());
        }

        [Fact]
        public async Task BasicCollectionTypes_IEnumerables_SerializeCorrectly()
        {
            // Create test model with IEnumerables
            var testModel = new BasicCollectionTypesModel
            {
                IntEnumerable = new List<int> { 1, 2, 3, 4, 5 },
                StringEnumerable = new List<string> { "one", "two", "three", "four", "five" }
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

            // Test IEnumerables
            var response = await client.GetAsync("/test?include=[IntEnumerable,StringEnumerable]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert IntEnumerable
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntEnumerable", out var intEnumerable), "IntEnumerable property is present");
            Assert.Equal(5, intEnumerable.GetArrayLength());
            Assert.Equal(1, intEnumerable[0].GetInt32());
            Assert.Equal(2, intEnumerable[1].GetInt32());
            Assert.Equal(3, intEnumerable[2].GetInt32());
            Assert.Equal(4, intEnumerable[3].GetInt32());
            Assert.Equal(5, intEnumerable[4].GetInt32());

            // Assert StringEnumerable
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringEnumerable", out var stringEnumerable), "StringEnumerable property is present");
            Assert.Equal(5, stringEnumerable.GetArrayLength());
            Assert.Equal("one", stringEnumerable[0].GetString());
            Assert.Equal("two", stringEnumerable[1].GetString());
            Assert.Equal("three", stringEnumerable[2].GetString());
            Assert.Equal("four", stringEnumerable[3].GetString());
            Assert.Equal("five", stringEnumerable[4].GetString());
        }

        [Fact]
        public async Task BasicCollectionTypes_ReadOnlyCollections_SerializeCorrectly()
        {
            // Create test model with ReadOnlyCollections
            var intList = new List<int> { 1, 2, 3, 4, 5 };
            var stringList = new List<string> { "one", "two", "three", "four", "five" };
            
            var testModel = new BasicCollectionTypesModel
            {
                ReadOnlyIntCollection = new System.Collections.ObjectModel.ReadOnlyCollection<int>(intList),
                ReadOnlyStringCollection = new System.Collections.ObjectModel.ReadOnlyCollection<string>(stringList)
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

            // Test ReadOnlyCollections
            var response = await client.GetAsync("/test?include=[ReadOnlyIntCollection,ReadOnlyStringCollection]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ReadOnlyIntCollection
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ReadOnlyIntCollection", out var readOnlyIntCollection), "ReadOnlyIntCollection property is present");
            Assert.Equal(5, readOnlyIntCollection.GetArrayLength());
            Assert.Equal(1, readOnlyIntCollection[0].GetInt32());
            Assert.Equal(2, readOnlyIntCollection[1].GetInt32());
            Assert.Equal(3, readOnlyIntCollection[2].GetInt32());
            Assert.Equal(4, readOnlyIntCollection[3].GetInt32());
            Assert.Equal(5, readOnlyIntCollection[4].GetInt32());

            // Assert ReadOnlyStringCollection
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ReadOnlyStringCollection", out var readOnlyStringCollection), "ReadOnlyStringCollection property is present");
            Assert.Equal(5, readOnlyStringCollection.GetArrayLength());
            Assert.Equal("one", readOnlyStringCollection[0].GetString());
            Assert.Equal("two", readOnlyStringCollection[1].GetString());
            Assert.Equal("three", readOnlyStringCollection[2].GetString());
            Assert.Equal("four", readOnlyStringCollection[3].GetString());
            Assert.Equal("five", readOnlyStringCollection[4].GetString());
        }

        [Fact]
        public async Task BasicCollectionTypes_ComplexItems_SerializeCorrectly()
        {
            // Create test model with complex items
            var testModel = new BasicCollectionTypesModel
            {
                ComplexItemsList = new List<ComplexItem>
                {
                    new ComplexItem { Id = 1, Name = "Item 1", Description = "Description 1", CreatedDate = new DateTime(2025, 1, 1) },
                    new ComplexItem { Id = 2, Name = "Item 2", Description = "Description 2", CreatedDate = new DateTime(2025, 1, 2) },
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

            // Test ComplexItemsList with no specific properties requested
            var response = await client.GetAsync("/test?include=[ComplexItemsList]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert ComplexItemsList
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ComplexItemsList", out var complexItemsList), "ComplexItemsList property is present");
            Assert.Equal(3, complexItemsList.GetArrayLength());
            
            // Check that only Default and Always properties are included
            Assert.True(complexItemsList[0].TryGetProperty("Description", out _), "Default property 'Description' is included");
            Assert.True(complexItemsList[0].TryGetProperty("CreatedDate", out _), "Always property 'CreatedDate' is included");
            Assert.False(complexItemsList[0].TryGetProperty("Id", out _), "Non-default property 'Id' is not included");
            Assert.False(complexItemsList[0].TryGetProperty("Name", out _), "Non-default property 'Name' is not included");

            // Test ComplexItemsList with specific properties requested
            response = await client.GetAsync("/test?include=[ComplexItemsList[Id,Name,Description,CreatedDate]]");
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);

            // Assert ComplexItemsList with specific properties
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ComplexItemsList", out complexItemsList), "ComplexItemsList property is present");
            Assert.Equal(3, complexItemsList.GetArrayLength());
            
            // Check that all requested properties are included
            Assert.True(complexItemsList[0].TryGetProperty("Id", out var id), "Property 'Id' is included");
            Assert.Equal(1, id.GetInt32());
            
            Assert.True(complexItemsList[0].TryGetProperty("Name", out var name), "Property 'Name' is included");
            Assert.Equal("Item 1", name.GetString());
            
            Assert.True(complexItemsList[0].TryGetProperty("Description", out var description), "Property 'Description' is included");
            Assert.Equal("Description 1", description.GetString());
            
            Assert.True(complexItemsList[0].TryGetProperty("CreatedDate", out var createdDate), "Property 'CreatedDate' is included");
            Assert.Equal(new DateTime(2025, 1, 1), DateTime.Parse(createdDate.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
        }

        [Fact]
        public async Task BasicCollectionTypes_NestedCollections_SerializeCorrectly()
        {
            // Create test model with nested collections
            var testModel = new BasicCollectionTypesModel
            {
                NestedIntLists = new List<List<int>>
                {
                    new List<int> { 1, 2, 3 },
                    new List<int> { 4, 5, 6 },
                    new List<int> { 7, 8, 9 }
                },
                ListOfStringArrays = new List<string[]>
                {
                    new[] { "a", "b", "c" },
                    new[] { "d", "e", "f" },
                    new[] { "g", "h", "i" }
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

            // Test nested collections
            var response = await client.GetAsync("/test?include=[NestedIntLists,ListOfStringArrays]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert NestedIntLists
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedIntLists", out var nestedIntLists), "NestedIntLists property is present");
            Assert.Equal(3, nestedIntLists.GetArrayLength());
            
            // Check first nested list
            Assert.Equal(3, nestedIntLists[0].GetArrayLength());
            Assert.Equal(1, nestedIntLists[0][0].GetInt32());
            Assert.Equal(2, nestedIntLists[0][1].GetInt32());
            Assert.Equal(3, nestedIntLists[0][2].GetInt32());
            
            // Check second nested list
            Assert.Equal(3, nestedIntLists[1].GetArrayLength());
            Assert.Equal(4, nestedIntLists[1][0].GetInt32());
            Assert.Equal(5, nestedIntLists[1][1].GetInt32());
            Assert.Equal(6, nestedIntLists[1][2].GetInt32());
            
            // Check third nested list
            Assert.Equal(3, nestedIntLists[2].GetArrayLength());
            Assert.Equal(7, nestedIntLists[2][0].GetInt32());
            Assert.Equal(8, nestedIntLists[2][1].GetInt32());
            Assert.Equal(9, nestedIntLists[2][2].GetInt32());

            // Assert ListOfStringArrays
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ListOfStringArrays", out var listOfStringArrays), "ListOfStringArrays property is present");
            Assert.Equal(3, listOfStringArrays.GetArrayLength());
            
            // Check first string array
            Assert.Equal(3, listOfStringArrays[0].GetArrayLength());
            Assert.Equal("a", listOfStringArrays[0][0].GetString());
            Assert.Equal("b", listOfStringArrays[0][1].GetString());
            Assert.Equal("c", listOfStringArrays[0][2].GetString());
            
            // Check second string array
            Assert.Equal(3, listOfStringArrays[1].GetArrayLength());
            Assert.Equal("d", listOfStringArrays[1][0].GetString());
            Assert.Equal("e", listOfStringArrays[1][1].GetString());
            Assert.Equal("f", listOfStringArrays[1][2].GetString());
            
            // Check third string array
            Assert.Equal(3, listOfStringArrays[2].GetArrayLength());
            Assert.Equal("g", listOfStringArrays[2][0].GetString());
            Assert.Equal("h", listOfStringArrays[2][1].GetString());
            Assert.Equal("i", listOfStringArrays[2][2].GetString());
        }

        [Fact]
        public async Task BasicCollectionTypes_EmptyCollections_SerializeCorrectly()
        {
            // Create test model with empty collections
            var testModel = new BasicCollectionTypesModel
            {
                // All collections are already empty by default
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
            var response = await client.GetAsync("/test?include=[IntArray,StringArray,IntList,StringList,ComplexItemsList,NestedIntLists,ListOfStringArrays]");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Assert empty collections
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntArray", out var intArray), "IntArray property is present");
            Assert.Equal(0, intArray.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringArray", out var stringArray), "StringArray property is present");
            Assert.Equal(0, stringArray.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntList", out var intList), "IntList property is present");
            Assert.Equal(0, intList.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringList", out var stringList), "StringList property is present");
            Assert.Equal(0, stringList.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ComplexItemsList", out var complexItemsList), "ComplexItemsList property is present");
            Assert.Equal(0, complexItemsList.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedIntLists", out var nestedIntLists), "NestedIntLists property is present");
            Assert.Equal(0, nestedIntLists.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ListOfStringArrays", out var listOfStringArrays), "ListOfStringArrays property is present");
            Assert.Equal(0, listOfStringArrays.GetArrayLength());
        }
    }
}
