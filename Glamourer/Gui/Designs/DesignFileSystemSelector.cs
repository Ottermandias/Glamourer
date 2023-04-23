using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Glamourer.Designs;
using OtterGui;
using OtterGui.FileSystem.Selector;

namespace Glamourer.Gui.Designs;

public sealed class DesignFileSystemSelector : FileSystemSelector<Design, DesignFileSystemSelector.DesignState>
{
    private readonly Design.Manager _manager;

    public struct DesignState
    { }

    public DesignFileSystemSelector(Design.Manager manager, DesignFileSystem fileSystem, KeyState keyState)
        : base(fileSystem, keyState)
    {
        _manager              =  manager;
        _manager.DesignChange += OnDesignChange;
        AddButton(DeleteButton, 1000);
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

    private void DeleteButton(Vector2 size)
    {
        var keys = true;
        var tt = SelectedLeaf == null
            ? "No design selected."
            : "Delete the currently selected design entirely from your drive.\n"
          + "This can not be undone.";
        //if (!keys)
        //    tt += $"\nHold {_config.DeleteModModifier} while clicking to delete the mod.";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), size, tt, SelectedLeaf == null || !keys, true)
         && Selected != null)
            _manager.Delete(Selected);
    }
}
