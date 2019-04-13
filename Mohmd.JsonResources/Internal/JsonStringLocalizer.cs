using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Mohmd.JsonResources.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mohmd.JsonResources.Internal
{
    internal class JsonStringLocalizer : IStringLocalizer
    {
        #region Fields

        private readonly ConcurrentDictionary<string, Lazy<JObject>> _resourceObjectCache = new ConcurrentDictionary<string, Lazy<JObject>>();
        private readonly IEnumerable<string> _resourceFileLocations;
        private readonly JsonGlobalResources _globalResources;
        private readonly RequestCulture _defaultCulture;
        private readonly IHostingEnvironment _env;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly JsonLocalizationOptions _options;

        #endregion

        #region Constructors

        public JsonStringLocalizer(
            string resourceBaseName,
            IHostingEnvironment env,
            JsonGlobalResources globalResources,
            RequestCulture defaultCulture,
            IActionContextAccessor actionContextAccessor,
            JsonLocalizationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _globalResources = globalResources ?? throw new ArgumentNullException(nameof(globalResources));
            _defaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));

            _resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _env.ApplicationName).ToList();
        }

        #endregion

        #region Indexers

        public LocalizedString this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var value = GetStringSafely(name, null);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var format = GetStringSafely(name, null);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        #endregion

        #region Methods

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Utilites

        private string GetStringSafely(string name, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var keyCulture = culture ?? CultureInfo.CurrentUICulture;
            var currentCulture = keyCulture;
            CultureInfo previousCulture = null;
            do
            {
                // first try resources per type
                JToken token = null;
                var local = GetResourceObject(keyCulture);
                if (local?.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out token) == true)
                {
                    var localized = token.ToString();
                    return localized;
                }

                string areaName = null;

                object obj = default;
                if (_actionContextAccessor.ActionContext?.RouteData.Values.TryGetValue("area", out obj) == true)
                {
                    areaName = obj?.ToString();
                    if (string.IsNullOrEmpty(areaName?.ToString()))
                    {
                        areaName = null;
                    }
                }

                List<ResourceCollection> resourceCollections = _globalResources.GetResources(keyCulture, areaName);
                List<KeyValuePair<string, string>> flatResources = resourceCollections
                    .SelectMany(x => x.Resources)
                    .ToList();

                // if not found, then try find the name in area resources (if available)
                // if not found, then try find the name in global resources
                if (flatResources.Any(x => x.Key?.Normalize().ToUpperInvariant() == name?.Normalize().ToUpperInvariant()))
                {
                    return resourceCollections
                        .SelectMany(x => x.Resources)
                        .Where(x => x.Key?.Normalize().ToUpperInvariant() == name?.Normalize().ToUpperInvariant())
                        .First()
                        .Value;
                }

                // Consult parent culture.
                previousCulture = currentCulture;
                currentCulture = currentCulture.Parent;
            }
            while (previousCulture != currentCulture);

            // if we got here, so no resource found
            return null;
        }

        private JObject GetResourceObject(CultureInfo currentCulture)
        {
            if (currentCulture == null)
            {
                throw new ArgumentNullException(nameof(currentCulture));
            }

            var cultureSuffix = "." + currentCulture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(_defaultCulture.UICulture, currentCulture) || LocalizerUtil.IsChildCulture(currentCulture, _defaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var lazyJObjectGetter = new Lazy<JObject>(
                () =>
                {
                    // First attempt to find a resource file location that exists.
                    string resourcePath = null;
                    foreach (var resourceFileLocation in _resourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + cultureSuffix + ".json";
                        resourcePath = Path.Combine(_env.ContentRootPath, resourcePath);
                        if (File.Exists(resourcePath))
                        {
                            break;
                        }
                        else
                        {
                            resourcePath = null;
                        }
                    }

                    if (resourcePath == null)
                    {
                        return null;
                    }

                    // Found a resource file path: attempt to parse it into a JObject.
                    try
                    {
                        var resourceFileStream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        using (resourceFileStream)
                        {
                            var resourceReader = new JsonTextReader(new StreamReader(resourceFileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true));
                            using (resourceReader)
                            {
                                return JObject.Load(resourceReader);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }, LazyThreadSafetyMode.ExecutionAndPublication);

            var cacheKey = string.IsNullOrEmpty(cultureSuffix) ? "default" : cultureSuffix;
            lazyJObjectGetter = _resourceObjectCache.GetOrAdd(cacheKey, lazyJObjectGetter);
            var resourceObject = lazyJObjectGetter.Value;
            return resourceObject;
        }

        #endregion
    }
}
