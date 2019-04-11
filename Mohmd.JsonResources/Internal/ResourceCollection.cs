using System.Collections.Generic;

namespace Mohmd.JsonResources.Internal
{
    internal class ResourceCollection
    {
        public string Locale { get; set; }

        public IDictionary<string, string> Resources { get; set; }
    }
}
