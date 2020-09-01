using System.Globalization;
using System.Reflection;

namespace Mohmd.JsonResources.Internal.Types
{
    public struct AssemblyResources
    {
        public Assembly MainAssembly { get; set; }

        public EmbededResourceFile[] DefaultResources { get; set; }

        public (CultureInfo CultureInfo, EmbededResourceFile[] Files)[] CulturalFiles { get; set; }
    }
}
