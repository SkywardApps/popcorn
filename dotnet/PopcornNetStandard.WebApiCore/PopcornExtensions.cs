using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Skyward.Popcorn
{
    /// <summary>
    /// Some useful extensions for Web App style configuration
    /// </summary>
    public static class PopcornExtensions
    {
        /// <summary>
        /// Configure the AspNet Core MVC options to include an Api Expander.  Allow the caller to configure it with an action.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        public static void UsePopcorn(this MvcOptions options, Action<PopcornConfiguration> configure = null)
        {
            // Create an expander object
            var expander = new Expander();
            var configuration = new PopcornConfiguration(expander);

            // optionally configure this expander
            if (configure != null)
            {
                configure(configuration);
            }

            expander.ServiceProvider = configuration.ServiceProvider;

            // Assign a global expander that'll run on all endpoints
            options.Filters.Add(new ExpandActionFilter(expander, configuration.Context, configuration.Inspector, configuration.ApplyToAllEndpoints, configuration.ServiceProvider));
        }


        public static void UsePopcornService(this IServiceCollection services, Action<PopcornConfiguration> configure = null)
        {
            var expander = new Expander();
            services.AddScoped<IPopcornContextAccessor, PopcornContextAccessor>();
            services.Configure<PopcornConfiguration>(configure ?? ((config) =>
            {
                config.SetDefaultApiResponseInspector();
                config.EnableBlindExpansion(true);
            }));
            services.AddSingleton<IExpanderService, ExpanderService>();
            //services.AddControllers(c => c.Filters.Add<ExpandServiceFilter>());
        }
    }
}