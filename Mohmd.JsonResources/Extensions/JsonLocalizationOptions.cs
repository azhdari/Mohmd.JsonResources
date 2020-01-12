namespace Mohmd.JsonResources.Extensions
{
    public class JsonLocalizationOptions
    {
        public string ResourcesPath { get; set; }
        public string GlobalResourceFileName { get; set; }
        public string AreasResourcePrefix { get; set; }
        public bool SetDefaultCultureCookie { get; set; } = true;
    }
}
