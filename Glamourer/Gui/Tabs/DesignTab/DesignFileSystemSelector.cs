using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;
using OtterGui.Log;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignFileSystemSelector : FileSystemSelector<Design, DesignFileSystemSelector.DesignState>
{
    private readonly DesignManager   _designManager;
    private readonly DesignChanged   _event;
    private readonly Configuration   _config;
    private readonly DesignConverter _converter;
    private readonly TabSelected     _selectionEvent;
    private readonly DesignColors    _designColors;
    private readonly DesignApplier   _designApplier;

    private string? _clipboardText;
    private Design? _cloneDesign;
    private string  _newName = string.Empty;

    public bool IncognitoMode
    {
        get => _config.Ephemeral.IncognitoMode;
        set
        {
            _config.Ephemeral.IncognitoMode = value;
            _config.Ephemeral.Save();
        }
    }

    public new DesignFileSystem.Leaf? SelectedLeaf
        => base.SelectedLeaf;

    public record struct DesignState(uint Color)
    { }

    public DesignFileSystemSelector(DesignManager designManager, DesignFileSystem fileSystem, IKeyState keyState, DesignChanged @event,
        Configuration config, DesignConverter converter, TabSelected selectionEvent, Logger log, DesignColors designColors,
        DesignApplier designApplier)
        : base(fileSystem, keyState, log, allowMultipleSelection: true)
    {
        _designManager  = designManager;
        _event          = @event;
        _config         = config;
        _converter      = converter;
        _selectionEvent = selectionEvent;
        _designColors   = designColors;
        _designApplier  = designApplier;
        _event.Subscribe(OnDesignChange, DesignChanged.Priority.DesignFileSystemSelector);
        _selectionEvent.Subscribe(OnTabSelected, TabSelected.Priority.DesignSelector);
        _designColors.ColorChanged += SetFilterDirty;

        AddButton(NewDesignButton,    0);
        AddButton(ImportDesignButton, 10);
        AddButton(CloneDesignButton,  20);
        AddButton(DeleteButton,       1000);
        UnsubscribeRightClickLeaf(RenameLeaf);
        SetRenameSearchPath(_config.ShowRename);
        SetFilterTooltip();

        if (_config.Ephemeral.SelectedDesign == Guid.Empty)
            return;

        var design = designManager.Designs.ByIdentifier(_config.Ephemeral.SelectedDesign);
        if (design != null)
            SelectByValue(design);
    }

    public void SetRenameSearchPath(RenameField value)
    {
        switch (value)
        {
            case RenameField.RenameSearchPath:
                SubscribeRightClickLeaf(RenameLeafDesign, 1000);
                UnsubscribeRightClickLeaf(RenameDesign);
                break;
            case RenameField.RenameData:
                UnsubscribeRightClickLeaf(RenameLeafDesign);
                SubscribeRightClickLeaf(RenameDesign, 1000);
                break;
            case RenameField.BothSearchPathPrio:
                UnsubscribeRightClickLeaf(RenameLeafDesign);
                UnsubscribeRightClickLeaf(RenameDesign);
                SubscribeRightClickLeaf(RenameLeafDesign, 1001);
                SubscribeRightClickLeaf(RenameDesign,     1000);
                break;
            case RenameField.BothDataPrio:
                UnsubscribeRightClickLeaf(RenameLeafDesign);
                UnsubscribeRightClickLeaf(RenameDesign);
                SubscribeRightClickLeaf(RenameLeafDesign, 1000);
                SubscribeRightClickLeaf(RenameDesign,     1001);
                break;
            default:
                UnsubscribeRightClickLeaf(RenameLeafDesign);
                UnsubscribeRightClickLeaf(RenameDesign);
                break;
        }
    }

    private void RenameLeafDesign(DesignFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        RenameLeaf(leaf);
    }

    private void RenameDesign(DesignFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Name.Text;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Design:");
        if (ImGui.InputText("##RenameDesign", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _designManager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }

        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed design.");
    }

    protected override void Select(FileSystem<Design>.Leaf? leaf, bool clear, in DesignState storage = default)
    {
        base.Select(leaf, clear, storage);
        var id = SelectedLeaf?.Value.Identifier ?? Guid.Empty;
        if (id != _config.Ephemeral.SelectedDesign)
        {
            _config.Ephemeral.SelectedDesign = id;
            _config.Ephemeral.Save();
        }
    }

    protected override void DrawPopups()
    {
        DrawNewDesignPopup();
    }

    protected override void DrawLeafName(FileSystem<Design>.Leaf leaf, in DesignState state, bool selected)
    {
        var       flag  = selected ? ImGuiTreeNodeFlags.Selected | LeafFlags : LeafFlags;
        var       name  = IncognitoMode ? leaf.Value.Incognito : leaf.Value.Name.Text;
        using var color = ImRaii.PushColor(ImGuiCol.Text, state.Color);
        using var _     = ImRaii.TreeNode(name, flag);
        if (_config.AllowDoubleClickToApply && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            _designApplier.ApplyToPlayer(leaf.Value);
    }

    public override void Dispose()
    {
        base.Dispose();
        _event.Unsubscribe(OnDesignChange);
        _selectionEvent.Unsubscribe(OnTabSelected);
        _designColors.ColorChanged -= SetFilterDirty;
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
            case DesignChanged.Type.ApplyStain:
            case DesignChanged.Type.ApplyCrest:
            case DesignChanged.Type.Customize:
            case DesignChanged.Type.Equip:
            case DesignChanged.Type.ChangedColor:
                SetFilterDirty();
                break;
        }
    }

    private void NewDesignButton(Vector2 size)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size, "Create a new design with default configuration.", false,
                true))
        {
            _cloneDesign   = null;
            _clipboardText = null;
            ImGui.OpenPopup("##NewDesign");
        }
    }

    private void ImportDesignButton(Vector2 size)
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FileImport.ToIconString(), size, "Try to import a design from your clipboard.", false,
                true))
            return;

        try
        {
            _cloneDesign   = null;
            _clipboardText = ImGui.GetClipboardText();
            ImGui.OpenPopup("##NewDesign");
        }
        catch
        {
            Glamourer.Messager.NotificationMessage("Could not import data from clipboard.", NotificationType.Error, false);
        }
    }

    private void CloneDesignButton(Vector2 size)
    {
        var tt = SelectedLeaf == null
            ? "No design selected."
            : "Clone the currently selected design to a duplicate";
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clone.ToIconString(), size, tt, SelectedLeaf == null, true))
            return;

        _clipboardText = null;
        _cloneDesign   = Selected!;
        ImGui.OpenPopup("##NewDesign");
    }

    private void DeleteButton(Vector2 size)
        => DeleteSelectionButton(size, _config.DeleteDesignModifier, "design", "designs", _designManager.Delete);

    private void DrawNewDesignPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewDesign", ref _newName))
            return;

        if (_clipboardText != null)
        {
            var design = _converter.FromBase64(_clipboardText, true, true, out _);
            if (design is Design d)
                _designManager.CreateClone(d, _newName, true);
            else if (design != null)
                _designManager.CreateClone(design, _newName, true);
            else
                Glamourer.Messager.NotificationMessage("Could not create a design, clipboard did not contain valid design data.",
                    NotificationType.Error, false);
            _clipboardText = null;
        }
        else if (_cloneDesign != null)
        {
            _designManager.CreateClone(_cloneDesign, _newName, true);
            _cloneDesign = null;
        }
        else
        {
            _designManager.CreateEmpty(_newName, true);
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
          + "Enter c:[string] to filter for designs set to specific colors.\n"
          + "Enter i:[string] to filter for designs containing specific items.\n"
          + "Enter n:[string] to filter only for design names and no paths.\n\n"
          + "Use None as a placeholder value that only matches empty lists or names.";
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
                    'm' => filterValue.Length == 2 ? (LowerString.Empty, -1) : ParseFilter(filterValue, 2),
                    'M' => filterValue.Length == 2 ? (LowerString.Empty, -1) : ParseFilter(filterValue, 2),
                    't' => filterValue.Length == 2 ? (LowerString.Empty, -1) : ParseFilter(filterValue, 3),
                    'T' => filterValue.Length == 2 ? (LowerString.Empty, -1) : ParseFilter(filterValue, 3),
                    'i' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 4),
                    'I' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 4),
                    'c' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 5),
                    'C' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 5),
                    _   => (new LowerString(filterValue), 0),
                },
            _ => (new LowerString(filterValue), 0),
        };

        return true;
    }

    private const int EmptyOffset = 128;

    private static (LowerString, int) ParseFilter(string value, int id)
    {
        value = value[2..];
        var lower = new LowerString(value);
        return (lower, lower.Lower is "none" ? id + EmptyOffset : id);
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
            -1              => false,
            0               => !(_designFilter.IsContained(leaf.FullName()) || design.Name.Contains(_designFilter)),
            1               => !design.Name.Contains(_designFilter),
            2               => !design.AssociatedMods.Any(kvp => _designFilter.IsContained(kvp.Key.Name)),
            3               => !design.Tags.Any(_designFilter.IsContained),
            4               => !design.DesignData.ContainsName(_designFilter),
            5               => !_designFilter.IsContained(design.Color.Length == 0 ? DesignColors.AutomaticName : design.Color),
            2 + EmptyOffset => design.AssociatedMods.Count > 0,
            3 + EmptyOffset => design.Tags.Length > 0,
            _               => false, // Should never happen
        };
    }

    /// <summary> Combined wrapper for handling all filters and setting state. </summary>
    private bool ApplyFiltersAndState(DesignFileSystem.Leaf leaf, out DesignState state)
    {
        state = new DesignState(_designColors.GetColor(leaf.Value));
        return ApplyStringFilters(leaf, leaf.Value);
    }

    #endregion
}
