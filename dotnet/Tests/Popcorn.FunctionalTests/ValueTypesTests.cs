using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Popcorn.FunctionalTests.Models;
using Popcorn.Shared;
using Xunit;

namespace Popcorn.FunctionalTests
{
    public class ValueTypesTests
    {
        [Fact]
        public async Task ValueTypes_WithAllInclude_SerializesAllProperties()
        {
            // Create test model with known values for all value types
            var testModel = new ValueTypesTestModel 
            { 
                // Simple struct
                SimpleStructValue = new SimpleStruct(42),
                
                // Struct with properties
                PointStructValue = new PointStruct(10, 20),
                
                // Struct with nested struct
                ComplexStructValue = new ComplexStruct(
                    "Complex Point", 
                    new PointStruct(30, 40)
                ),
                
                // Record struct
                PositionRecordStructValue = new PositionRecordStruct(
                    Latitude: 37.7749,
                    Longitude: -122.4194
                ),
                
                // Record (reference type)
                PersonRecordValue = new PersonRecord(
                    FirstName: "John",
                    LastName: "Doe",
                    Age: 30
                ),
                
                // ValueTuple
                ValueTupleValue = (X: 1, Y: "Hello", Z: true),
                
                // Named ValueTuple
                NamedValueTupleValue = (Id: 123, Name: "Test Item"),
                
                // Tuple (reference type)
                TupleValue = new Tuple<int, string, bool>(1, "Hello", true),
                
                // Nested ValueTuple
                NestedValueTupleValue = (
                    Id: 456, 
                    Name: (First: "Jane", Last: "Smith")
                ),
                
                // Nullable struct with value
                NullableStructValue = new SimpleStruct(99),
                
                // Nullable record struct with value
                NullableRecordStructValue = new PositionRecordStruct(
                    Latitude: 40.7128,
                    Longitude: -74.0060
                ),
                
                // Array of structs
                StructArrayValue = new SimpleStruct[]
                {
                    new SimpleStruct(1),
                    new SimpleStruct(2),
                    new SimpleStruct(3)
                },
                
                // List of record structs
                RecordStructListValue = new List<PositionRecordStruct>
                {
                    new PositionRecordStruct(37.7749, -122.4194),
                    new PositionRecordStruct(40.7128, -74.0060),
                    new PositionRecordStruct(34.0522, -118.2437)
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
            
            // Make request with include=[!all]
            var response = await client.GetAsync("/test?include=[!all]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert all properties are present
            // Simple struct
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SimpleStructValue", out var simpleStructValue), "SimpleStructValue property is present");
            Assert.Equal(42, simpleStructValue.GetProperty("Value").GetInt32());
            
            // Struct with properties
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PointStructValue", out var pointStructValue), "PointStructValue property is present");
            Assert.Equal(10, pointStructValue.GetProperty("X").GetInt32());
            Assert.Equal(20, pointStructValue.GetProperty("Y").GetInt32());
            
            // Struct with nested struct
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ComplexStructValue", out var complexStructValue), "ComplexStructValue property is present");
            Assert.Equal("Complex Point", complexStructValue.GetProperty("Name").GetString());
            Assert.Equal(30, complexStructValue.GetProperty("Point").GetProperty("X").GetInt32());
            Assert.Equal(40, complexStructValue.GetProperty("Point").GetProperty("Y").GetInt32());
            
            // Record struct
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PositionRecordStructValue", out var positionRecordStructValue), "PositionRecordStructValue property is present");
            Assert.Equal(37.7749, positionRecordStructValue.GetProperty("Latitude").GetDouble());
            Assert.Equal(-122.4194, positionRecordStructValue.GetProperty("Longitude").GetDouble());
            
            // Record (reference type)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PersonRecordValue", out var personRecordValue), "PersonRecordValue property is present");
            Assert.Equal("John", personRecordValue.GetProperty("FirstName").GetString());
            Assert.Equal("Doe", personRecordValue.GetProperty("LastName").GetString());
            Assert.Equal(30, personRecordValue.GetProperty("Age").GetInt32());
            
            // ValueTuple
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ValueTupleValue", out var valueTupleValue), "ValueTupleValue property is present");
            Assert.Equal(1, valueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Hello", valueTupleValue.GetProperty("Item2").GetString());
            Assert.True(valueTupleValue.GetProperty("Item3").GetBoolean());
            
            // Named ValueTuple
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NamedValueTupleValue", out var namedValueTupleValue), "NamedValueTupleValue property is present");
            Assert.Equal(123, namedValueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Test Item", namedValueTupleValue.GetProperty("Item2").GetString());
            
            // Tuple (reference type)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("TupleValue", out var tupleValue), "TupleValue property is present");
            Assert.Equal(1, tupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Hello", tupleValue.GetProperty("Item2").GetString());
            Assert.True(tupleValue.GetProperty("Item3").GetBoolean());
            
            // Nested ValueTuple
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedValueTupleValue", out var nestedValueTupleValue), "NestedValueTupleValue property is present");
            Assert.Equal(456, nestedValueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Jane", nestedValueTupleValue.GetProperty("Item2").GetProperty("Item1").GetString());
            Assert.Equal("Smith", nestedValueTupleValue.GetProperty("Item2").GetProperty("Item2").GetString());
            
            // Nullable struct with value
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableStructValue", out var nullableStructValue), "NullableStructValue property is present");
            Assert.Equal(99, nullableStructValue.GetProperty("Value").GetInt32());
            
            // Nullable record struct with value
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableRecordStructValue", out var nullableRecordStructValue), "NullableRecordStructValue property is present");
            Assert.Equal(40.7128, nullableRecordStructValue.GetProperty("Latitude").GetDouble());
            Assert.Equal(-74.0060, nullableRecordStructValue.GetProperty("Longitude").GetDouble());
            
            // Array of structs
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StructArrayValue", out var structArrayValue), "StructArrayValue property is present");
            Assert.Equal(3, structArrayValue.GetArrayLength());
            Assert.Equal(1, structArrayValue[0].GetProperty("Value").GetInt32());
            Assert.Equal(2, structArrayValue[1].GetProperty("Value").GetInt32());
            Assert.Equal(3, structArrayValue[2].GetProperty("Value").GetInt32());
            
            // List of record structs
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("RecordStructListValue", out var recordStructListValue), "RecordStructListValue property is present");
            Assert.Equal(3, recordStructListValue.GetArrayLength());
            Assert.Equal(37.7749, recordStructListValue[0].GetProperty("Latitude").GetDouble());
            Assert.Equal(-122.4194, recordStructListValue[0].GetProperty("Longitude").GetDouble());
            Assert.Equal(40.7128, recordStructListValue[1].GetProperty("Latitude").GetDouble());
            Assert.Equal(-74.0060, recordStructListValue[1].GetProperty("Longitude").GetDouble());
            Assert.Equal(34.0522, recordStructListValue[2].GetProperty("Latitude").GetDouble());
            Assert.Equal(-118.2437, recordStructListValue[2].GetProperty("Longitude").GetDouble());
        }

        [Fact]
        public async Task ValueTypes_WithSelectiveInclude_SerializesOnlyRequestedProperties()
        {
            // Create test model with known values
            var testModel = new ValueTypesTestModel 
            { 
                // Simple struct
                SimpleStructValue = new SimpleStruct(42),
                
                // Struct with properties
                PointStructValue = new PointStruct(10, 20),
                
                // Record (reference type)
                PersonRecordValue = new PersonRecord(
                    FirstName: "John",
                    LastName: "Doe",
                    Age: 30
                ),
                
                // ValueTuple
                ValueTupleValue = (X: 1, Y: "Hello", Z: true)
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
            
            // Make request with include=[SimpleStructValue,PersonRecordValue]
            var response = await client.GetAsync("/test?include=[SimpleStructValue[!all],PersonRecordValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert requested properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SimpleStructValue", out var simpleStructValue), "Requested SimpleStructValue property is present");
            Assert.Equal(42, simpleStructValue.GetProperty("Value").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PersonRecordValue", out var personRecordValue), "Requested PersonRecordValue property is present");
            Assert.Equal("John", personRecordValue.GetProperty("FirstName").GetString());
            Assert.Equal("Doe", personRecordValue.GetProperty("LastName").GetString());
            Assert.Equal(30, personRecordValue.GetProperty("Age").GetInt32());
            
            // Assert non-requested properties are NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("PointStructValue", out _), "Non-requested PointStructValue property is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("ValueTupleValue", out _), "Non-requested ValueTupleValue property is NOT present");
        }

        [Fact]
        public async Task ValueTypes_StructTypes_SerializesCorrectly()
        {
            // Create test model with struct values
            var testModel = new ValueTypesTestModel 
            { 
                // Simple struct
                SimpleStructValue = new SimpleStruct(42),
                
                // Struct with properties
                PointStructValue = new PointStruct(10, 20),
                
                // Struct with nested struct
                ComplexStructValue = new ComplexStruct(
                    "Complex Point", 
                    new PointStruct(30, 40)
                )
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
            
            // Make request with include=[SimpleStructValue,PointStructValue,ComplexStructValue]
            var response = await client.GetAsync("/test?include=[SimpleStructValue[!all],PointStructValue[!all],ComplexStructValue[!all,Point[!all]]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert struct values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SimpleStructValue", out var simpleStructValue), "SimpleStructValue property is present");
            Assert.Equal(42, simpleStructValue.GetProperty("Value").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PointStructValue", out var pointStructValue), "PointStructValue property is present");
            Assert.Equal(10, pointStructValue.GetProperty("X").GetInt32());
            Assert.Equal(20, pointStructValue.GetProperty("Y").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ComplexStructValue", out var complexStructValue), "ComplexStructValue property is present");
            Assert.Equal("Complex Point", complexStructValue.GetProperty("Name").GetString());
            Assert.Equal(30, complexStructValue.GetProperty("Point").GetProperty("X").GetInt32());
            Assert.Equal(40, complexStructValue.GetProperty("Point").GetProperty("Y").GetInt32());
            
            // Test default struct values
            testModel.SimpleStructValue = default;
            testModel.PointStructValue = default;
            testModel.ComplexStructValue = default;
            
            response = await client.GetAsync("/test?include=[SimpleStructValue[!all],PointStructValue[!all],ComplexStructValue[!all,Point[!all]]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert default struct values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SimpleStructValue", out simpleStructValue), "SimpleStructValue property is present");
            Assert.Equal(0, simpleStructValue.GetProperty("Value").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PointStructValue", out pointStructValue), "PointStructValue property is present");
            Assert.Equal(0, pointStructValue.GetProperty("X").GetInt32());
            Assert.Equal(0, pointStructValue.GetProperty("Y").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ComplexStructValue", out complexStructValue), "ComplexStructValue property is present");
            Assert.Null(complexStructValue.GetProperty("Name").GetString());
            Assert.Equal(0, complexStructValue.GetProperty("Point").GetProperty("X").GetInt32());
            Assert.Equal(0, complexStructValue.GetProperty("Point").GetProperty("Y").GetInt32());
        }

        [Fact]
        public async Task ValueTypes_RecordTypes_SerializesCorrectly()
        {
            // Create test model with record values
            var testModel = new ValueTypesTestModel 
            { 
                // Record struct
                PositionRecordStructValue = new PositionRecordStruct(
                    Latitude: 37.7749,
                    Longitude: -122.4194
                ),
                
                // Record (reference type)
                PersonRecordValue = new PersonRecord(
                    FirstName: "John",
                    LastName: "Doe",
                    Age: 30
                )
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
            
            // Make request with include=[PositionRecordStructValue,PersonRecordValue]
            var response = await client.GetAsync("/test?include=[PositionRecordStructValue[!all],PersonRecordValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert record values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PositionRecordStructValue", out var positionRecordStructValue), "PositionRecordStructValue property is present");
            Assert.Equal(37.7749, positionRecordStructValue.GetProperty("Latitude").GetDouble());
            Assert.Equal(-122.4194, positionRecordStructValue.GetProperty("Longitude").GetDouble());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PersonRecordValue", out var personRecordValue), "PersonRecordValue property is present");
            Assert.Equal("John", personRecordValue.GetProperty("FirstName").GetString());
            Assert.Equal("Doe", personRecordValue.GetProperty("LastName").GetString());
            Assert.Equal(30, personRecordValue.GetProperty("Age").GetInt32());
            
            // Test default record struct value
            testModel.PositionRecordStructValue = default;
            
            response = await client.GetAsync("/test?include=[PositionRecordStructValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert default record struct value
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PositionRecordStructValue", out positionRecordStructValue), "PositionRecordStructValue property is present");
            Assert.Equal(0, positionRecordStructValue.GetProperty("Latitude").GetDouble());
            Assert.Equal(0, positionRecordStructValue.GetProperty("Longitude").GetDouble());
            
            // Test null record (reference type)
            testModel.PersonRecordValue = null;
            
            response = await client.GetAsync("/test?include=[PersonRecordValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert null record value
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("PersonRecordValue", out personRecordValue), "PersonRecordValue property is present");
            Assert.True(personRecordValue.ValueKind == JsonValueKind.Null, "PersonRecordValue is null");
        }

        [Fact]
        public async Task ValueTypes_TupleTypes_SerializesCorrectly()
        {
            // Create test model with tuple values
            var testModel = new ValueTypesTestModel 
            { 
                // ValueTuple
                ValueTupleValue = (X: 1, Y: "Hello", Z: true),
                
                // Named ValueTuple
                NamedValueTupleValue = (Id: 123, Name: "Test Item"),
                
                // Tuple (reference type)
                TupleValue = new Tuple<int, string, bool>(1, "Hello", true),
                
                // Nested ValueTuple
                NestedValueTupleValue = (
                    Id: 456, 
                    Name: (First: "Jane", Last: "Smith")
                )
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
            
            // Make request with include=[ValueTupleValue,NamedValueTupleValue,TupleValue,NestedValueTupleValue]
            var response = await client.GetAsync("/test?include=[ValueTupleValue[!all],NamedValueTupleValue[!all],TupleValue[!all],NestedValueTupleValue[Item1[!all],Item2[!all]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert tuple values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ValueTupleValue", out var valueTupleValue), "ValueTupleValue property is present");
            Assert.Equal(1, valueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Hello", valueTupleValue.GetProperty("Item2").GetString());
            Assert.True(valueTupleValue.GetProperty("Item3").GetBoolean());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NamedValueTupleValue", out var namedValueTupleValue), "NamedValueTupleValue property is present");
            Assert.Equal(123, namedValueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Test Item", namedValueTupleValue.GetProperty("Item2").GetString());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("TupleValue", out var tupleValue), "TupleValue property is present");
            Assert.Equal(1, tupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Hello", tupleValue.GetProperty("Item2").GetString());
            Assert.True(tupleValue.GetProperty("Item3").GetBoolean());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NestedValueTupleValue", out var nestedValueTupleValue), "NestedValueTupleValue property is present");
            Assert.Equal(456, nestedValueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Equal("Jane", nestedValueTupleValue.GetProperty("Item2").GetProperty("Item1").GetString());
            Assert.Equal("Smith", nestedValueTupleValue.GetProperty("Item2").GetProperty("Item2").GetString());
            
            // Test default ValueTuple
            testModel.ValueTupleValue = default;
            testModel.NamedValueTupleValue = default;
            
            response = await client.GetAsync("/test?include=[ValueTupleValue[!all],NamedValueTupleValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert default ValueTuple values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ValueTupleValue", out valueTupleValue), "ValueTupleValue property is present");
            Assert.Equal(0, valueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Null(valueTupleValue.GetProperty("Item2").GetString());
            Assert.False(valueTupleValue.GetProperty("Item3").GetBoolean());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NamedValueTupleValue", out namedValueTupleValue), "NamedValueTupleValue property is present");
            Assert.Equal(0, namedValueTupleValue.GetProperty("Item1").GetInt32());
            Assert.Null(namedValueTupleValue.GetProperty("Item2").GetString());
            
            // Test null Tuple (reference type)
            testModel.TupleValue = null;
            
            response = await client.GetAsync("/test?include=[TupleValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert null Tuple value
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("TupleValue", out tupleValue), "TupleValue property is present");
            Assert.True(tupleValue.ValueKind == JsonValueKind.Null, "TupleValue is null");
        }

        [Fact]
        public async Task ValueTypes_NullableTypes_SerializesCorrectly()
        {
            // Create test model with nullable value types
            var testModel = new ValueTypesTestModel 
            { 
                // Nullable struct with value
                NullableStructValue = new SimpleStruct(99),
                
                // Nullable record struct with value
                NullableRecordStructValue = new PositionRecordStruct(
                    Latitude: 40.7128,
                    Longitude: -74.0060
                )
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
            
            // Make request with include=[NullableStructValue,NullableRecordStructValue]
            var response = await client.GetAsync("/test?include=[NullableStructValue[!all],NullableRecordStructValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert nullable value types with values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableStructValue", out var nullableStructValue), "NullableStructValue property is present");
            Assert.Equal(99, nullableStructValue.GetProperty("Value").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableRecordStructValue", out var nullableRecordStructValue), "NullableRecordStructValue property is present");
            Assert.Equal(40.7128, nullableRecordStructValue.GetProperty("Latitude").GetDouble());
            Assert.Equal(-74.0060, nullableRecordStructValue.GetProperty("Longitude").GetDouble());
            
            // Test null values
            testModel.NullableStructValue = null;
            testModel.NullableRecordStructValue = null;
            
            response = await client.GetAsync("/test?include=[NullableStructValue,NullableRecordStructValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert null values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableStructValue", out nullableStructValue), "NullableStructValue property is present");
            Assert.True(nullableStructValue.ValueKind == JsonValueKind.Null, "NullableStructValue is null");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableRecordStructValue", out nullableRecordStructValue), "NullableRecordStructValue property is present");
            Assert.True(nullableRecordStructValue.ValueKind == JsonValueKind.Null, "NullableRecordStructValue is null");
        }

        [Fact]
        public async Task ValueTypes_CollectionTypes_SerializesCorrectly()
        {
            // Create test model with collection values
            var testModel = new ValueTypesTestModel 
            { 
                // Array of structs
                StructArrayValue = new SimpleStruct[]
                {
                    new SimpleStruct(1),
                    new SimpleStruct(2),
                    new SimpleStruct(3)
                },
                
                // List of record structs
                RecordStructListValue = new List<PositionRecordStruct>
                {
                    new PositionRecordStruct(37.7749, -122.4194),
                    new PositionRecordStruct(40.7128, -74.0060),
                    new PositionRecordStruct(34.0522, -118.2437)
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
            
            // Note: We need to use [!all] for nested properties because struct fields
            // are not automatically included unless they have [Always] or [Default] attributes
            // or are explicitly requested
            
            // Make request with include=[StructArrayValue[!all],RecordStructListValue[!all]]
            var response = await client.GetAsync("/test?include=[StructArrayValue[!all],RecordStructListValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert collection values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StructArrayValue", out var structArrayValue), "StructArrayValue property is present");
            Assert.Equal(3, structArrayValue.GetArrayLength());
            Assert.Equal(1, structArrayValue[0].GetProperty("Value").GetInt32());
            Assert.Equal(2, structArrayValue[1].GetProperty("Value").GetInt32());
            Assert.Equal(3, structArrayValue[2].GetProperty("Value").GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("RecordStructListValue", out var recordStructListValue), "RecordStructListValue property is present");
            Assert.Equal(3, recordStructListValue.GetArrayLength());
            Assert.Equal(37.7749, recordStructListValue[0].GetProperty("Latitude").GetDouble());
            Assert.Equal(-122.4194, recordStructListValue[0].GetProperty("Longitude").GetDouble());
            Assert.Equal(40.7128, recordStructListValue[1].GetProperty("Latitude").GetDouble());
            Assert.Equal(-74.0060, recordStructListValue[1].GetProperty("Longitude").GetDouble());
            Assert.Equal(34.0522, recordStructListValue[2].GetProperty("Latitude").GetDouble());
            Assert.Equal(-118.2437, recordStructListValue[2].GetProperty("Longitude").GetDouble());
            
            // Test empty collections
            testModel.StructArrayValue = Array.Empty<SimpleStruct>();
            testModel.RecordStructListValue = new List<PositionRecordStruct>();
            
            response = await client.GetAsync("/test?include=[StructArrayValue[!all],RecordStructListValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert empty collections
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StructArrayValue", out structArrayValue), "StructArrayValue property is present");
            Assert.Equal(0, structArrayValue.GetArrayLength());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("RecordStructListValue", out recordStructListValue), "RecordStructListValue property is present");
            Assert.Equal(0, recordStructListValue.GetArrayLength());
            
            // Test null collections
            testModel.StructArrayValue = null;
            testModel.RecordStructListValue = null;
            
            response = await client.GetAsync("/test?include=[StructArrayValue[!all],RecordStructListValue[!all]]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert null collections
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StructArrayValue", out structArrayValue), "StructArrayValue property is present");
            Assert.True(structArrayValue.ValueKind == JsonValueKind.Null, "StructArrayValue is null");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("RecordStructListValue", out recordStructListValue), "RecordStructListValue property is present");
            Assert.True(recordStructListValue.ValueKind == JsonValueKind.Null, "RecordStructListValue is null");
        }
    }
}
