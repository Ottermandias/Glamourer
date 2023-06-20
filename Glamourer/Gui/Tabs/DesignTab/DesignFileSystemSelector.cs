using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Events;
using OtterGui;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignFileSystemSelector : FileSystemSelector<Design, DesignFileSystemSelector.DesignState>
{
    private readonly DesignManager _designManager;
    private readonly DesignChanged _event;
    private readonly Configuration _config;

    public struct DesignState
    { }

    public DesignFileSystemSelector(DesignManager designManager, DesignFileSystem fileSystem, KeyState keyState, DesignChanged @event,
        Configuration config)
        : base(fileSystem, keyState)
    {
        _designManager = designManager;
        _event         = @event;
        _config        = config;
        _event.Subscribe(OnDesignChange, DesignChanged.Priority.DesignFileSystemSelector);
        AddButton(DeleteButton, 1000);
    }

    public override void Dispose()
    {
        base.Dispose();
        _event.Unsubscribe(OnDesignChange);
    }

    public override ISortMode<Design> SortMode
        => _config.SortMode;

    protected override uint ExpandedFolderColor
        => ColorId.FolderExpanded.Value();

    protected override uint CollapsedFolderColor
        => ColorId.FolderCollapsed.Value();

    protected override uint FolderLineColor
        => ColorId.FolderLine.Value();

    protected override bool FoldersDefaultOpen
        => _config.OpenFoldersByDefault;

    private void OnDesignChange(DesignChanged.Type type, Design design, object? oldData)
    {
        switch (type)
        {
            case DesignChanged.Type.ReloadedAll:
            case DesignChanged.Type.Renamed:
            case DesignChanged.Type.AddedTag:
            case DesignChanged.Type.ChangedTag:
            case DesignChanged.Type.RemovedTag:
                SetFilterDirty();
                break;
        }
    }

    private void DeleteButton(Vector2 size)
    {
        var keys = _config.DeleteDesignModifier.IsActive();
        var tt = SelectedLeaf == null
            ? "No design selected."
            : "Delete the currently selected design entirely from your drive.\n"
          + "This can not be undone.";
        if (!keys)
            tt += $"\nHold {_config.DeleteDesignModifier} while clicking to delete the mod.";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), size, tt, SelectedLeaf == null || !keys, true)
         && Selected != null)
            _designManager.Delete(Selected);
    }
}
