using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Popcorn.Shared;

namespace Popcorn.FunctionalTests
{
    internal static class TestServerHelper
    {
        public static TestServer CreateServer<T>(T model, string route = "/test")
            => CreateServer(model, configureOptions: null, route);

        public static TestServer CreateServer<T>(T model, Action<JsonSerializerOptions>? configureOptions, string route = "/test")
        {
            return new TestServer(new WebHostBuilder()
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
                        endpoints.MapGet(route, async context =>
                        {
                            var response = context.Respond(model);
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
                            options.AddPopcornOptions();
                            configureOptions?.Invoke(options);
                            await JsonSerializer.SerializeAsync(context.Response.Body, response, options);
                        });
                    });
                }));
        }

        public static TestServer CreateServerWithThrowingHandler(
            Action<HttpContext>? beforeThrow = null,
            Action<PopcornOptions>? configurePopcorn = null,
            Exception? toThrow = null,
            string route = "/test")
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn(configurePopcorn);
                    // Registers the generator-emitted custom error-envelope writer at DI time,
                    // so the exception middleware finds it even when the endpoint never reaches serialization.
                    services.AddPopcornEnvelopes();
                })
                .Configure(app =>
                {
                    app.UsePopcornExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(route, context =>
                        {
                            beforeThrow?.Invoke(context);
                            throw toThrow ?? new InvalidOperationException("boom");
                        });
                    });
                }));
        }

        public static TestServer CreateServerWithCustomEnvelope<T>(T payloadData, Func<Pop<T>, object> envelopeBuilder, string route = "/test")
        {
            return new TestServer(new WebHostBuilder()
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
                        endpoints.MapGet(route, async context =>
                        {
                            var accessor = context.RequestServices.GetRequiredService<IPopcornAccessor>();
                            var pop = new Pop<T> { Data = payloadData, PropertyReferences = accessor.PropertyReferences };
                            var envelope = envelopeBuilder(pop);
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(context.Response.Body, envelope, envelope.GetType(), options);
                        });
                    });
                }));
        }

        public static TestServer CreateServerWithWritePathException<T>(T model, string route = "/test")
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddRouting();
                    services.AddPopcorn();
                })
                .Configure(app =>
                {
                    app.UsePopcornExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(route, async context =>
                        {
                            var response = context.Respond(model);
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
                            options.AddPopcornOptions();
                            await JsonSerializer.SerializeAsync(context.Response.Body, response, options);
                        });
                    });
                }));
        }

        public static async Task<JsonDocument> GetJsonAsync(HttpClient client, string relativeUrl)
        {
            var response = await client.GetAsync(relativeUrl);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body);
        }

        public static JsonElement GetData(this JsonDocument doc) =>
            doc.RootElement.GetProperty("Data");

        public static bool HasProperty(this JsonElement element, string name) =>
            element.TryGetProperty(name, out _);
    }
}
