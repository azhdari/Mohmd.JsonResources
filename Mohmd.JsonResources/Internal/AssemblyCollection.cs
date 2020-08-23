using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mohmd.JsonResources.Internal
{
    public static class AssemblyCollection
    {
        static AssemblyCollection()
        {
            Assemblies = new ConcurrentBag<Assembly>();
        }

        public static ConcurrentBag<Assembly> Assemblies { get; }

        public static EmbededResourceItem[] GetResourceFileContents(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new System.ArgumentNullException(nameof(assembly));
            }

            string[] names = assembly.GetManifestResourceNames();

            return names
                .Select(name => new
                {
                    Name = name,
                    Stream = assembly.GetManifestResourceStream(name),
                })
                .Select(item =>
                {
                    using StreamReader sr = new StreamReader(item.Stream);
                    return new EmbededResourceItem(item.Name, sr.ReadToEnd());
                })
                .ToArray();
        }

        internal static EmbededResourceItem[] GetAllResourceFileContents()
        {
            return Assemblies?.ToList().SelectMany(GetResourceFileContents).ToArray() ?? new EmbededResourceItem[0];
        }
    }
}
