using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Internal.Types
{
    public struct AssemblyCultures
    {
        public Assembly MainAssembly { get; set; }

        public (CultureInfo ci, Assembly ass)[] Resources { get; set; }
    }
}
