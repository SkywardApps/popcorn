#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Popcorn.Shared
{
    public static class HttpContextExtensions
    {
        public static global::Popcorn.Shared.ApiResponse<T> Respond<T>(this HttpContext context, T data)
        {
            // Build the property references
            var propertyReferences = PropertyReference.ParseIncludeStatement(context.Request.Query["include"]);
            return new global::Popcorn.Shared.ApiResponse<T>(propertyReferences, data);
        }
    }
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPopcorn(this IServiceCollection services)
        {
            services.AddScoped<IPopcornAccessor, PopcornAccessor>();
            return services;
        }
    }
}
