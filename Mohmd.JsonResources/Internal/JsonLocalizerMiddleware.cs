using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.Internal
{
    internal class JsonLocalizerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestLocalizationOptions _options;

        public JsonLocalizerMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName))
            {
                httpContext.Response.Cookies.Append(
                    key: CookieRequestCultureProvider.DefaultCookieName,
                    value: CookieRequestCultureProvider.MakeCookieValue(_options.DefaultRequestCulture));

                CultureInfo.CurrentCulture = _options.DefaultRequestCulture.Culture;
                CultureInfo.CurrentUICulture = _options.DefaultRequestCulture.UICulture;
            }

            await _next.Invoke(httpContext);
        }
    }
}
