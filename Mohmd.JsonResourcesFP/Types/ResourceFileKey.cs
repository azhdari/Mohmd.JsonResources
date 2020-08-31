using System;

namespace Mohmd.JsonResourcesFP.Types
{
    public struct ResourceFileKey
    {
        public ResourceFileKey(string name, Type? type = null)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type? Type { get; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }
    }
}
