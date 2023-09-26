using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
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
    private readonly TabSelected     _selectionEvent;

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
    {
        public ColorId Color;
    }

    public DesignFileSystemSelector(DesignManager designManager, DesignFileSystem fileSystem, KeyState keyState, DesignChanged @event,
        Configuration config, DesignConverter converter, TabSelected selectionEvent)
        : base(fileSystem, keyState, allowMultipleSelection: true)
    {
        _designManager  = designManager;
        _event          = @event;
        _config         = config;
        _converter      = converter;
        _selectionEvent = selectionEvent;
        _event.Subscribe(OnDesignChange, DesignChanged.Priority.DesignFileSystemSelector);
        _selectionEvent.Subscribe(OnTabSelected, TabSelected.Priority.DesignSelector);

        AddButton(NewDesignButton,    0);
        AddButton(ImportDesignButton, 10);
        AddButton(CloneDesignButton,  20);
        AddButton(DeleteButton,       1000);
        SetFilterTooltip();
    }

    protected override void DrawPopups()
    {
        DrawNewDesignPopup();
    }

    protected override void DrawLeafName(FileSystem<Design>.Leaf leaf, in DesignState state, bool selected)
    {
        var       flag  = selected ? ImGuiTreeNodeFlags.Selected | LeafFlags : LeafFlags;
        var       name  = IncognitoMode ? leaf.Value.Incognito : leaf.Value.Name.Text;
        using var color = ImRaii.PushColor(ImGuiCol.Text, state.Color.Value());
        using var _     = ImRaii.TreeNode(name, flag);
    }

    public override void Dispose()
    {
        base.Dispose();
        _event.Unsubscribe(OnDesignChange);
        _selectionEvent.Unsubscribe(OnTabSelected);
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
            case DesignChanged.Type.ApplyCustomize:
            case DesignChanged.Type.ApplyEquip:
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
            var design = _converter.FromBase64(_clipboardText, true, true, out _);
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

    private void OnTabSelected(MainWindow.TabType type, Design? design)
    {
        if (type == MainWindow.TabType.Designs && design != null)
            SelectByValue(design);
    }

    #region Filters

    private const StringComparison IgnoreCase    = StringComparison.OrdinalIgnoreCase;
    private       LowerString      _designFilter = LowerString.Empty;
    private       int              _filterType   = -1;

    private void SetFilterTooltip()
    {
        FilterTooltip = "Filter designs for those where their full paths or names contain the given substring.\n"
          + "Enter m:[string] to filter for designs with with a mod association containing the string.\n"
          + "Enter t:[string] to filter for designs set to specific tags.\n"
          + "Enter n:[string] to filter only for design names and no paths.";
    }

    /// <summary> Appropriately identify and set the string filter and its type. </summary>
    protected override bool ChangeFilter(string filterValue)
    {
        (_designFilter, _filterType) = filterValue.Length switch
        {
            0 => (LowerString.Empty, -1),
            > 1 when filterValue[1] == ':' =>
                filterValue[0] switch
                {
                    'n' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 1),
                    'N' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 1),
                    'm' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 2),
                    'M' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 2),
                    't' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 3),
                    'T' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 3),
                    _   => (new LowerString(filterValue), 0),
                },
            _ => (new LowerString(filterValue), 0),
        };

        return true;
    }

    /// <summary>
    /// The overwritten filter method also computes the state.
    /// Folders have default state and are filtered out on the direct string instead of the other options.
    /// If any filter is set, they should be hidden by default unless their children are visible,
    /// or they contain the path search string.
    /// </summary>
    protected override bool ApplyFiltersAndState(FileSystem<Design>.IPath path, out DesignState state)
    {
        if (path is DesignFileSystem.Folder f)
        {
            state = default;
            return FilterValue.Length > 0 && !f.FullName().Contains(FilterValue, IgnoreCase);
        }

        return ApplyFiltersAndState((DesignFileSystem.Leaf)path, out state);
    }

    /// <summary> Apply the string filters. </summary>
    private bool ApplyStringFilters(DesignFileSystem.Leaf leaf, Design design)
    {
        return _filterType switch
        {
            -1 => false,
            0  => !(_designFilter.IsContained(leaf.FullName()) || design.Name.Contains(_designFilter)),
            1  => !design.Name.Contains(_designFilter),
            2  => !design.AssociatedMods.Any(kvp => _designFilter.IsContained(kvp.Key.Name)),
            3  => !design.Tags.Any(_designFilter.IsContained),
            _  => false, // Should never happen
        };
    }

    /// <summary> Combined wrapper for handling all filters and setting state. </summary>
    private bool ApplyFiltersAndState(DesignFileSystem.Leaf leaf, out DesignState state)
    {
        var applyEquip     = leaf.Value.ApplyEquip != 0;
        var applyCustomize = (leaf.Value.ApplyCustomize & ~(CustomizeFlag.BodyType | CustomizeFlag.Race)) != 0;

        state.Color = (applyEquip, applyCustomize) switch
        {
            (false, false) => ColorId.StateDesign,
            (false, true)  => ColorId.CustomizationDesign,
            (true, false)  => ColorId.EquipmentDesign,
            (true, true)   => ColorId.NormalDesign,
        };

        return ApplyStringFilters(leaf, leaf.Value);
    }

    #endregion
}
