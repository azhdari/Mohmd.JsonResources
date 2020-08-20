namespace Mohmd.JsonResources.Extensions
{
    public class JsonLocalizationOptions
    {
        public string ResourcesPath { get; set; } = "Resources";
        public string GlobalResourceFileName { get; set; } = "Global";
        public string AreasResourcePrefix { get; set; } = "Area";
        public bool SetDefaultCultureCookie { get; set; } = true;

        public string DefaultUICultureName { get; set; } = "en-US";
        public bool UseEmbededResources { get; set; } = false;

        public static JsonLocalizationOptions Current { get; set; }
    }
}
