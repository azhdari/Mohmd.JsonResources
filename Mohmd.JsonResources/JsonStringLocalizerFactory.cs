using Microsoft.Extensions.Localization;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using Mohmd.JsonResources.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mohmd.JsonResources
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private static readonly string[] _knownViewExtensions = new[] { ".cshtml" };
        private readonly ConcurrentDictionary<string, IStringLocalizer> _localizerCache = new ConcurrentDictionary<string, IStringLocalizer>();
        private readonly IJsonResourceProviderFactory _resourceProviderFactory;

        #region Ctors

        public JsonStringLocalizerFactory(IJsonResourceProviderFactory resourceProviderFactory)
        {
            _resourceProviderFactory = resourceProviderFactory;
        }

        #endregion

        #region Methods

        public IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource is null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            var typeInfo = resourceSource.GetTypeInfo();

            var resourceBaseName = typeInfo.FullName;

            Type localizerType = typeof(JsonStringLocalizer<>).MakeGenericType(resourceSource.GetTypeInfo());

            IStringLocalizer GetLocalizer(string str)
            {
                object localizer = Activator.CreateInstance(localizerType, resourceBaseName, JsonLocalizationOptions.Current, _resourceProviderFactory);
                if (localizer != null && localizer is IStringLocalizer stringLocalizer)
                {
                    return stringLocalizer;
                }
                else
                {
                    return Create(resourceSource.Name, string.Empty);
                }
            };

            return _localizerCache.GetOrAdd(resourceBaseName, GetLocalizer);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            location ??= string.Empty;
            var resourceBaseName = location + "." + LocalizerUtil.TrimPrefix(baseName, location + ".");
            resourceBaseName = resourceBaseName.TrimStart('.');

            var viewExtension = _knownViewExtensions.FirstOrDefault(extension => resourceBaseName.EndsWith(extension));
            if (viewExtension != null)
            {
                resourceBaseName = resourceBaseName.Substring(0, resourceBaseName.Length - viewExtension.Length);
            }

            return _localizerCache.GetOrAdd(
                resourceBaseName,
                new JsonStringLocalizer(resourceBaseName, JsonLocalizationOptions.Current, _resourceProviderFactory));
        }

        #endregion
    }
}
