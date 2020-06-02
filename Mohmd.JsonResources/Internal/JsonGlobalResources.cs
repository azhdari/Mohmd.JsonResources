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
            GlobalName = _options.GlobalResourceFileName ?? "global";
            AreaName = _options.AreasResourcePrefix ?? "areas";
            DefaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));

            ResourceRelativePath = _options.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(ResourceRelativePath))
            {
                ResourceRelativePath = ResourceRelativePath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            }
        }

        #endregion

        #region Properties

        public RequestCulture DefaultCulture { get; private set; }
        public string ResourceRelativePath { get; private set; }
        public string GlobalName { get; private set; }
        public string AreaName { get; private set; }

        #endregion

        #region Methods

        public string[] GetGlobalFileLocations(CultureInfo culture)
        {
            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(DefaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, DefaultCulture.UICulture))
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

            if (LocalizerUtil.IsChildCulture(DefaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, DefaultCulture.UICulture))
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

        public JsonDocument GetGlobalResources(CultureInfo culture)
        {
            var cultureSuffix = "." + culture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(DefaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, DefaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var cacheName = "global";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            var lazyJObjectGetter = new Lazy<JsonDocument>(
                () =>
                {
                    string root = _app.ContentRootPath;

                    if (!string.IsNullOrEmpty(ResourceRelativePath))
                    {
                        root = Path.Combine(root, ResourceRelativePath.Trim('/', '\\'));
                    }

                    var resourceBaseName = GlobalName;
                    var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

                    string resourcePath = null;
                    foreach (var resourceFileLocation in resourceFileLocations)
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

            if (LocalizerUtil.IsChildCulture(DefaultCulture.UICulture, culture) || LocalizerUtil.IsChildCulture(culture, DefaultCulture.UICulture))
            {
                cultureSuffix = string.Empty;
            }

            var areaSuffix = $".{areaName}";

            var cacheName = $"{AreaName}{areaSuffix}";
            cacheName += string.IsNullOrEmpty(cultureSuffix) ? ".default" : cultureSuffix;

            var lazyJObjectGetter = new Lazy<JsonDocument>(
                () =>
                {
                    string root = _app.ContentRootPath;

                    if (!string.IsNullOrEmpty(ResourceRelativePath))
                    {
                        root = Path.Combine(root, ResourceRelativePath.Trim('/', '\\'));
                    }

                    var resourceBaseName = AreaName;
                    var resourceFileLocations = LocalizerUtil.ExpandPaths(resourceBaseName, _app.ApplicationName).ToList();

                    string resourcePath = null;
                    foreach (var resourceFileLocation in resourceFileLocations)
                    {
                        resourcePath = resourceFileLocation + areaSuffix + cultureSuffix + ".json";
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
