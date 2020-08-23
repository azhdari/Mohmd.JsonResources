using Mohmd.JsonResources.Internal;
using System;
using System.Reflection;

namespace Mohmd.JsonResources.Extensions
{
    public class JsonLocalizationOptions
    {
        public string ResourcesPath { get; set; } = "Resources";
        public string GlobalResourceFileName { get; set; } = "Global";
        public string AreasResourcePrefix { get; set; } = "Area";
        public bool SetDefaultCultureCookie { get; set; } = true;

        public string DefaultUICultureName { get; set; } = "en-US";
        public bool UseEmbededResources { get; set; } = false;

        public static JsonLocalizationOptions Current { get; set; }

        public JsonLocalizationOptions AddAssembly<T>()
        {
            Type typeToFindAssembly = typeof(T);
            Assembly assembly = typeToFindAssembly.Assembly;
            return AddAssembly(assembly);
        }

        public JsonLocalizationOptions AddAssembly(Type typeToFindAssembly)
        {
            if (typeToFindAssembly is null)
            {
                throw new ArgumentNullException(nameof(typeToFindAssembly));
            }

            Assembly assembly = typeToFindAssembly.Assembly;
            return AddAssembly(assembly);
        }

        public JsonLocalizationOptions AddAssembly(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            AssemblyCollection.Assemblies.Add(assembly);
            return this;
        }
    }
}
