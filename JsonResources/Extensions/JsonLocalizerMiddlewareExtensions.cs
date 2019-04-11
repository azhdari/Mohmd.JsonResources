using Mohmd.JsonResources.Internal;

namespace Microsoft.AspNetCore.Builder
{
    public static class JsonLocalizerMiddlewareExtensions
    {
        public static IApplicationBuilder UseJsonLocalizer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JsonLocalizerMiddleware>();
        }
    }
}
