using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mohmd.JsonResources.Extensions;

namespace Mohmd.JsonResources.Providers
{
    public class JsonResourceProviderFactory : IJsonResourceProviderFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHostingEnvironment _env;
        private readonly IActionContextAccessor _actionContextAccessor;

        public JsonResourceProviderFactory(ILoggerFactory loggerFactory, IHostingEnvironment env, IActionContextAccessor actionContextAccessor)
        {
            _loggerFactory = loggerFactory;
            _env = env;
            _actionContextAccessor = actionContextAccessor;
        }

        public IJsonResourceProvider Create(string resourceBaseName, JsonLocalizationOptions options)
        {
            if (options.UseEmbededResources)
            {
                return new EmbeddedProvider(resourceBaseName, _actionContextAccessor, options);
            }
            else
            {
                return new FileProvider(resourceBaseName, _loggerFactory, _env, _actionContextAccessor, options);
            }
        }
    }
}
