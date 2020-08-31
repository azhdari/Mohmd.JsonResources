using System.Globalization;
using System.Reflection;

namespace Mohmd.JsonResourcesFP.Types
{
    public struct AssemblyCultures
    {
        public Assembly MainAssembly { get; set; }

        public (CultureInfo ci, Assembly ass)[] Resources { get; set; }
    }
}
