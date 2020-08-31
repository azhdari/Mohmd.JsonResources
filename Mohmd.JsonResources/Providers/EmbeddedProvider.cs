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
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly JsonLocalizationOptions _options;

        private readonly string _resourceFileLocation;
        private readonly Lazy<AssemblyResources[]> _assemblyResources;

        public EmbeddedProvider(string resourceBaseName, IActionContextAccessor actionContextAccessor, JsonLocalizationOptions options)
        {
            _options = options;
            _actionContextAccessor = actionContextAccessor;

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
