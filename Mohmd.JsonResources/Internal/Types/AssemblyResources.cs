using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Mohmd.JsonResources.Internal.Types
{
    [DebuggerDisplay("{MainAssembly.FullName}")]
    public struct AssemblyResources
    {
        public Assembly MainAssembly { get; set; }

        public EmbededResourceFile[] DefaultResources { get; set; }

        public (CultureInfo CultureInfo, EmbededResourceFile[] Files)[] CulturalFiles { get; set; }
    }
}
