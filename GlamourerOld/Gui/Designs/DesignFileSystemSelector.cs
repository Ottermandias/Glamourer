using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Glamourer.Designs;
using OtterGui;
using OtterGui.FileSystem.Selector;

namespace Glamourer.Gui.Designs;

public sealed class DesignFileSystemSelector : FileSystemSelector<Design, DesignFileSystemSelector.DesignState>
{
    private readonly DesignManager _designManager;

    public struct DesignState
    { }

    public DesignFileSystemSelector(DesignManager designManager, DesignFileSystem fileSystem, KeyState keyState)
        : base(fileSystem, keyState)
    {
        _designManager              =  designManager;
        _designManager.DesignChange += OnDesignChange;
        AddButton(DeleteButton, 1000);
    }

    public override void Dispose()
    {
        base.Dispose();
        _designManager.DesignChange -= OnDesignChange;
    }

    private void OnDesignChange(DesignManager.DesignChangeType type, Design design, object? oldData)
    {
        switch (type)
        {
            case DesignManager.DesignChangeType.ReloadedAll:
            case DesignManager.DesignChangeType.Renamed:
            case DesignManager.DesignChangeType.AddedTag:
            case DesignManager.DesignChangeType.ChangedTag:
            case DesignManager.DesignChangeType.RemovedTag:
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
            _designManager.Delete(Selected);
    }
}
