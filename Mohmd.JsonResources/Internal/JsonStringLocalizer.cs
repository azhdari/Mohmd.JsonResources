using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Mohmd.JsonResources.Extensions;

namespace Mohmd.JsonResources.Internal
{
    internal class JsonStringLocalizer : JsonStringLocalizer<object>
    {
        public JsonStringLocalizer(string resourceBaseName,
                                   IHostingEnvironment env,
                                   JsonGlobalResources globalResources,
                                   RequestCulture defaultCulture,
                                   IActionContextAccessor actionContextAccessor,
                                   JsonLocalizationOptions options,
                                   ILoggerFactory loggerFactory) : base(resourceBaseName, env, globalResources, defaultCulture, actionContextAccessor, options, loggerFactory)
        {
        }
    }
}
