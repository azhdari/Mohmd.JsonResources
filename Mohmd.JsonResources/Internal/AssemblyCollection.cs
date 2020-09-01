using System.Collections.Concurrent;
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
    }
}
