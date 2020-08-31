﻿using System.Globalization;
using System.Reflection;

namespace Mohmd.JsonResources.Internal.Types
{
    public struct AssemblyResources
    {
        public Assembly MainAssembly { get; set; }

        public ResourceItem[] DefaultResources { get; set; }

        public (CultureInfo CultureInfo, ResourceItem[] Resources)[] CultureResources { get; set; }
    }
}