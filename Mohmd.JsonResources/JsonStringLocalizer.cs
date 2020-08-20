using Microsoft.Extensions.Localization;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using Mohmd.JsonResources.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mohmd.JsonResources
{
    public class JsonStringLocalizer : JsonStringLocalizer<object>
    {
        public JsonStringLocalizer(string resourceName, JsonLocalizationOptions options, IJsonResourceProviderFactory resourceProviderFactory)
            : base(resourceName, options, resourceProviderFactory)
        {
        }
    }
}
