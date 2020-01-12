using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Mohmd.JsonResources.Extensions;
using System.Globalization;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Internal
{
    internal class JsonLocalizerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptions<RequestLocalizationOptions> _options;
        private readonly IOptions<JsonLocalizationOptions> _jsonOptions;

        public JsonLocalizerMiddleware(RequestDelegate next,
                                       IOptions<RequestLocalizationOptions> options,
                                       IOptions<JsonLocalizationOptions> jsonOptions)
        {
            _next = next;
            _options = options;
            _jsonOptions = jsonOptions;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (_jsonOptions.Value.SetDefaultCultureCookie && !httpContext.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName))
            {
                httpContext.Response.Cookies.Append(
                    key: CookieRequestCultureProvider.DefaultCookieName,
                    value: CookieRequestCultureProvider.MakeCookieValue(_options.Value.DefaultRequestCulture));

                CultureInfo.CurrentCulture = _options.Value.DefaultRequestCulture.Culture;
                CultureInfo.CurrentUICulture = _options.Value.DefaultRequestCulture.UICulture;
            }

            await _next.Invoke(httpContext);
        }
    }
}
