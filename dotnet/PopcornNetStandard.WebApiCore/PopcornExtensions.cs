using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Skyward.Popcorn.Abstractions;

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
        public static void UsePopcorn(this IServiceCollection services, Action<PopcornFactory> configure = null)
        {
            var factory = new PopcornFactory();
            services.AddScoped<IPopcornContextAccessor, PopcornContextAccessor>();
            configure(factory);
            services.AddSingleton<PopcornFactory>(factory);
            services.AddScoped(svs =>
            {
                return svs.GetRequiredService<PopcornFactory>().CreatePopcorn();
            });
        }
    }
}