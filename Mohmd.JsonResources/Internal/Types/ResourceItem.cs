using System.Diagnostics;

namespace Mohmd.JsonResources.Internal.Types
{
    [DebuggerDisplay("{Value}", Name = "{Key}")]
    public struct ResourceItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
