using Glamourer.FileSystem;

namespace Glamourer.Designs
{
    public class Design : IFileSystemBase
    {
        public Folder Parent { get; set; }
        public string Name   { get; set; }

        public CharacterSave Data { get; set; }

        internal Design(Folder parent, string name)
        {
            Parent = parent;
            Name   = name;
            Data   = new CharacterSave();
        }

        public override string ToString()
            => Name;
    }
}
