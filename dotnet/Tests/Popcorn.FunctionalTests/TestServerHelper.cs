using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
