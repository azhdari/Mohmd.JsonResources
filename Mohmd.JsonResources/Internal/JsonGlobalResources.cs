using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
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

        private readonly ConcurrentDictionary<string, Lazy<JsonDocument>> _resources = new ConcurrentDictionary<string, Lazy<JsonDocument>>();
        private readonly IHostingEnvironment _app;
        private readonly string _resourcesRelativePath;
        private readonly string _globalName;
        private readonly string _areaName;
        private readonly RequestCulture _defaultCulture;
        private readonly JsonLocalizationOptions _options;

        #endregion

        #region Constructors

        public JsonGlobalResources(IHostingEnvironment hostingEnvironment, IOptions<JsonLocalizationOptions> options, RequestCulture defaultCulture)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
            _app = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _globalName = _options.GlobalResourceFileName ?? "global";
            _areaName = _options.AreasResourcePrefix ?? "areas";
            _defaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));

            _resourcesRelativePath = _options.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            }
        }

        #endregion

        #region Methods

        public string[] GetGlobalFileLocations(CultureInfo culture)
        {
            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(_defaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, _defaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var resourceBaseName = string.IsNullOrEmpty(_resourcesRelativePath) ? _app.ApplicationName : _app.ApplicationName + "." + _resourcesRelativePath + "." + _globalName;
            var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

            resourceFileLocations = resourceFileLocations.Select(str => str + cultureSuffix + ".json").ToList();

            return resourceFileLocations.ToArray();
        }

        public string[] GetAreaFileLocations(CultureInfo culture, string areaName)
        {
            if (string.IsNullOrEmpty(areaName?.Trim()))
            {
                throw new ArgumentNullException(nameof(areaName));
            }

            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(_defaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, _defaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var areaSuffix = $".{areaName}";

            var resourceBaseName = string.IsNullOrEmpty(_resourcesRelativePath) ? _app.ApplicationName : _app.ApplicationName + "." + _resourcesRelativePath + "." + _areaName;
            var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

            resourceFileLocations = resourceFileLocations.Select(str => str + areaSuffix + cultureSuffix + ".json").ToList();

            return resourceFileLocations.ToArray();
        }

        public JsonDocument GetGlobalResources(CultureInfo culture)
        {
            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(_defaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, _defaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var cacheName = "global";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            var lazyJObjectGetter = new Lazy<JsonDocument>(
                () =>
                {
                    var resourceBaseName = string.IsNullOrEmpty(_resourcesRelativePath) ? _app.ApplicationName : _app.ApplicationName + "." + _resourcesRelativePath + "." + _globalName;
                    var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

                    string resourcePath = null;
                    foreach (var resourceFileLocation in resourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + cultureSuffix + ".json";
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

            lazyJObjectGetter = _resources.GetOrAdd(cacheName, lazyJObjectGetter);
            return lazyJObjectGetter.Value;
        }

        public JsonDocument GetAreaResources(CultureInfo culture, string areaName)
        {
            if (string.IsNullOrEmpty(areaName?.Trim()))
            {
                throw new ArgumentNullException(nameof(areaName));
            }

            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(_defaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, _defaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var areaSuffix = $".{areaName}";

            var cacheName = $"{_areaName}{areaSuffix}";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            var lazyJObjectGetter = new Lazy<JsonDocument>(
                () =>
                {
                    var resourceBaseName = string.IsNullOrEmpty(_resourcesRelativePath) ? _app.ApplicationName : _app.ApplicationName + "." + _resourcesRelativePath + "." + _areaName;
                    var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

                    string resourcePath = null;
                    foreach (var resourceFileLocation in resourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + areaSuffix + cultureSuffix + ".json";
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

            lazyJObjectGetter = _resources.GetOrAdd(cacheName, lazyJObjectGetter);
            return lazyJObjectGetter.Value;
        }

        public List<ResourceCollection> GetResources(CultureInfo culture, string areaName = null)
        {
            List<ResourceCollection> resources = new List<ResourceCollection>();

            if (!string.IsNullOrWhiteSpace(areaName))
            {
                ResourceCollection area = ConvertToResourceCollection(GetAreaResources(culture, areaName), culture.Name);
                if (area != null)
                {
                    resources.Add(area);
                }
            }

            ResourceCollection global = ConvertToResourceCollection(GetGlobalResources(culture), culture.Name);
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

        private ResourceCollection ConvertToResourceCollection(JsonDocument jsonObject, string locale)
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

            return new ResourceCollection
            {
                Locale = locale,
                Resources = dic,
            };
        }

#endregion
    }
}
