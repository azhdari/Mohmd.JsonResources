using System.Collections.Generic;

namespace Mohmd.JsonResources.Internal
{
    internal class ResourceCollection
    {
        public ResourceCollection(string locale, IDictionary<string, string> resources)
        {
            Locale = locale;
            Resources = resources;
        }

        public string Locale { get; }

        public IDictionary<string, string> Resources { get; }
    }
}
