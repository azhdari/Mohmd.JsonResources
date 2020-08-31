using LanguageExt;
using Mohmd.JsonResourcesFP.Types;
using System;
using System.Collections.Concurrent;
using static LanguageExt.Prelude;

namespace Mohmd.JsonResourcesFP
{
    public class MemoryStorage
    {
        private static readonly ConcurrentDictionary<ResourceFileKey, ResourceFileContent> _storage = new ConcurrentDictionary<ResourceFileKey, ResourceFileContent>();

        public static Func<ResourceFileContent, Either<StorageError, ResourceFileContent>> Store(ResourceFileKey key) => (ResourceFileContent content) =>
        {
            if (_storage.TryAdd(key, content))
            {
                return Right(content);
            }
            else
            {
                return Left(new StorageError { Message = "Duplicate key" });
            }
        };

        public static Func<Option<ResourceFileContent>> Find(ResourceFileKey key) => () =>
        {
            return Try(() => _storage[key])
                .ToOption();
        };

        public static Func<ResourceKey, Option<ResourceValue>> FindKey(ResourceFileContent content) => (ResourceKey key) =>
        {
            return Try(() => content[key])
                .ToOption();
        };
    }
}
