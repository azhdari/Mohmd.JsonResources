using System;

namespace Mohmd.JsonResources.Internal.Types
{
    public struct ResourceFileKey
    {
        public ResourceFileKey(string name, string cultureName)
        {
            Name = name;
            CultureName = cultureName;
        }

        public string Name { get; }
        public string CultureName { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, CultureName);
        }
    }
}
