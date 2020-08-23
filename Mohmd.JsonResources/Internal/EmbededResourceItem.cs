namespace Mohmd.JsonResources.Internal
{
    public class EmbededResourceItem
    {
        public EmbededResourceItem(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; set; }

        public string Content { get; set; }
    }
}
