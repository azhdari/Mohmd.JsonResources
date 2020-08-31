using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using Mohmd.JsonResources.Internal.Embedded;
using Mohmd.JsonResources.Internal.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Providers
{
    public class EmbeddedProvider : IJsonResourceProvider
    {
        private readonly ILogger _logger;
        private readonly IHostingEnvironment _env;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly JsonLocalizationOptions _options;
        private readonly string _resourcesRelativePath;
        private readonly ConcurrentDictionary<string, Lazy<JsonDocument?>> _resourceObjectCache = new ConcurrentDictionary<string, Lazy<JsonDocument?>>();
        private readonly Lazy<EmbededResourceFile[]> _allEmbededResources;
        private readonly JsonGlobalResources _globalResources;

        private readonly string _resourceFileLocation;
        private readonly Lazy<AssemblyResources[]> _assemblyResources;

        public EmbeddedProvider(string resourceBaseName, ILoggerFactory loggerFactory, IHostingEnvironment env, IActionContextAccessor actionContextAccessor, JsonLocalizationOptions options)
        {
            _logger = loggerFactory.CreateLogger<EmbeddedProvider>();
            _env = env;
            _options = options;
            _actionContextAccessor = actionContextAccessor;
            _resourcesRelativePath = _options.ResourcesPath.Replace(Path.AltDirectorySeparatorChar, '.').Replace(Path.DirectorySeparatorChar, '.');
            _globalResources = new JsonGlobalResources(env, options, loggerFactory);

            _allEmbededResources = new Lazy<EmbededResourceFile[]>(AssemblyCollection.GetAllResourceFileContents);

            _resourceFileLocation = LocalizerUtil.ExpandPaths(resourceBaseName, _options.ResourcesPath).First();
            _assemblyResources = new Lazy<AssemblyResources[]>(() =>
            {
                if (!MemoryStorage.AssemblyResources.Any())
                {
                    EmbeddedResourcesHelper.GetResources(AssemblyCollection.Assemblies.ToArray())
                        .ToList()
                        .ForEach(MemoryStorage.AssemblyResources.Add);
                }

                return MemoryStorage.AssemblyResources.ToArray();
            });
        }

        public string? GetStringSafely(string name)
        {
            ResourceFileContent? resourceFileContent = GetResource(CultureInfo.CurrentUICulture, IsDefaultCulture(CultureInfo.CurrentUICulture));

            if (resourceFileContent == null)
            {
                return null;
            }

            name = name.Normalize().ToLowerInvariant();

            ResourceItem? item = resourceFileContent.FirstOrDefault(x => x.Key?.Normalize().ToLowerInvariant() == name);

            if (item == null)
            {
                return null;
            }

            return item.Value.Value;

            //var keyCulture = CultureInfo.CurrentUICulture;
            //var currentCulture = keyCulture;
            //CultureInfo? previousCulture = null;
            //do
            //{
            //    // first try resources per type
            //    var local = GetResourceObject(keyCulture);
            //    if (local != null && local.RootElement.TryGetProperty(name, out var token))
            //    {
            //        return token.GetString();
            //    }

            //    string? areaName = FindCurrentContextAreaName();

            //    List<ResourceCollection> resourceCollections = _globalResources.GetResources(keyCulture, areaName);
            //    List<KeyValuePair<string, string>> flatResources = resourceCollections.SelectMany(x => x.Resources)
            //                                                                          .ToList();

            //    // if not found, then try find the name in area resources (if available)
            //    // if not found, then try find the name in global resources
            //    if (flatResources.Any(x => x.Key?.Normalize().ToUpperInvariant() == name?.Normalize().ToUpperInvariant()))
            //    {
            //        return resourceCollections.SelectMany(x => x.Resources)
            //                                  .Where(x => x.Key?.Normalize().ToUpperInvariant() == name?.Normalize().ToUpperInvariant())
            //                                  .First().Value;
            //    }
            //    else
            //    {
            //        _logger.LogDebug_Localizer($"Resource key {name} not found in {flatResources.Count}.");
            //    }

            //    // Consult parent culture.
            //    previousCulture = currentCulture;
            //    currentCulture = currentCulture.Parent;
            //}
            //while (previousCulture != currentCulture);

            //// if we got here, so no resource found
            //return null;
        }
        public JsonDocument? GetResourceObject(CultureInfo currentCulture)
        {
            if (currentCulture == null)
            {
                throw new ArgumentNullException(nameof(currentCulture));
            }

            var cultureSuffix = "." + currentCulture.Name;
            cultureSuffix = cultureSuffix == "." ? string.Empty : cultureSuffix;

            if (LocalizerUtil.IsChildCulture(new CultureInfo(_options.DefaultUICultureName), currentCulture) || LocalizerUtil.IsChildCulture(currentCulture, new CultureInfo(_options.DefaultUICultureName)))
            {
                cultureSuffix = string.Empty;
            }

            var cacheKey = string.IsNullOrEmpty(cultureSuffix) ? "default" : cultureSuffix;

            var lazyJsonDocumentGetter = new Lazy<JsonDocument?>(
                () =>
                {
                    _logger.LogDebug_Localizer($"Resource file content not found in cache ({cacheKey}), try to load from file.");

                    // First attempt to find a resource file location that exists.
                    string? resourcePath = null;
                    resourcePath = _resourceFileLocation + cultureSuffix + ".json";

                    if (!_assemblyResources.Value.Any(x => x.CultureResources.Any(cr => cr.Resources.Any(r => r.Key == resourcePath))))
                    {
                        _logger.LogWarning_Localizer($"There is no resource file found for {cacheKey}");
                        return null;
                    }

                    // Found a resource file path: attempt to parse it into a JsonDocument.
                    try
                    {
                        var resourceItem = _allEmbededResources.Value.FirstOrDefault(x => x.Name == resourcePath);
                        var content = JsonDocument.Parse(resourceItem.Content);

                        _logger.LogInformation_Localizer($"Resource file content loaded: {resourcePath}");

                        return content;
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

        private ResourceFileContent? GetResource(CultureInfo cultureInfo, bool isDefaultCulture)
        {
            if (isDefaultCulture)
            {
                return MemoryStorage.Find(new ResourceFileKey(_resourceFileLocation, cultureInfo.Name), key =>
                {
                    return _assemblyResources.Value
                        .SelectMany(x => x.DefaultResources)
                        .ToList()
                        .ToResourceFileContent();
                });
            }
            else
            {
                return MemoryStorage.Find(new ResourceFileKey(_resourceFileLocation, cultureInfo.Name), key =>
                {
                    return _assemblyResources.Value
                        .SelectMany(x => x.CultureResources)
                        .Where(x => IsCultureInSameFamily(cultureInfo)(x.CultureInfo))
                        .SelectMany(x => x.Resources)
                        .Union(_assemblyResources.Value.SelectMany(x => x.DefaultResources))
                        .ToList()
                        .ToResourceFileContent();
                });
            }
        }

        private string? FindCurrentContextAreaName()
        {
            string? areaName = null;

            object? obj = default;
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

        private Func<CultureInfo, bool> IsCultureInSameFamily(CultureInfo cultureInfo)
            => (CultureInfo toCheck)
            => (LocalizerUtil.IsChildCulture(cultureInfo, toCheck) || LocalizerUtil.IsChildCulture(toCheck, cultureInfo));

        private bool IsDefaultCulture(CultureInfo cultureInfo) => IsCultureInSameFamily(new CultureInfo(_options.DefaultUICultureName))(cultureInfo);
    }
}
