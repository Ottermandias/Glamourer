using Glamourer.Designs;

namespace Glamourer.FileSystem
{
    public class Link : IFileSystemBase
    {
        public Folder Parent { get; set; }

        public  string Name   { get; set; }

        public IFileSystemBase Data { get; }

        public Link(Folder parent, string name, IFileSystemBase data)
        {
            Parent = parent;
            Name   = name;
            Data   = data;
        }
    }
}
