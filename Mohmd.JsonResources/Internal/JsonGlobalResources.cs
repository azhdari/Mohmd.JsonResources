using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    internal class JsonGlobalResources
    {
        #region Fields

        private readonly ConcurrentDictionary<string, Lazy<JsonDocument?>> _resources = new ConcurrentDictionary<string, Lazy<JsonDocument?>>();
        private readonly IHostingEnvironment _app;
        private readonly JsonLocalizationOptions _options;
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        public JsonGlobalResources(IHostingEnvironment hostingEnvironment,
                                   JsonLocalizationOptions options,
                                   ILoggerFactory loggerFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _app = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            GlobalName = _options.GlobalResourceFileName ?? "global";
            AreaName = _options.AreasResourcePrefix ?? "areas";
            DefaultCulture = new CultureInfo(_options.DefaultUICultureName);
            _logger = loggerFactory.CreateLogger<JsonGlobalResources>();

            ResourceRelativePath = _options.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(ResourceRelativePath))
            {
                ResourceRelativePath = ResourceRelativePath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            }
        }

        #endregion

        #region Properties

        public CultureInfo DefaultCulture { get; private set; }
        public string ResourceRelativePath { get; private set; }
        public string GlobalName { get; private set; }
        public string AreaName { get; private set; }

        #endregion

        #region Methods

        public string[] GetGlobalFileLocations(CultureInfo culture)
        {
            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(new CultureInfo(_options.DefaultUICultureName), culture) || LocalizerUtil.IsChildCulture(culture, new CultureInfo(_options.DefaultUICultureName)))
            {
                cultureSuffix = string.Empty;
            }

            var cacheName = "global";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            string root = _app.ContentRootPath;

            if (!string.IsNullOrEmpty(ResourceRelativePath))
            {
                root = Path.Combine(root, ResourceRelativePath.Trim('/', '\\'));
            }

            var resourceBaseName = GlobalName;
            var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

            return resourceFileLocations.Select(resourceFileLocation => resourceFileLocation + cultureSuffix + ".json")
                                        .Select(resourcePath => Path.Combine(root, resourcePath))
                                        .ToArray();
        }

        public string[] GetAreaFileLocations(CultureInfo culture, string areaName)
        {
            if (string.IsNullOrEmpty(areaName?.Trim()))
            {
                throw new ArgumentNullException(nameof(areaName));
            }

            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(new CultureInfo(_options.DefaultUICultureName), culture) || LocalizerUtil.IsChildCulture(culture, new CultureInfo(_options.DefaultUICultureName)))
            {
                cultureSuffix = string.Empty;
            }

            var areaSuffix = $".{areaName}";

            var cacheName = $"{AreaName}{areaSuffix}";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            string root = _app.ContentRootPath;

            if (!string.IsNullOrEmpty(ResourceRelativePath))
            {
                root = Path.Combine(root, ResourceRelativePath.Trim('/', '\\'));
            }

            var resourceBaseName = AreaName;
            var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

            return resourceFileLocations.Select(resourceFileLocation => resourceFileLocation + areaSuffix + cultureSuffix + ".json")
                                        .Select(resourcePath => Path.Combine(root, resourcePath))
                                        .ToArray();
        }

        //

        public JsonDocument? GetGlobalResources(CultureInfo culture)
        {
            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(DefaultCulture, culture) || LocalizerUtil.IsChildCulture(culture, DefaultCulture))
            {
                cultureSuffix = string.Empty;
            }

            var cacheName = "global";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            var lazyJObjectGetter = new Lazy<JsonDocument?>(() =>
                {
                    _logger.LogDebug_Localizer($"Resource file content not found in cache ({cacheName}), try to load from file.");

                    string root = _app.ContentRootPath;

                    if (!string.IsNullOrEmpty(ResourceRelativePath))
                    {
                        root = Path.Combine(root, ResourceRelativePath.Trim('/', '\\'));
                    }

                    _logger.LogDebug_Localizer($"Looking for resource files in {root}");

                    var resourceBaseName = GlobalName;
                    var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

                    string? resourcePath = null;
                    foreach (var resourceFileLocation in resourceFileLocations)
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
                        _logger.LogWarning_Localizer($"There is no resource file found for {resourceBaseName}");
                        return null;
                    }

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

            _logger.LogInformation_Localizer($"Trying to load resource file content from cache {cacheName}.");

            lazyJObjectGetter = _resources.GetOrAdd(cacheName, lazyJObjectGetter);
            return lazyJObjectGetter.Value;
        }

        public JsonDocument? GetAreaResources(CultureInfo culture, string areaName)
        {
            if (string.IsNullOrEmpty(areaName?.Trim()))
            {
                throw new ArgumentNullException(nameof(areaName));
            }

            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(DefaultCulture, culture) || LocalizerUtil.IsChildCulture(culture, DefaultCulture))
            {
                cultureSuffix = string.Empty;
            }

            var areaSuffix = $".{areaName}";

            var cacheName = $"{AreaName}{areaSuffix}";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            var lazyJObjectGetter = new Lazy<JsonDocument?>(
                () =>
                {
                    _logger.LogDebug_Localizer($"Resource file content not found in cache ({cacheName}), try to load from file.");

                    string root = _app.ContentRootPath;

                    if (!string.IsNullOrEmpty(ResourceRelativePath))
                    {
                        root = Path.Combine(root, ResourceRelativePath.Trim('/', '\\'));
                    }

                    _logger.LogDebug_Localizer($"Looking for resource files in {root}");

                    var resourceBaseName = AreaName;
                    var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

                    string? resourcePath = null;
                    foreach (var resourceFileLocation in resourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + areaSuffix + cultureSuffix + ".json";
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
                        _logger.LogWarning_Localizer($"There is no resource file found for {resourceBaseName}");
                        return null;
                    }

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

            _logger.LogInformation_Localizer($"Trying to load resource file content from cache {cacheName}.");

            lazyJObjectGetter = _resources.GetOrAdd(cacheName, lazyJObjectGetter);
            return lazyJObjectGetter.Value;
        }

        public List<ResourceCollection> GetResources(CultureInfo culture, string? areaName = null)
        {
            List<ResourceCollection> resources = new List<ResourceCollection>();

            if (!string.IsNullOrWhiteSpace(areaName))
            {
                ResourceCollection? area = ConvertToResourceCollection(GetAreaResources(culture, areaName ?? string.Empty), culture.Name);
                if (area != null)
                {
                    resources.Add(area);
                }
            }

            ResourceCollection? global = ConvertToResourceCollection(GetGlobalResources(culture), culture.Name);
            if (global != null)
            {
                resources.Add(global);
            }

            return resources;
        }

        public void ClearCache()
        {
            _resources.Clear();
        }

        #endregion

        #region Utilities

        private ResourceCollection? ConvertToResourceCollection(JsonDocument? jsonObject, string locale)
        {
            if (jsonObject == null)
            {
                return null;
            }

            IDictionary<string, string> dic = new Dictionary<string, string>();

            jsonObject
                .RootElement
                .EnumerateObject()
                .ToList()
                .ForEach(item =>
                {
#if NETSTANDARD2_1
                    dic.TryAdd(item.Name, item.Value.GetString());
#else
                    if (!dic.ContainsKey(item.Name))
                    {
                        dic.Add(item.Name, item.Value.ToString());
                    }
#endif
                });

            _logger.LogInformation_Localizer($"{dic.Count} resource keys loaded for {locale}");

            return new ResourceCollection(locale, dic);
        }

        #endregion
    }
}
