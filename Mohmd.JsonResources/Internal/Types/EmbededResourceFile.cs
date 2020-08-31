namespace Mohmd.JsonResources.Internal.Types
{
    public class EmbededResourceFile
    {
        public EmbededResourceFile(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; set; }

        public string Content { get; set; }
    }
}
