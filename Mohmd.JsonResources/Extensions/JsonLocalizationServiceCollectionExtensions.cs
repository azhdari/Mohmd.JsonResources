using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JsonLocalizationServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddJsonLocalization(services, null);
        }

        public static IServiceCollection AddJsonLocalization(this IServiceCollection services, Action<JsonLocalizationOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAdd(new ServiceDescriptor(typeof(IActionContextAccessor), typeof(ActionContextAccessor), ServiceLifetime.Singleton));
            services.TryAdd(new ServiceDescriptor(typeof(IStringLocalizerFactory), typeof(JsonStringLocalizerFactory), ServiceLifetime.Singleton));
            services.TryAdd(new ServiceDescriptor(typeof(IStringLocalizer), typeof(JsonStringLocalizer), ServiceLifetime.Singleton));

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }
    }
}
