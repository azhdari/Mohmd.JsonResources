using Mohmd.JsonResources.Extensions;

namespace Mohmd.JsonResources.Providers
{
    public interface IJsonResourceProviderFactory
    {
        IJsonResourceProvider Create(string resourceBaseName, JsonLocalizationOptions options);
    }
}
