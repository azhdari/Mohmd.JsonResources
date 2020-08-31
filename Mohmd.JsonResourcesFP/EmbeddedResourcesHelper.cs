using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Mohmd.JsonResourcesFP.Types;
using static LanguageExt.Prelude;

namespace Mohmd.JsonResourcesFP
{
    public class EmbeddedResourcesHelper
    {
        public static string AssemblyLocation(Assembly assembly)
        {
            return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(assembly.CodeBase).Path));
        }

        public static string ResourceAssemblyFileName(Assembly assembly)
        {
            return pipe(AssemblyLocation(assembly),
                        location => Path.GetFileNameWithoutExtension(location) + ".resources.dll");
        }

        public static (string assemblyLocation, string assemblyFileName) AssemblyFileDetails(Assembly assembly)
        {
            return (
                assemblyLocation: AssemblyLocation(assembly),
                assemblyFileName: ResourceAssemblyFileName(assembly)
            );
        }

        public static Func<CultureInfo, Option<CultureInfo>> CultureSupported(Assembly assembly) => (CultureInfo ci) =>
        {
            return pipe(AssemblyFileDetails(assembly),
                 assDetails => Path.Combine(assDetails.assemblyLocation, ci.Name, assDetails.assemblyFileName),
                 path => File.Exists(path) ? Option<CultureInfo>.Some(ci) : Option<CultureInfo>.None);
        };

        public static Option<CultureInfo>[] GetSupportedCultures(Assembly assembly)
        {
            return CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Where(ci => !ci.Equals(CultureInfo.InvariantCulture))
                    .Select(CultureSupported(assembly))
                    .Where(x => x.IsSome)
                    .ToArray();
        }

        public static Func<CultureInfo, Option<Assembly>> GetResourceAssemblyLocation(Assembly assembly) => (CultureInfo ci) =>
        {
            return pipe(AssemblyFileDetails(assembly),
                 assDetails => Path.Combine(assDetails.assemblyLocation, ci.Name, assDetails.assemblyFileName),
                 path => File.Exists(path) ? Some(path) : Option<string>.None,
                 result => result.Map(path => Assembly.LoadFrom(path)));
        };

        public static Func<CultureInfo, Option<Assembly>> GetResourceAssembly(Assembly mainAssembly) => (CultureInfo ci) =>
        {
            pipe(CultureSupported(mainAssembly)(ci).ToEither(Option<CultureInfo>.None),
                 map<Option<CultureInfo>, CultureInfo>(ci => ));
        };

        public static AssemblyCultures[] GetAssemblyCultures(params Assembly[] assemblies)
        {
            assemblies.Select(ass => (ass, cultures: GetSupportedCultures(ass)))
                .SelectMany(item =>
                {
                    return item.cultures.Select(ci => pipe(
                        ci,
                        GetResourceAssemblyLocation(item.ass),
                        x => x.Map(assembly => (item.ass, ci, assembly))
                        ));
                })
                .MapT(x => x.Map(item => (mainAssembly: item.ass, (item.ci, item.assembly))))
                .Match(item =>
                {
                })
                .Select(item =>
                {
                    item.Map(x => new AssemblyCultures { MainAssembly = x.mainAssembly, Resources = new (CultureInfo ci, Assembly ass)[0] });
                });
        }
    }
}
