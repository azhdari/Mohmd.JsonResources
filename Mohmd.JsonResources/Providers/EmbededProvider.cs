using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Providers
{
    public class EmbededProvider : IJsonResourceProvider
    {
        public JsonDocument? GetResourceObject(CultureInfo currentCulture)
        {
            throw new NotImplementedException();
        }

        public string? GetStringSafely(string name)
        {
            throw new NotImplementedException();
        }
    }
}
