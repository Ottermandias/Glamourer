using Glamourer.Designs;
using OtterGui.FileSystem.Selector;

namespace Glamourer.Gui.Designs;

public sealed class DesignFileSystemSelector : FileSystemSelector<Design, DesignFileSystemSelector.DesignState>
{
    private readonly Design.Manager _manager;

    public struct DesignState
    { }

    public DesignFileSystemSelector(Design.Manager manager, DesignFileSystem fileSystem)
        : base(fileSystem, Dalamud.KeyState)
    {
        _manager              =  manager;
        _manager.DesignChange += OnDesignChange;
    }

    public override void Dispose()
    {
        base.Dispose();
        _manager.DesignChange -= OnDesignChange;
    }

    private void OnDesignChange(Design.Manager.DesignChangeType type, Design design, object? oldData)
    {
        switch (type)
        {
            case Design.Manager.DesignChangeType.ReloadedAll:
            case Design.Manager.DesignChangeType.Renamed:
            case Design.Manager.DesignChangeType.AddedTag:
            case Design.Manager.DesignChangeType.ChangedTag:
            case Design.Manager.DesignChangeType.RemovedTag:
                SetFilterDirty();
                break;
        }
    }
}
