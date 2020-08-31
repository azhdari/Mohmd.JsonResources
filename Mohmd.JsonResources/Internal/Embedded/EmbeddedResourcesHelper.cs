using Mohmd.JsonResources.Internal.Types;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Mohmd.JsonResources.Internal.Embedded
{
    public class EmbeddedResourcesHelper
    {
        public static AssemblyResources[] GetResources(params Assembly[] assemblies)
        {
            return GetAssemblyCultures(assemblies)
                .Select(item =>
                {
                    return new AssemblyResources
                    {
                        MainAssembly = item.MainAssembly,
                        DefaultResources = GetResourceFileContents(item.MainAssembly),
                        CultureResources = item.Resources.Select(x => (cultureInfo: x.ci, resources: GetResourceFileContents(x.ass))).ToArray(),
                    };
                })
                .ToArray();
        }

        private static string AssemblyLocation(Assembly assembly)
        {
            return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(assembly.CodeBase).Path));
        }

        private static string ResourceAssemblyFileName(Assembly assembly)
        {
            string fileName = Path.GetFileNameWithoutExtension(assembly.Location) + ".resources.dll";

            return fileName;
        }

        private static (string assemblyLocation, string assemblyFileName) AssemblyFileDetails(Assembly assembly)
        {
            return (
                assemblyLocation: AssemblyLocation(assembly),
                assemblyFileName: ResourceAssemblyFileName(assembly)
            );
        }

        private static Func<CultureInfo, CultureInfo?> CultureSupported(Assembly assembly) => (CultureInfo ci) =>
        {
            (string assemblyLocation, string assemblyFileName) = AssemblyFileDetails(assembly);
            string filePath = Path.Combine(assemblyLocation, ci.Name, assemblyFileName);
            CultureInfo? cultureInfo = File.Exists(filePath) ? ci : null;

            Console.WriteLine("{0} FilePath: {1}", cultureInfo != null ? "Exists" : "", filePath);

            return cultureInfo;
        };

        private static CultureInfo[] GetSupportedCultures(Assembly assembly)
        {
            CultureInfo[]? result = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Where(ci => !ci.Equals(CultureInfo.InvariantCulture))
                    .Select(CultureSupported(assembly))
                    .Where(x => x != null)
                    .OfType<CultureInfo>()
                    .ToArray();

            return result ?? new CultureInfo[0];
        }

        private static Func<CultureInfo, Assembly?> GetResourceAssembly(Assembly assembly) => (CultureInfo ci) =>
        {
            (string assemblyLocation, string assemblyFileName) = AssemblyFileDetails(assembly);
            string filePath = Path.Combine(assemblyLocation, ci.Name, assemblyFileName);
            Assembly? resourceAssembly = File.Exists(filePath) ? Assembly.LoadFrom(filePath) : null;

            return resourceAssembly;
        };

        private static ResourceItem[] GetResourceFileContents(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            string[] names = assembly.GetManifestResourceNames();

            ResourceItem[]? result = names
                .Select(name => new
                {
                    Name = name,
                    Stream = assembly.GetManifestResourceStream(name),
                })
                .Select(item =>
                {
                    using StreamReader sr = new StreamReader(item.Stream);
                    return new EmbededResourceFile(item.Name, sr.ReadToEnd());
                })
                .SelectMany(file =>
                {
                    return JsonDocument.Parse(file.Content)
                        .RootElement
                        .EnumerateObject()
                        .Select(item => new ResourceItem { Key = item.Name, Value = item.Value.GetString() })
                        .ToArray();
                })
                .ToArray();

            return result;
        }

        private static AssemblyCultures[] GetAssemblyCultures(params Assembly[] assemblies)
        {
            AssemblyCultures[]? result = assemblies.Select(mainAssembly => (mainAssembly, cultures: GetSupportedCultures(mainAssembly)))
                .Select(item =>
                {
                    var getResourceAssembly = GetResourceAssembly(item.mainAssembly);

                    return new AssemblyCultures
                    {
                        MainAssembly = item.mainAssembly,
                        Resources = item.cultures
                            .Select(ci =>
                            (
                                ResourceCultureInfo: ci,
                                ResourceAssembly: getResourceAssembly(ci)
                            ))
                            .Where(x => x.ResourceAssembly != null)
                            .OfType<(CultureInfo, Assembly)>()
                            .ToArray(),
                    };
                })
                .ToArray();

            return result ?? new AssemblyCultures[0];
        }
    }
}
