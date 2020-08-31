using Mohmd.JsonResources.Internal.Types;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Mohmd.JsonResources.Internal
{
    public class MemoryStorage
    {
        #region Fields

        private static readonly ConcurrentDictionary<ResourceFileKey, ResourceFileContent> _storage = new ConcurrentDictionary<ResourceFileKey, ResourceFileContent>();

        #endregion

        #region Properties

        public static ConcurrentBag<AssemblyResources> AssemblyResources { get; } = new ConcurrentBag<AssemblyResources>();

        #endregion

        #region Methods

        public static void Store(AssemblyResources assemblyResources)
        {
            AssemblyResources.Add(assemblyResources);
        }

        public static Func<ResourceFileContent, bool> Store(ResourceFileKey key) => (ResourceFileContent content) =>
        {
            return _storage.TryAdd(key, content);
        };

        public static ResourceFileContent? Find(ResourceFileKey key, Func<ResourceFileKey, ResourceFileContent?>? populate = null)
        {
            if (_storage.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                if (populate != null)
                {
                    var result = populate.Invoke(key);
                    if (result != null)
                    {
                        Store(key)(result);
                    }

                    return result;
                }

                return null;
            }
        }

        public static Func<string, ResourceItem?> FindKey(ResourceFileContent? content) => (string key) =>
        {
            return content?.FirstOrDefault(x => x.Key == key);
        };

        #endregion
    }
}
