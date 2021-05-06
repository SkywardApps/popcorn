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
        }
    }
}