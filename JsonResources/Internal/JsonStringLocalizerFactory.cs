using JsonResources.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JsonResources.Internal
{
    internal class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        #region Fields

        private static readonly string[] KnownViewExtensions = new[] { ".cshtml" };

        private readonly ConcurrentDictionary<string, JsonStringLocalizer> _localizerCache = new ConcurrentDictionary<string, JsonStringLocalizer>();
        private readonly IHostingEnvironment _app;
        private readonly string _resourcesRelativePath;
        private readonly JsonGlobalResources _globalResources;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly RequestCulture _defaultCulture;

        #endregion

        #region Constructors

        public JsonStringLocalizerFactory(IHostingEnvironment hostingEnvironment, IOptions<JsonLocalizationOptions> options, IOptions<RequestLocalizationOptions> requestLocalizationOptions, IActionContextAccessor actionContextAccessor)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _app = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _defaultCulture = requestLocalizationOptions.Value.DefaultRequestCulture;

            _resourcesRelativePath = options.Value?.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            }

            _globalResources = new JsonGlobalResources(hostingEnvironment, options, _defaultCulture);
        }

        #endregion

        #region Methods

        public IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;

            // Re-root the base name if a resources path is set.
            var resourceBaseName = string.IsNullOrEmpty(_resourcesRelativePath) ? typeInfo.FullName : _app.ApplicationName + "." + _resourcesRelativePath + "." + LocalizerUtil.TrimPrefix(typeInfo.FullName, _app.ApplicationName + ".");
            return _localizerCache.GetOrAdd(resourceBaseName, new JsonStringLocalizer(resourceBaseName, _app.ApplicationName, _globalResources, _actionContextAccessor, _defaultCulture, _app));
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            location = location ?? _app.ApplicationName;
            var resourceBaseName = location + "." + _resourcesRelativePath + "." + LocalizerUtil.TrimPrefix(baseName, location + ".");

            var viewExtension = KnownViewExtensions.FirstOrDefault(extension => resourceBaseName.EndsWith(extension));
            if (viewExtension != null)
            {
                resourceBaseName = resourceBaseName.Substring(0, resourceBaseName.Length - viewExtension.Length);
            }

            return _localizerCache.GetOrAdd(resourceBaseName, new JsonStringLocalizer(resourceBaseName, _app.ApplicationName, _globalResources, _actionContextAccessor, _defaultCulture, _app));
        }

        public void ClearCache()
        {
            _globalResources.ClearCache();
            _localizerCache.Clear();
        }

        #endregion
    }
}
