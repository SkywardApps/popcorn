using System;
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
    public class PrimitiveTypesTests
    {
        [Fact]
        public async Task PrimitiveTypes_WithAllInclude_SerializesAllProperties()
        {
            // Create test model with known values for all primitive types
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Integer types
                IntValue = 42,
                LongValue = 9223372036854775807, // Max long value
                ShortValue = 32767, // Max short value
                ByteValue = 255, // Max byte value
                SByteValue = -128, // Min sbyte value
                UIntValue = 4294967295, // Max uint value
                ULongValue = 18446744073709551615, // Max ulong value
                UShortValue = 65535, // Max ushort value
                
                // Floating point types
                FloatValue = 3.14159f,
                DoubleValue = 3.14159265359,
                DecimalValue = 3.1415926535897932384626433832m,
                
                // Boolean type
                BoolValue = true,
                
                // Character type
                CharValue = 'A',
                
                // String type
                StringValue = "Hello, World!",
                
                // Date and time types
                DateTimeValue = new DateTime(2025, 2, 25, 12, 0, 0, DateTimeKind.Utc),
                DateTimeOffsetValue = new DateTimeOffset(2025, 2, 25, 12, 0, 0, TimeSpan.FromHours(-5)),
                TimeSpanValue = TimeSpan.FromHours(24),
                
                // Identifier type
                GuidValue = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                
                // Nullable primitive types with values
                NullableIntValue = 100,
                NullableDoubleValue = 2.71828,
                NullableBoolValue = false,
                NullableDateTimeValue = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                NullableGuidValue = Guid.Parse("00000000-0000-0000-0000-000000000002")
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
            // Integer types
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntValue", out var intValue), "IntValue property is present");
            Assert.Equal(42, intValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("LongValue", out var longValue), "LongValue property is present");
            Assert.Equal(9223372036854775807, longValue.GetInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ShortValue", out var shortValue), "ShortValue property is present");
            Assert.Equal(32767, shortValue.GetInt16());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ByteValue", out var byteValue), "ByteValue property is present");
            Assert.Equal(255, byteValue.GetByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SByteValue", out var sbyteValue), "SByteValue property is present");
            Assert.Equal(-128, sbyteValue.GetSByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UIntValue", out var uintValue), "UIntValue property is present");
            Assert.Equal(4294967295, uintValue.GetUInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ULongValue", out var ulongValue), "ULongValue property is present");
            Assert.Equal(18446744073709551615, ulongValue.GetUInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UShortValue", out var ushortValue), "UShortValue property is present");
            Assert.Equal(65535, ushortValue.GetUInt16());
            
            // Floating point types
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("FloatValue", out var floatValue), "FloatValue property is present");
            Assert.Equal(3.14159f, floatValue.GetSingle());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DoubleValue", out var doubleValue), "DoubleValue property is present");
            Assert.Equal(3.14159265359, doubleValue.GetDouble());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DecimalValue", out var decimalValue), "DecimalValue property is present");
            Assert.Equal(3.1415926535897932384626433832m, decimalValue.GetDecimal());
            
            // Boolean type
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("BoolValue", out var boolValue), "BoolValue property is present");
            Assert.True(boolValue.GetBoolean());
            
            // Character type
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("CharValue", out var charValue), "CharValue property is present");
            Assert.Equal("A", charValue.GetString());
            
            // String type
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var stringValue), "StringValue property is present");
            Assert.Equal("Hello, World!", stringValue.GetString());
            
            // Date and time types
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeValue", out var dateTimeValue), "DateTimeValue property is present");
            Assert.Equal(new DateTime(2025, 2, 25, 12, 0, 0, DateTimeKind.Utc), DateTime.Parse(dateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeOffsetValue", out var dateTimeOffsetValue), "DateTimeOffsetValue property is present");
            Assert.Equal(new DateTimeOffset(2025, 2, 25, 12, 0, 0, TimeSpan.FromHours(-5)), DateTimeOffset.Parse(dateTimeOffsetValue.GetString()));
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("TimeSpanValue", out var timeSpanValue), "TimeSpanValue property is present");
            Assert.Equal(TimeSpan.FromHours(24), TimeSpan.Parse(timeSpanValue.GetString()));
            
            // Identifier type
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("GuidValue", out var guidValue), "GuidValue property is present");
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse(guidValue.GetString()));
            
            // Nullable primitive types
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableIntValue", out var nullableIntValue), "NullableIntValue property is present");
            Assert.Equal(100, nullableIntValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDoubleValue", out var nullableDoubleValue), "NullableDoubleValue property is present");
            Assert.Equal(2.71828, nullableDoubleValue.GetDouble());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableBoolValue", out var nullableBoolValue), "NullableBoolValue property is present");
            Assert.False(nullableBoolValue.GetBoolean());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDateTimeValue", out var nullableDateTimeValue), "NullableDateTimeValue property is present");
            Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.Parse(nullableDateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableGuidValue", out var nullableGuidValue), "NullableGuidValue property is present");
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), Guid.Parse(nullableGuidValue.GetString()));
        }

        [Fact]
        public async Task PrimitiveTypes_WithSelectiveInclude_SerializesOnlyRequestedProperties()
        {
            // Create test model with known values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Integer types
                IntValue = 42,
                LongValue = 9223372036854775807,
                
                // Floating point types
                DoubleValue = 3.14159265359,
                
                // Boolean type
                BoolValue = true,
                
                // String type
                StringValue = "Hello, World!",
                
                // Date and time types
                DateTimeValue = new DateTime(2025, 2, 25, 12, 0, 0, DateTimeKind.Utc),
                
                // Identifier type
                GuidValue = Guid.Parse("00000000-0000-0000-0000-000000000001")
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
            
            // Make request with include=[IntValue,StringValue,BoolValue]
            var response = await client.GetAsync("/test?include=[IntValue,StringValue,BoolValue]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert requested properties are present
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntValue", out var intValue), "Requested IntValue property is present");
            Assert.Equal(42, intValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var stringValue), "Requested StringValue property is present");
            Assert.Equal("Hello, World!", stringValue.GetString());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("BoolValue", out var boolValue), "Requested BoolValue property is present");
            Assert.True(boolValue.GetBoolean());
            
            // Assert non-requested properties are NOT present
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("LongValue", out _), "Non-requested LongValue property is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("DoubleValue", out _), "Non-requested DoubleValue property is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeValue", out _), "Non-requested DateTimeValue property is NOT present");
            Assert.False(result.RootElement.GetProperty("Data").TryGetProperty("GuidValue", out _), "Non-requested GuidValue property is NOT present");
        }

        [Fact]
        public async Task PrimitiveTypes_NumericTypes_SerializesCorrectly()
        {
            // Create test model with edge case values (min/max, zero, negative)
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Integer types with edge cases
                IntValue = int.MaxValue,
                LongValue = long.MinValue,
                ShortValue = 0,
                ByteValue = byte.MaxValue,
                SByteValue = sbyte.MinValue,
                UIntValue = uint.MaxValue,
                ULongValue = ulong.MaxValue,
                UShortValue = ushort.MaxValue,
                
                // Floating point types with precision tests
                FloatValue = 3.1415926535897932384626433832795f, // Will lose precision
                DoubleValue = 3.1415926535897932384626433832795, // More precision
                DecimalValue = 3.1415926535897932384626433832795m // Most precision
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
            
            // Make request with include=[IntValue,LongValue,ShortValue,ByteValue,SByteValue,UIntValue,ULongValue,UShortValue,FloatValue,DoubleValue,DecimalValue]
            var response = await client.GetAsync("/test?include=[IntValue,LongValue,ShortValue,ByteValue,SByteValue,UIntValue,ULongValue,UShortValue,FloatValue,DoubleValue,DecimalValue]");
            response.EnsureSuccessStatusCode();
            
            // Verify response
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert integer types
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntValue", out var intValue), "IntValue property is present");
            Assert.Equal(int.MaxValue, intValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("LongValue", out var longValue), "LongValue property is present");
            Assert.Equal(long.MinValue, longValue.GetInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ShortValue", out var shortValue), "ShortValue property is present");
            Assert.Equal(0, shortValue.GetInt16());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ByteValue", out var byteValue), "ByteValue property is present");
            Assert.Equal(byte.MaxValue, byteValue.GetByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SByteValue", out var sbyteValue), "SByteValue property is present");
            Assert.Equal(sbyte.MinValue, sbyteValue.GetSByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UIntValue", out var uintValue), "UIntValue property is present");
            Assert.Equal(uint.MaxValue, uintValue.GetUInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ULongValue", out var ulongValue), "ULongValue property is present");
            Assert.Equal(ulong.MaxValue, ulongValue.GetUInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UShortValue", out var ushortValue), "UShortValue property is present");
            Assert.Equal(ushort.MaxValue, ushortValue.GetUInt16());
            
            // Assert floating point types and precision
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("FloatValue", out var floatValue), "FloatValue property is present");
            // Float will lose precision, so we use approximate comparison
            Assert.True(Math.Abs(3.1415926535897932384626433832795f - floatValue.GetSingle()) < 0.0001f, "FloatValue has expected precision");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DoubleValue", out var doubleValue), "DoubleValue property is present");
            // Double has more precision than float but still loses some
            Assert.True(Math.Abs(3.1415926535897932384626433832795 - doubleValue.GetDouble()) < 0.0000000000001, "DoubleValue has expected precision");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DecimalValue", out var decimalValue), "DecimalValue property is present");
            // Decimal has the most precision for financial calculations
            Assert.Equal(3.1415926535897932384626433832795m, decimalValue.GetDecimal());
        }

        [Fact]
        public async Task PrimitiveTypes_StringType_SerializesCorrectly()
        {
            // Create test model with various string values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Empty string
                StringValue = string.Empty
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
            
            // Test empty string
            var response = await client.GetAsync("/test?include=[StringValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var emptyStringValue), "StringValue property is present");
            Assert.Equal(string.Empty, emptyStringValue.GetString());
            
            // Test string with special characters
            testModel.StringValue = "Special chars: !@#$%^&*()_+{}|:<>?[];',./\\\"";
            
            response = await client.GetAsync("/test?include=[StringValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var specialCharsValue), "StringValue property is present");
            Assert.Equal("Special chars: !@#$%^&*()_+{}|:<>?[];',./\\\"", specialCharsValue.GetString());
            
            // Test very long string
            testModel.StringValue = new string('a', 10000); // 10,000 'a' characters
            
            response = await client.GetAsync("/test?include=[StringValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var longStringValue), "StringValue property is present");
            Assert.Equal(10000, longStringValue.GetString().Length);
            Assert.Equal(new string('a', 10000), longStringValue.GetString());
            
            // Test Unicode characters
            testModel.StringValue = "Unicode: 你好, こんにちは, 안녕하세요, Привет, مرحبا, שלום";
            
            response = await client.GetAsync("/test?include=[StringValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var unicodeValue), "StringValue property is present");
            Assert.Equal("Unicode: 你好, こんにちは, 안녕하세요, Привет, مرحبا, שלום", unicodeValue.GetString());
        }

        [Fact]
        public async Task PrimitiveTypes_BooleanType_SerializesCorrectly()
        {
            // Create test model with true value
            var testModel = new PrimitiveTypesTestModel 
            { 
                BoolValue = true
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
            
            // Test true value
            var response = await client.GetAsync("/test?include=[BoolValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("BoolValue", out var trueValue), "BoolValue property is present");
            Assert.True(trueValue.GetBoolean());
            
            // Test false value
            testModel.BoolValue = false;
            
            response = await client.GetAsync("/test?include=[BoolValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("BoolValue", out var falseValue), "BoolValue property is present");
            Assert.False(falseValue.GetBoolean());
        }

        [Fact]
        public async Task PrimitiveTypes_DateTimeTypes_SerializesCorrectly()
        {
            // Create test model with various date/time values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // UTC DateTime
                DateTimeValue = new DateTime(2025, 2, 25, 12, 0, 0, DateTimeKind.Utc),
                
                // DateTimeOffset with timezone
                DateTimeOffsetValue = new DateTimeOffset(2025, 2, 25, 12, 0, 0, TimeSpan.FromHours(-5)),
                
                // TimeSpan
                TimeSpanValue = TimeSpan.FromDays(1.5) // 1 day and 12 hours
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
            
            // Test date/time values
            var response = await client.GetAsync("/test?include=[DateTimeValue,DateTimeOffsetValue,TimeSpanValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert DateTime
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeValue", out var dateTimeValue), "DateTimeValue property is present");
            var parsedDateTime = DateTime.Parse(dateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            Assert.Equal(2025, parsedDateTime.Year);
            Assert.Equal(2, parsedDateTime.Month);
            Assert.Equal(25, parsedDateTime.Day);
            Assert.Equal(12, parsedDateTime.Hour);
            Assert.Equal(0, parsedDateTime.Minute);
            Assert.Equal(0, parsedDateTime.Second);
            
            // Assert DateTimeOffset
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeOffsetValue", out var dateTimeOffsetValue), "DateTimeOffsetValue property is present");
            var parsedDateTimeOffset = DateTimeOffset.Parse(dateTimeOffsetValue.GetString());
            Assert.Equal(2025, parsedDateTimeOffset.Year);
            Assert.Equal(2, parsedDateTimeOffset.Month);
            Assert.Equal(25, parsedDateTimeOffset.Day);
            Assert.Equal(12, parsedDateTimeOffset.Hour);
            Assert.Equal(0, parsedDateTimeOffset.Minute);
            Assert.Equal(0, parsedDateTimeOffset.Second);
            Assert.Equal(TimeSpan.FromHours(-5), parsedDateTimeOffset.Offset);
            
            // Assert TimeSpan
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("TimeSpanValue", out var timeSpanValue), "TimeSpanValue property is present");
            var parsedTimeSpan = TimeSpan.Parse(timeSpanValue.GetString());
            Assert.Equal(1, parsedTimeSpan.Days);
            Assert.Equal(12, parsedTimeSpan.Hours);
            Assert.Equal(0, parsedTimeSpan.Minutes);
            Assert.Equal(0, parsedTimeSpan.Seconds);
            
            // Test minimum DateTime
            testModel.DateTimeValue = DateTime.MinValue;
            
            response = await client.GetAsync("/test?include=[DateTimeValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeValue", out var minDateTimeValue), "DateTimeValue property is present");
            Assert.Equal(DateTime.MinValue, DateTime.Parse(minDateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
            
            // Test maximum DateTime
            testModel.DateTimeValue = DateTime.MaxValue;
            
            response = await client.GetAsync("/test?include=[DateTimeValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeValue", out var maxDateTimeValue), "DateTimeValue property is present");
            Assert.Equal(DateTime.MaxValue, DateTime.Parse(maxDateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
        }

        [Fact]
        public async Task PrimitiveTypes_GuidType_SerializesCorrectly()
        {
            // Create test model with various Guid values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Empty Guid
                GuidValue = Guid.Empty
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
            
            // Test empty Guid
            var response = await client.GetAsync("/test?include=[GuidValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("GuidValue", out var emptyGuidValue), "GuidValue property is present");
            Assert.Equal(Guid.Empty, Guid.Parse(emptyGuidValue.GetString()));
            
            // Test specific Guid
            var specificGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            testModel.GuidValue = specificGuid;
            
            response = await client.GetAsync("/test?include=[GuidValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("GuidValue", out var specificGuidValue), "GuidValue property is present");
            Assert.Equal(specificGuid, Guid.Parse(specificGuidValue.GetString()));
            
            // Test random Guid
            var randomGuid = Guid.NewGuid();
            testModel.GuidValue = randomGuid;
            
            response = await client.GetAsync("/test?include=[GuidValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("GuidValue", out var randomGuidValue), "GuidValue property is present");
            Assert.Equal(randomGuid, Guid.Parse(randomGuidValue.GetString()));
        }

        [Fact]
        public async Task PrimitiveTypes_NullableTypes_SerializesCorrectly()
        {
            // Create test model with null values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Nullable primitive types with null values
                NullableIntValue = null,
                NullableDoubleValue = null,
                NullableBoolValue = null,
                NullableDateTimeValue = null,
                NullableGuidValue = null
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
            
            // Test null values
            var response = await client.GetAsync("/test?include=[NullableIntValue,NullableDoubleValue,NullableBoolValue,NullableDateTimeValue,NullableGuidValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert null values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableIntValue", out var nullIntValue), "NullableIntValue property is present");
            Assert.True(nullIntValue.ValueKind == JsonValueKind.Null, "NullableIntValue is null");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDoubleValue", out var nullDoubleValue), "NullableDoubleValue property is present");
            Assert.True(nullDoubleValue.ValueKind == JsonValueKind.Null, "NullableDoubleValue is null");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableBoolValue", out var nullBoolValue), "NullableBoolValue property is present");
            Assert.True(nullBoolValue.ValueKind == JsonValueKind.Null, "NullableBoolValue is null");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDateTimeValue", out var nullDateTimeValue), "NullableDateTimeValue property is present");
            Assert.True(nullDateTimeValue.ValueKind == JsonValueKind.Null, "NullableDateTimeValue is null");
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableGuidValue", out var nullGuidValue), "NullableGuidValue property is present");
            Assert.True(nullGuidValue.ValueKind == JsonValueKind.Null, "NullableGuidValue is null");
            
            // Test non-null values
            testModel.NullableIntValue = 42;
            testModel.NullableDoubleValue = 3.14;
            testModel.NullableBoolValue = true;
            testModel.NullableDateTimeValue = new DateTime(2025, 2, 25, 12, 0, 0, DateTimeKind.Utc);
            testModel.NullableGuidValue = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            
            response = await client.GetAsync("/test?include=[NullableIntValue,NullableDoubleValue,NullableBoolValue,NullableDateTimeValue,NullableGuidValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert non-null values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableIntValue", out var nonNullIntValue), "NullableIntValue property is present");
            Assert.Equal(42, nonNullIntValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDoubleValue", out var nonNullDoubleValue), "NullableDoubleValue property is present");
            Assert.Equal(3.14, nonNullDoubleValue.GetDouble());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableBoolValue", out var nonNullBoolValue), "NullableBoolValue property is present");
            Assert.True(nonNullBoolValue.GetBoolean());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDateTimeValue", out var nonNullDateTimeValue), "NullableDateTimeValue property is present");
            Assert.Equal(new DateTime(2025, 2, 25, 12, 0, 0, DateTimeKind.Utc), DateTime.Parse(nonNullDateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableGuidValue", out var nonNullGuidValue), "NullableGuidValue property is present");
            Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789abc"), Guid.Parse(nonNullGuidValue.GetString()));
        }

        [Fact]
        public async Task PrimitiveTypes_DefaultValues_SerializesCorrectly()
        {
            // Create test model with default values
            var testModel = new PrimitiveTypesTestModel();
            // Default values are already set by the compiler/runtime

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
            
            // Test default values
            var response = await client.GetAsync("/test?include=[!all]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert default values for numeric types
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntValue", out var intValue), "IntValue property is present");
            Assert.Equal(0, intValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("LongValue", out var longValue), "LongValue property is present");
            Assert.Equal(0, longValue.GetInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("FloatValue", out var floatValue), "FloatValue property is present");
            Assert.Equal(0, floatValue.GetSingle());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DoubleValue", out var doubleValue), "DoubleValue property is present");
            Assert.Equal(0, doubleValue.GetDouble());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DecimalValue", out var decimalValue), "DecimalValue property is present");
            Assert.Equal(0, decimalValue.GetDecimal());
            
            // Assert default value for boolean
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("BoolValue", out var boolValue), "BoolValue property is present");
            Assert.False(boolValue.GetBoolean());
            
            // Assert default value for char (should be '\0')
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("CharValue", out var charValue), "CharValue property is present");
            Assert.Equal("\0", charValue.GetString());
            
            // Assert default value for string (should be empty string from the model)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var stringValue), "StringValue property is present");
            Assert.Equal(string.Empty, stringValue.GetString());
            
            // Assert default value for DateTime (should be DateTime.MinValue)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DateTimeValue", out var dateTimeValue), "DateTimeValue property is present");
            Assert.Equal(default(DateTime), DateTime.Parse(dateTimeValue.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind));
            
            // Assert default value for Guid (should be Guid.Empty)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("GuidValue", out var guidValue), "GuidValue property is present");
            Assert.Equal(Guid.Empty, Guid.Parse(guidValue.GetString()));
            
            // Assert default values for nullable types (should be null)
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableIntValue", out var nullableIntValue), "NullableIntValue property is present");
            Assert.True(nullableIntValue.ValueKind == JsonValueKind.Null, "NullableIntValue is null by default");
        }

        [Fact]
        public async Task PrimitiveTypes_BoundaryValues_SerializesCorrectly()
        {
            // Create test model with boundary values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Integer boundary values
                IntValue = int.MinValue,
                LongValue = long.MinValue,
                ShortValue = short.MinValue,
                ByteValue = byte.MinValue,
                SByteValue = sbyte.MinValue,
                UIntValue = uint.MinValue,
                ULongValue = ulong.MinValue,
                UShortValue = ushort.MinValue
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
            
            // Test minimum boundary values
            var response = await client.GetAsync("/test?include=[IntValue,LongValue,ShortValue,ByteValue,SByteValue,UIntValue,ULongValue,UShortValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert minimum boundary values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntValue", out var intValue), "IntValue property is present");
            Assert.Equal(int.MinValue, intValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("LongValue", out var longValue), "LongValue property is present");
            Assert.Equal(long.MinValue, longValue.GetInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ShortValue", out var shortValue), "ShortValue property is present");
            Assert.Equal(short.MinValue, shortValue.GetInt16());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ByteValue", out var byteValue), "ByteValue property is present");
            Assert.Equal(byte.MinValue, byteValue.GetByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SByteValue", out var sbyteValue), "SByteValue property is present");
            Assert.Equal(sbyte.MinValue, sbyteValue.GetSByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UIntValue", out var uintValue), "UIntValue property is present");
            Assert.Equal(uint.MinValue, uintValue.GetUInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ULongValue", out var ulongValue), "ULongValue property is present");
            Assert.Equal(ulong.MinValue, ulongValue.GetUInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UShortValue", out var ushortValue), "UShortValue property is present");
            Assert.Equal(ushort.MinValue, ushortValue.GetUInt16());
            
            // Test maximum boundary values
            testModel.IntValue = int.MaxValue;
            testModel.LongValue = long.MaxValue;
            testModel.ShortValue = short.MaxValue;
            testModel.ByteValue = byte.MaxValue;
            testModel.SByteValue = sbyte.MaxValue;
            testModel.UIntValue = uint.MaxValue;
            testModel.ULongValue = ulong.MaxValue;
            testModel.UShortValue = ushort.MaxValue;
            
            response = await client.GetAsync("/test?include=[IntValue,LongValue,ShortValue,ByteValue,SByteValue,UIntValue,ULongValue,UShortValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert maximum boundary values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("IntValue", out intValue), "IntValue property is present");
            Assert.Equal(int.MaxValue, intValue.GetInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("LongValue", out longValue), "LongValue property is present");
            Assert.Equal(long.MaxValue, longValue.GetInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ShortValue", out shortValue), "ShortValue property is present");
            Assert.Equal(short.MaxValue, shortValue.GetInt16());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ByteValue", out byteValue), "ByteValue property is present");
            Assert.Equal(byte.MaxValue, byteValue.GetByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("SByteValue", out sbyteValue), "SByteValue property is present");
            Assert.Equal(sbyte.MaxValue, sbyteValue.GetSByte());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UIntValue", out uintValue), "UIntValue property is present");
            Assert.Equal(uint.MaxValue, uintValue.GetUInt32());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("ULongValue", out ulongValue), "ULongValue property is present");
            Assert.Equal(ulong.MaxValue, ulongValue.GetUInt64());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("UShortValue", out ushortValue), "UShortValue property is present");
            Assert.Equal(ushort.MaxValue, ushortValue.GetUInt16());
        }

        [Fact]
        public async Task PrimitiveTypes_SpecialFloatingPointValues_SerializesCorrectly()
        {
            // Create test model with special floating point values
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Special floating point values
                FloatValue = float.NaN,
                DoubleValue = double.PositiveInfinity,
                NullableDoubleValue = double.NegativeInfinity
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
            
            // Test special floating point values
            var response = await client.GetAsync("/test?include=[FloatValue,DoubleValue,NullableDoubleValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert special floating point values
            // Note: System.Text.Json serializes special values as strings
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("FloatValue", out var floatValue), "FloatValue property is present");
            Assert.Equal("NaN", floatValue.GetRawText().Trim('"'));
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DoubleValue", out var doubleValue), "DoubleValue property is present");
            Assert.Equal("Infinity", doubleValue.GetRawText().Trim('"'));
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("NullableDoubleValue", out var nullableDoubleValue), "NullableDoubleValue property is present");
            Assert.Equal("-Infinity", nullableDoubleValue.GetRawText().Trim('"'));
            
            // Test epsilon values
            testModel.FloatValue = float.Epsilon;
            testModel.DoubleValue = double.Epsilon;
            
            response = await client.GetAsync("/test?include=[FloatValue,DoubleValue]");
            response.EnsureSuccessStatusCode();
            
            json = await response.Content.ReadAsStringAsync();
            result = JsonDocument.Parse(json);
            
            // Assert epsilon values
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("FloatValue", out floatValue), "FloatValue property is present");
            Assert.Equal(float.Epsilon, floatValue.GetSingle());
            
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("DoubleValue", out doubleValue), "DoubleValue property is present");
            Assert.Equal(double.Epsilon, doubleValue.GetDouble());
        }

        [Fact]
        public async Task PrimitiveTypes_ExtendedUnicodeCharacters_SerializesCorrectly()
        {
            // Create test model with extended Unicode characters
            var testModel = new PrimitiveTypesTestModel 
            { 
                // Extended Unicode characters
                StringValue = "🚀 Emoji: 😀😎🤔👍\n" +
                              "Mathematical symbols: ∑∫∂√∞≠≈\n" +
                              "Currency symbols: $€£¥₹₽₿\n" +
                              "Arrows: ←↑→↓↔↕↖↗↘↙\n" +
                              "Technical symbols: ⌘⌥⇧⌃⌫⎋⏏\n" +
                              "Musical symbols: ♩♪♫♬\n" +
                              "Chess pieces: ♔♕♖♗♘♙♚♛♜♝♞♟\n" +
                              "Box drawing: ┌─┐│└┘├┤┬┴┼\n" +
                              "Superscript/subscript: x²y³z₁₂₃\n" +
                              "Fractions: ½⅓¼⅕⅙⅐⅛⅑⅒\n" +
                              "Ancient scripts: 𓀀𓀁𓀂𓀃 (Egyptian), 𐎠𐎡𐎢𐎣 (Ugaritic)"
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
            
            // Test extended Unicode characters
            var response = await client.GetAsync("/test?include=[StringValue]");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);
            
            // Assert extended Unicode characters
            Assert.True(result.RootElement.GetProperty("Data").TryGetProperty("StringValue", out var stringValue), "StringValue property is present");
            
            // The string should be preserved exactly as it was
            var expectedString = "🚀 Emoji: 😀😎🤔👍\n" +
                                "Mathematical symbols: ∑∫∂√∞≠≈\n" +
                                "Currency symbols: $€£¥₹₽₿\n" +
                                "Arrows: ←↑→↓↔↕↖↗↘↙\n" +
                                "Technical symbols: ⌘⌥⇧⌃⌫⎋⏏\n" +
                                "Musical symbols: ♩♪♫♬\n" +
                                "Chess pieces: ♔♕♖♗♘♙♚♛♜♝♞♟\n" +
                                "Box drawing: ┌─┐│└┘├┤┬┴┼\n" +
                                "Superscript/subscript: x²y³z₁₂₃\n" +
                                "Fractions: ½⅓¼⅕⅙⅐⅛⅑⅒\n" +
                                "Ancient scripts: 𓀀𓀁𓀂𓀃 (Egyptian), 𐎠𐎡𐎢𐎣 (Ugaritic)";
            
            Assert.Equal(expectedString, stringValue.GetString());
            
            // Test string length
            Assert.Equal(expectedString.Length, stringValue.GetString().Length);
        }
    }
}
