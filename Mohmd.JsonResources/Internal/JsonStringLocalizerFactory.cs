using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mohmd.JsonResources
{
    internal class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        #region Fields

        private static readonly string[] KnownViewExtensions = new[] { ".cshtml" };

        private readonly ConcurrentDictionary<string, IStringLocalizer> _localizerCache = new ConcurrentDictionary<string, IStringLocalizer>();
        private readonly IHostingEnvironment _env;
        private readonly string _resourcesRelativePath;
        private readonly JsonGlobalResources _globalResources;
        private readonly RequestCulture _defaultCulture;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly JsonLocalizationOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        #endregion

        #region Constructors

        public JsonStringLocalizerFactory(
            IHostingEnvironment hostingEnvironment,
            IOptions<JsonLocalizationOptions> options,
            IOptions<RequestLocalizationOptions> requestLocalizationOptions,
            IActionContextAccessor actionContextAccessor,
            ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _env = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _defaultCulture = requestLocalizationOptions.Value.DefaultRequestCulture;
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _loggerFactory = loggerFactory;

            _resourcesRelativePath = _options.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            }

            _globalResources = new JsonGlobalResources(hostingEnvironment, options, _defaultCulture, _loggerFactory);
        }

        #endregion

        #region Methods

        public virtual IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            var typeInfo = resourceSource.GetTypeInfo();

            var resourceBaseName = typeInfo.FullName;

            Type localizerType = typeof(JsonStringLocalizer<>).MakeGenericType(resourceSource);

            return _localizerCache.GetOrAdd(resourceBaseName, str => Activator.CreateInstance(localizerType, resourceBaseName, _env, _globalResources, _defaultCulture, _actionContextAccessor, _options, _loggerFactory) as IStringLocalizer);
        }

        public virtual IStringLocalizer Create(string baseName, string location)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            location ??= string.Empty;
            var resourceBaseName = location + "." + LocalizerUtil.TrimPrefix(baseName, location + ".");
            resourceBaseName = resourceBaseName.TrimStart('.');

            var viewExtension = KnownViewExtensions.FirstOrDefault(extension => resourceBaseName.EndsWith(extension));
            if (viewExtension != null)
            {
                resourceBaseName = resourceBaseName.Substring(0, resourceBaseName.Length - viewExtension.Length);
            }

            return _localizerCache.GetOrAdd(resourceBaseName, new JsonStringLocalizer(resourceBaseName, _env, _globalResources, _defaultCulture, _actionContextAccessor, _options, _loggerFactory));
        }

        public virtual void ClearCache()
        {
            _globalResources.ClearCache();
            _localizerCache.Clear();
        }

        #endregion
    }
}
