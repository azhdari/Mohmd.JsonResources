using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Providers
{
    public interface IJsonResourceProvider
    {
        string? GetStringSafely(string name);
        JsonDocument? GetResourceObject(CultureInfo currentCulture);
    }
}
