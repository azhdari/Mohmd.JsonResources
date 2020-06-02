using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        public JsonStringLocalizer(
            string resourceBaseName,
            IHostingEnvironment env,
            JsonGlobalResources globalResources,
            RequestCulture defaultCulture,
            IActionContextAccessor actionContextAccessor,
            JsonLocalizationOptions options,
            ILoggerFactory loggerFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            GlobalResources = globalResources ?? throw new ArgumentNullException(nameof(globalResources));
            DefaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _logger = loggerFactory.CreateLogger<JsonStringLocalizer<T>>();

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
                List<KeyValuePair<string, string>> flatResources = resourceCollections.SelectMany(x => x.Resources)
                                                                                      .ToList();

                // if not found, then try find the name in area resources (if available)
                // if not found, then try find the name in global resources
                if (flatResources.Any(x => x.Key?.Normalize().ToUpperInvariant() == name?.Normalize().ToUpperInvariant()))
                {
                    return resourceCollections.SelectMany(x => x.Resources)
                                              .Where(x => x.Key?.Normalize().ToUpperInvariant() == name?.Normalize().ToUpperInvariant())
                                              .First().Value;
                }
                else
                {
                    _logger.LogDebug_Localizer($"Resource key {name} not found in {flatResources.Count}.");
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

            var cacheKey = string.IsNullOrEmpty(cultureSuffix) ? "default" : cultureSuffix;

            var lazyJsonDocumentGetter = new Lazy<JsonDocument>(
                () =>
                {
                    _logger.LogDebug_Localizer($"Resource file content not found in cache ({cacheKey}), try to load from file.");

                    string root = _env.ContentRootPath;

                    if (!string.IsNullOrEmpty(_resourcesRelativePath))
                    {
                        root = Path.Combine(root, _resourcesRelativePath.Trim('/', '\\'));
                    }

                    _logger.LogDebug_Localizer($"Looking for resource files in {root}");

                    // First attempt to find a resource file location that exists.
                    string resourcePath = null;
                    foreach (var resourceFileLocation in ResourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + cultureSuffix + ".json";
                        resourcePath = Path.Combine(root, resourcePath);

                        if (File.Exists(resourcePath))
                        {
                            _logger.LogDebug_Localizer($"Resource file found: {resourcePath}");
                            break;
                        }
                        else
                        {
                            _logger.LogDebug_Localizer($"Resource file not found: {resourcePath}");
                            resourcePath = null;
                        }
                    }

                    if (resourcePath == null)
                    {
                        _logger.LogWarning_Localizer($"There is no resource file found for {cacheKey}");
                        return null;
                    }

                    // Found a resource file path: attempt to parse it into a JsonDocument.
                    try
                    {
                        var resourceFileStream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        using (resourceFileStream)
                        {
                            var content = JsonDocument.Parse(resourceFileStream);

                            _logger.LogInformation_Localizer($"Resource file content loaded: {resourcePath}");

                            return content;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError_Localizer(ex, $"Error while loading resource file: {ex.Message} ({resourcePath})");
                        return null;
                    }
                }, LazyThreadSafetyMode.ExecutionAndPublication);

            _logger.LogInformation_Localizer($"Trying to load resource file content from cache {cacheKey}.");

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
