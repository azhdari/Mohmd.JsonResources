using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mohmd.JsonResources;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using Mohmd.JsonResources.Providers;
using System;
using JsonStringLocalizerFactory = Mohmd.JsonResources.JsonStringLocalizerFactory;

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

        public static IServiceCollection AddJsonLocalization(this IServiceCollection services, Action<JsonLocalizationOptions>? setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            services.AddSingleton<IJsonResourceProviderFactory, JsonResourceProviderFactory>();

            services.Configure<JsonLocalizationOptions>(s =>
            {
                setupAction?.Invoke(s);
                JsonLocalizationOptions.Current = s;
            });
            services.AddScoped(serviceProvider => serviceProvider.GetService<IOptionsSnapshot<JsonLocalizationOptions>>().Value);

            return services;
        }
    }
}
