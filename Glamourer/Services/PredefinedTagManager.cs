using Glamourer.Designs;
using Glamourer.Gui;
using Luna;

namespace Glamourer.Services;

public sealed class PredefinedTagManager : PredefinedTagManager<FilenameService, Design>
{
    private readonly DesignManager _designs;

    public PredefinedTagManager(SaveService saveService, MessageService messager, DesignManager designs)
        : base(saveService, messager)
    {
        _designs = designs;
        Load();
    }

    public override string LocalTagName
        => "tag";

    public override string ObjectName
        => "design";

    public override string ToFilePath(FilenameService fileNames)
        => fileNames.PredefinedTagFile;

    protected override IReadOnlyCollection<string> GetLocalTags(Design obj)
        => obj.Tags;

    public override Vector4 AddButtonColor
        => ColorId.PredefinedTagAdd.Vector;

    public override Vector4 RemoveButtonColor
        => ColorId.PredefinedTagRemove.Vector;

    protected override void ChangeLocalTag(Design obj, int tagIndex, string tag)
    {
        if (tag.Length is 0)
            _designs.RemoveTag(obj, tagIndex);
        else if (tagIndex == obj.Tags.Length)
            _designs.AddTag(obj, tag);
        else
            _designs.RenameTag(obj, tagIndex, tag);
    }
}
