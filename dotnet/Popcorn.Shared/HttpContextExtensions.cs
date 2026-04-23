#nullable enable
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            => AddPopcorn(services, configure: null);

        /// <summary>
        /// Registers Popcorn services and (optionally) configures <see cref="PopcornOptions"/>.
        /// Idempotent: repeated calls mutate the single options singleton rather than registering
        /// duplicates, so <c>AddPopcorn()</c> + <c>AddPopcorn(o =&gt; ...)</c> in that order works.
        /// </summary>
        public static IServiceCollection AddPopcorn(this IServiceCollection services, System.Action<PopcornOptions>? configure)
        {
            // Reuse an existing singleton if one is already registered so repeated calls don't
            // accumulate dead instances and reconfiguration is visible to every resolver.
            var existing = services
                .LastOrDefault(d => d.ServiceType == typeof(PopcornOptions))
                ?.ImplementationInstance as PopcornOptions;

            var options = existing ?? new PopcornOptions();
            configure?.Invoke(options);

            if (existing == null)
            {
                services.AddSingleton(options);
            }

            services.TryAddScoped<IPopcornAccessor, PopcornAccessor>();
            return services;
        }
    }
}
