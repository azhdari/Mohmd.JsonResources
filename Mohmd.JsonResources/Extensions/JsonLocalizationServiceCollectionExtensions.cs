using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using Mohmd.JsonResources.Providers;
using System;
using System.Reflection;
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

            JsonLocalizationOptions options = new JsonLocalizationOptions();
            setupAction?.Invoke(options);
            JsonLocalizationOptions.SetCurrentJsonLocalizationOptions(options);

            services.Configure<JsonLocalizationOptions>(s => { });
            services.AddScoped(serviceProvider => serviceProvider.GetService<IOptionsSnapshot<JsonLocalizationOptions>>().Value);

            return services;
        }

        public static IServiceCollection AddJsonLocalizationAssembly<T>(this IServiceCollection services)
        {
            Type typeToFindAssembly = typeof(T);
            Assembly assembly = typeToFindAssembly.Assembly;
            return AddJsonLocalizationAssembly(services, assembly);
        }

        public static IServiceCollection AddJsonLocalizationAssembly(this IServiceCollection services, Type typeToFindAssembly)
        {
            if (typeToFindAssembly is null)
            {
                throw new ArgumentNullException(nameof(typeToFindAssembly));
            }

            Assembly assembly = typeToFindAssembly.Assembly;
            return AddJsonLocalizationAssembly(services, assembly);
        }

        public static IServiceCollection AddJsonLocalizationAssembly(this IServiceCollection services, Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            AssemblyCollection.Assemblies.Add(assembly);
            return services;
        }
    }
}
