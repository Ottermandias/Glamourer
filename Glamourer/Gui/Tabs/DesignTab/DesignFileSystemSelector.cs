using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Designs;
using Glamourer.Events;
using ImGuiNET;
using OtterGui;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignFileSystemSelector : FileSystemSelector<Design, DesignFileSystemSelector.DesignState>
{
    private readonly DesignManager   _designManager;
    private readonly DesignChanged   _event;
    private readonly Configuration   _config;
    private readonly DesignConverter _converter;

    private string? _clipboardText;
    private Design? _cloneDesign = null;
    private string  _newName     = string.Empty;

    public bool IncognitoMode
    {
        get => _config.IncognitoMode;
        set
        {
            _config.IncognitoMode = value;
            _config.Save();
        }
    }

    public new DesignFileSystem.Leaf? SelectedLeaf
        => base.SelectedLeaf;

    public struct DesignState
    { }

    public DesignFileSystemSelector(DesignManager designManager, DesignFileSystem fileSystem, KeyState keyState, DesignChanged @event,
        Configuration config, DesignConverter converter)
        : base(fileSystem, keyState)
    {
        _designManager = designManager;
        _event         = @event;
        _config        = config;
        _converter     = converter;
        _event.Subscribe(OnDesignChange, DesignChanged.Priority.DesignFileSystemSelector);

        AddButton(NewDesignButton,    0);
        AddButton(ImportDesignButton, 10);
        AddButton(CloneDesignButton,  20);
        AddButton(DeleteButton,       1000);
    }

    protected override void DrawPopups()
    {
        DrawNewDesignPopup();
    }

    protected override void DrawLeafName(FileSystem<Design>.Leaf leaf, in DesignState state, bool selected)
    {
        var       flag = selected ? ImGuiTreeNodeFlags.Selected | LeafFlags : LeafFlags;
        var       name = IncognitoMode ? leaf.Value.Incognito : leaf.Value.Name.Text;
        using var _    = ImRaii.TreeNode(name, flag);
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
            case DesignChanged.Type.AddedMod:
            case DesignChanged.Type.RemovedMod:
            case DesignChanged.Type.Created:
            case DesignChanged.Type.Deleted:
                SetFilterDirty();
                break;
        }
    }

    private void NewDesignButton(Vector2 size)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size, "Create a new design with default configuration.", false,
                true))
            ImGui.OpenPopup("##NewDesign");
    }

    private void ImportDesignButton(Vector2 size)
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FileImport.ToIconString(), size, "Try to import a design from your clipboard.", false,
                true))
            return;

        try
        {
            _clipboardText = ImGui.GetClipboardText();
            ImGui.OpenPopup("##NewDesign");
        }
        catch
        {
            Glamourer.Chat.NotificationMessage("Could not import data from clipboard.", "Failure", NotificationType.Error);
        }
    }

    private void CloneDesignButton(Vector2 size)
    {
        var tt = SelectedLeaf == null
            ? "No design selected."
            : "Clone the currently selected design to a duplicate";
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clone.ToIconString(), size, tt, SelectedLeaf == null, true))
            return;

        _cloneDesign = Selected!;
        ImGui.OpenPopup("##NewDesign");
    }

    private void DeleteButton(Vector2 size)
    {
        var keys = _config.DeleteDesignModifier.IsActive();
        var tt = SelectedLeaf == null
            ? "No design selected."
            : "Delete the currently selected design entirely from your drive.\n"
          + "This can not be undone.";
        if (!keys)
            tt += $"\nHold {_config.DeleteDesignModifier} while clicking to delete the design.";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), size, tt, SelectedLeaf == null || !keys, true)
         && Selected != null)
            _designManager.Delete(Selected);
    }

    private void DrawNewDesignPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewDesign", ref _newName))
            return;

        if (_clipboardText != null)
        {
            var design = _converter.FromBase64(_clipboardText, true, true);
            if (design is Design d)
                _designManager.CreateClone(d, _newName);
            else if (design != null)
                _designManager.CreateClone(design, _newName);
            else
                Glamourer.Chat.NotificationMessage("Could not create a design, clipboard did not contain valid design data.", "Failure",
                    NotificationType.Error);
            _clipboardText = null;
        }
        else if (_cloneDesign != null)
        {
            _designManager.CreateClone(_cloneDesign, _newName);
            _cloneDesign = null;
        }
        else
        {
            _designManager.CreateEmpty(_newName);
        }

        _newName = string.Empty;
    }
}
