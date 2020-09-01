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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Providers
{
    public class EmbeddedProvider : IJsonResourceProvider
    {
        private readonly IHostingEnvironment _env;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly JsonLocalizationOptions _options;

        private readonly string _resourceFileName;
        private readonly Lazy<AssemblyResources[]> _assemblyResources;

        public EmbeddedProvider(string resourceFileName, IHostingEnvironment env, IActionContextAccessor actionContextAccessor, JsonLocalizationOptions options)
        {
            _env = env;
            _actionContextAccessor = actionContextAccessor;
            _options = options;

            _resourceFileName = resourceFileName;

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
                return MemoryStorage.Find(new ResourceFileKey(_resourceFileName, cultureInfo.Name), key =>
                {
                    return _assemblyResources.Value
                        .SelectMany(x => x.DefaultResources)
                        .Select(file => (File: file, DisplayOrder: GetDisplayOrder(file, FindCurrentContextAreaName())))
                        .Where(item => item.DisplayOrder >= 0)
                        .OrderBy(item => item.DisplayOrder)
                        .Select(item => item.File)
                        .FirstOrDefault()
                        ?.Items
                        ?.ToResourceFileContent();
                });
            }
            else
            {
                return MemoryStorage.Find(new ResourceFileKey(_resourceFileName, cultureInfo.Name), key =>
                {
                    return _assemblyResources.Value
                        .SelectMany(x => x.CulturalFiles)
                        .Where(x => IsCultureInSameFamily(cultureInfo)(x.CultureInfo))
                        .SelectMany(x => x.Files)
                        .Select(file => (File: file, DisplayOrder: GetDisplayOrder(file, FindCurrentContextAreaName())))
                        .Where(item => item.DisplayOrder >= 0)
                        .OrderBy(item => item.DisplayOrder)
                        .Select(item => item.File)
                        .Break(list =>
                        {
                            var item = list.Where(x => x.Name == _resourceFileName).ToList();
                        })
                        .FirstOrDefault()
                        ?.Items
                        ?.ToResourceFileContent();
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

        private string GetResourcesBaseLocation()
        {
            string result = $"{_env.ApplicationName}.{_options.ResourcesPath.Replace("/", ".").Replace("\\", ".")}";
            return result;
        }

        private string GetFileName(EmbededResourceFile file)
        {
            string path = GetResourcesBaseLocation();
            string result = file.Name.Replace(path, string.Empty).Trim('.');
            return result;
        }

        private bool IsGlobal(EmbededResourceFile file) => GetFileName(file).Equals($"{_options.GlobalResourceFileName}.json", StringComparison.OrdinalIgnoreCase);

        private bool IsArea(EmbededResourceFile file, string areaName)
        {
            var match = Regex.Match(GetFileName(file), $"^({_options.AreasResourcePrefix})\\.([^\\.]+)\\.json$", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count == 3)
            {
                return match.Groups[2].Value.Equals(areaName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }

        private bool IsExact(EmbededResourceFile file) => file.Name.Equals($"{_resourceFileName}.json", StringComparison.OrdinalIgnoreCase);

        private int GetDisplayOrder(EmbededResourceFile file, string? areaName = null)
        {
            if (IsExact(file))
            {
                return 1;
            }
            else if (areaName != null && IsArea(file, areaName))
            {
                return 2;
            }
            else if (IsGlobal(file))
            {
                return 3;
            }
            else
            {
                return -1;
            }
        }
    }
}
