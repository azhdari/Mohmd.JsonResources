using System.Diagnostics;

namespace Mohmd.JsonResources.Internal.Types
{
    [DebuggerDisplay("{Name} ({Items.Count})")]
    public class EmbededResourceFile
    {
        public EmbededResourceFile(string name, ResourceFileContent items)
        {
            Name = name;
            Items = items;
        }

        public string Name { get; set; }

        public ResourceFileContent Items { get; set; }
    }
}
