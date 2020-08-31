using System.Collections.Generic;

namespace Mohmd.JsonResources.Internal.Types
{
    public static class Extensions
    {
        public static ResourceFileContent ToResourceFileContent(this List<ResourceItem> items)
        {
            var fileContent = new ResourceFileContent();
            fileContent.AddRange(items);

            return fileContent;
        }
    }
}
