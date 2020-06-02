using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Mohmd.JsonResources.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Mohmd.JsonResources.Internal
{
    internal class JsonStringLocalizer<T> : IStringLocalizer<T>
    {
        #region Fields

        private readonly ConcurrentDictionary<string, Lazy<JsonDocument>> _resourceObjectCache = new ConcurrentDictionary<string, Lazy<JsonDocument>>();
        private readonly IHostingEnvironment _env;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly JsonLocalizationOptions _options;
        private readonly string _resourcesRelativePath;

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
            GlobalResources = globalResources ?? throw new ArgumentNullException(nameof(globalResources));
            DefaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));

            _resourcesRelativePath = _options.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            }

            ResourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName).ToList();
        }

        #endregion

        #region Indexers

        public LocalizedString this[string name]
        {
            get
            {
                _ = GlobalFileLocations;

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var value = GetStringSafely(name);
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

                var format = GetStringSafely(name);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        #endregion

        #region Properties

        public RequestCulture DefaultCulture { get; private set; }
        public JsonGlobalResources GlobalResources { get; private set; }
        public IEnumerable<string> ResourceFileLocations { get; private set; }

        public string[] GlobalFileLocations
        {
            get
            {
                var culture = CultureInfo.CurrentUICulture;
                return GlobalResources.GetGlobalFileLocations(culture);
            }
        }

        public string[] AreaFileLocations
        {
            get
            {
                var culture = CultureInfo.CurrentUICulture;
                string areaName = FindCurrentContextAreaName();

                if (!string.IsNullOrEmpty(areaName))
                {
                    return GlobalResources.GetAreaFileLocations(culture, areaName);
                }
                else
                {
                    return new string[0];
                }
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

        private string GetStringSafely(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var keyCulture = CultureInfo.CurrentUICulture;
            var currentCulture = keyCulture;
            CultureInfo previousCulture = null;
            do
            {
                // first try resources per type
                var local = GetResourceObject(keyCulture);
                if (local != null && local.RootElement.TryGetProperty(name, out var token))
                {
                    return token.GetString();
                }

                string areaName = FindCurrentContextAreaName();

                List<ResourceCollection> resourceCollections = GlobalResources.GetResources(keyCulture, areaName);
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

        private JsonDocument GetResourceObject(CultureInfo currentCulture)
        {
            if (currentCulture == null)
            {
                throw new ArgumentNullException(nameof(currentCulture));
            }

            var cultureSuffix = "." + currentCulture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(DefaultCulture.UICulture, currentCulture) || LocalizerUtil.IsChildCulture(currentCulture, DefaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var lazyJsonDocumentGetter = new Lazy<JsonDocument>(
                () =>
                {
                    string root = _env.ContentRootPath;

                    if (!string.IsNullOrEmpty(_resourcesRelativePath))
                    {
                        root = Path.Combine(root, _resourcesRelativePath.Trim('/', '\\'));
                    }

                    // First attempt to find a resource file location that exists.
                    string resourcePath = null;
                    foreach (var resourceFileLocation in ResourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + cultureSuffix + ".json";
                        resourcePath = Path.Combine(root, resourcePath);

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

                    // Found a resource file path: attempt to parse it into a JsonDocument.
                    try
                    {
                        var resourceFileStream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        using (resourceFileStream)
                        {
                            return JsonDocument.Parse(resourceFileStream);
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }, LazyThreadSafetyMode.ExecutionAndPublication);

            var cacheKey = string.IsNullOrEmpty(cultureSuffix) ? "default" : cultureSuffix;
            lazyJsonDocumentGetter = _resourceObjectCache.GetOrAdd(cacheKey, lazyJsonDocumentGetter);
            var resourceObject = lazyJsonDocumentGetter.Value;
            return resourceObject;
        }

        private string FindCurrentContextAreaName()
        {
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

            return areaName;
        }

        #endregion
    }
}
