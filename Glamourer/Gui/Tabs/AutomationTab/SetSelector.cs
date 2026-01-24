using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Automation;
using Glamourer.Events;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Extensions;
using OtterGui.Raii;
using Penumbra.GameData.Interop;
using Penumbra.String;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetSelector : IDisposable
{
    private readonly Configuration              _config;
    private readonly AutoDesignManager          _manager;
    private readonly AutomationChanged          _event;
    private readonly ActorObjectManager         _objects;
    private readonly List<(AutoDesignSet, int)> _list = [];

    public AutoDesignSet? Selection      { get; private set; }
    public int            SelectionIndex { get; private set; } = -1;

    public bool IncognitoMode
    {
        get => _config.Ephemeral.IncognitoMode;
        set
        {
            _config.Ephemeral.IncognitoMode = value;
            _config.Ephemeral.Save();
        }
    }

    private int     _dragIndex = -1;
    private Action? _endAction;

    internal int DragDesignIndex = -1;

    public SetSelector(AutoDesignManager manager, AutomationChanged @event, Configuration config, ActorObjectManager objects)
    {
        _manager = manager;
        _event   = @event;
        _config  = config;
        _objects = objects;
        _event.Subscribe(OnAutomationChange, AutomationChanged.Priority.SetSelector);
    }

    public void Dispose()
    {
        _event.Unsubscribe(OnAutomationChange);
    }

    public string SelectionName
        => GetSetName(Selection, SelectionIndex);

    public string GetSetName(AutoDesignSet? set, int index)
        => set == null ? "No Selection" : IncognitoMode ? $"Auto Design Set #{index + 1}" : set.Name;

    private void OnAutomationChange(AutomationChanged.Type type, AutoDesignSet? set, object? data)
    {
        switch (type)
        {
            case AutomationChanged.Type.DeletedSet:
                if (set == Selection)
                {
                    SelectionIndex = _manager.Count == 0 ? -1 : SelectionIndex == 0 ? 0 : SelectionIndex - 1;
                    Selection      = SelectionIndex >= 0 ? _manager[SelectionIndex] : null;
                }

                _dirty = true;
                break;
            case AutomationChanged.Type.AddedSet:
                SelectionIndex = (((int, string))data!).Item1;
                Selection      = set!;
                _dirty         = true;
                break;
            case AutomationChanged.Type.MovedSet:
                _dirty               = true;
                var (oldIdx, newIdx) = ((int, int))data!;
                if (SelectionIndex == oldIdx)
                    SelectionIndex = newIdx;
                break;
            case AutomationChanged.Type.RenamedSet:
            case AutomationChanged.Type.ChangeIdentifier:
            case AutomationChanged.Type.ToggleSet:
                _dirty = true;
                break;
        }
    }

    private LowerString _filter        = LowerString.Empty;
    private uint        _enabledFilter;
    private float       _width;
    private Vector2     _defaultItemSpacing;
    private Vector2     _selectableSize;
    private bool        _dirty = true;

    private bool CheckFilters(AutoDesignSet set, string identifierString)
    {
        if (_enabledFilter switch
            {
                1 => set.Enabled,
                3 => !set.Enabled,
                _ => false,
            })
            return false;

        if (!_filter.IsEmpty && !_filter.IsContained(set.Name) && !_filter.IsContained(identifierString))
            return false;

        return true;
    }

    private void UpdateList()
    {
        if (!_dirty)
            return;

        _list.Clear();
        foreach (var (set, idx) in _manager.WithIndex())
        {
            var id = set.Identifiers[0].ToString();
            if (CheckFilters(set, id))
                _list.Add((set, idx));
        }
    }

    public bool HasSelection
        => Selection != null;

    public void Draw(float width)
    {
        _width = width;
        using var group = ImRaii.Group();
        _defaultItemSpacing = ImGui.GetStyle().ItemSpacing;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(_width - ImGui.GetFrameHeight());
        if (LowerString.InputWithHint("##filter", "Filter...", ref _filter, 64))
            _dirty = true;
        Im.Line.Same();
        var f = _enabledFilter;

        if (ImGui.CheckboxFlags("##enabledFilter", ref f, 3u))
        {
            _enabledFilter = _enabledFilter switch
            {
                0 => 3,
                3 => 1,
                _ => 0,
            };
            _dirty = true;
        }

        var pos = ImGui.GetItemRectMin();
        pos.X -= ImGuiHelpers.GlobalScale;
        ImGui.GetWindowDrawList().AddLine(pos, pos with { Y = ImGui.GetItemRectMax().Y }, ImGui.GetColorU32(ImGuiCol.Border),
            ImGuiHelpers.GlobalScale);

        ImGuiUtil.HoverTooltip("Filter to show only enabled or disabled sets.");

        DrawSelector();
        DrawSelectionButtons();
    }

    private void DrawSelector()
    {
        using var child = ImRaii.Child("##Selector", new Vector2(_width, -ImGui.GetFrameHeight()), true);
        if (!child)
            return;

        UpdateList();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, _defaultItemSpacing);
        _selectableSize = new Vector2(0, 2 * ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y);
        ImGuiClip.ClippedDraw(_list, DrawSetSelectable, _selectableSize.Y + 2 * ImGui.GetStyle().ItemSpacing.Y);
        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawSetSelectable((AutoDesignSet Set, int Index) pair)
    {
        using var id = ImRaii.PushId(pair.Index);
        using (ImRaii.PushColor(ImGuiCol.Text, pair.Set.Enabled ? ColorId.EnabledAutoSet.Value() : ColorId.DisabledAutoSet.Value()))
        {
            if (ImGui.Selectable(GetSetName(pair.Set, pair.Index), pair.Set == Selection, ImGuiSelectableFlags.None, _selectableSize))
            {
                Selection      = pair.Set;
                SelectionIndex = pair.Index;
            }
        }

        var lineEnd   = ImGui.GetItemRectMax();
        var lineStart = new Vector2(ImGui.GetItemRectMin().X, lineEnd.Y);
        ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.Border), ImGuiHelpers.GlobalScale);

        DrawDragDrop(pair.Set, pair.Index);

        var text = pair.Set.Identifiers[0].ToString();
        if (IncognitoMode)
            text = pair.Set.Identifiers[0].Incognito(text);
        var textSize  = ImGui.CalcTextSize(text);
        var textColor = pair.Set.Identifiers.Any(_objects.ContainsKey) ? ColorId.AutomationActorAvailable : ColorId.AutomationActorUnavailable;
        ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().X - textSize.X,
            ImGui.GetCursorPosY() - ImGui.GetTextLineHeightWithSpacing()));
        ImGuiUtil.TextColored(textColor.Value(), text);
    }

    private void DrawSelectionButtons()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        var buttonWidth = new Vector2(_width / 4, 0);
        NewSetButton(buttonWidth);
        Im.Line.Same();
        DuplicateSetButton(buttonWidth);
        Im.Line.Same();
        HelpButton(buttonWidth);
        Im.Line.Same();
        DeleteSetButton(buttonWidth);
    }

    private static void HelpButton(Vector2 size)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.QuestionCircle.ToIconString(), size, "How does Automation work?", false, true))
            ImGui.OpenPopup("Automation Help");

        static void HalfLine()
            => ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));

        const string longestLine =
            "A single set can contain multiple automated designs that apply under different conditions and different parts of their design.";

        ImGuiUtil.HelpPopup("Automation Help",
            new Vector2(ImGui.CalcTextSize(longestLine).X + 50 * ImGuiHelpers.GlobalScale, 33 * ImGui.GetTextLineHeightWithSpacing()), () =>
            {
                HalfLine();
                ImGui.TextUnformatted("What is Automation?");
                ImGui.BulletText("Automation helps you to automatically apply Designs to specific characters under specific circumstances.");
                HalfLine();

                ImGui.TextUnformatted("Automated Design Sets");
                ImGui.BulletText("First, you create automated design sets. An automated design set can be... ");
                using var indent = ImRaii.PushIndent();
                ImGuiUtil.BulletTextColored(ColorId.EnabledAutoSet.Value(),  "... enabled, or");
                ImGuiUtil.BulletTextColored(ColorId.DisabledAutoSet.Value(), "... disabled.");
                indent.Pop(1);
                ImGui.BulletText("You can create new, empty automated design sets, or duplicate existing ones.");
                ImGui.BulletText("You can name automated design sets arbitrarily.");
                ImGui.BulletText("You can re-order automated design sets via drag & drop in the selector.");
                ImGui.BulletText("Each automated design set is assigned to exactly one specific character.");
                indent.Push();
                ImGui.BulletText("On creation, it is assigned to your current Player Character.");
                ImGui.BulletText("You can assign sets to any players, retainers, mannequins and most human NPCs.");
                ImGui.BulletText("Only one automated design set can be enabled at the same time for each specific character.");
                indent.Push();
                ImGui.BulletText("Enabling another automatically disables the prior one.");
                indent.Pop(2);

                HalfLine();
                ImGui.TextUnformatted("Automated Designs");
                ImGui.BulletText(longestLine);
                ImGui.BulletText(
                    "The order of these automated designs can also be changed via drag & drop, and is relevant for the application.");
                ImGui.BulletText("Automated designs respect their own, coarse applications rules, and the designs own application rules.");
                ImGui.BulletText("Automated designs can be configured to be job- or job-group specific and only apply on these jobs, then.");
                ImGui.BulletText("There is also the special option 'Reset', which can be used to reset remaining slots to the game's values.");
                ImGui.BulletText(
                    "Automated designs apply from top to bottom, either on top of your characters current state, or its game state.");
                ImGui.BulletText("For a value to apply, it needs to:");
                indent.Push();
                ImGui.BulletText("Be configured to apply in the design itself.");
                ImGui.BulletText("Be configured to apply in the automation rules.");
                ImGui.BulletText("Fulfill the conditions of the automation rules.");
                ImGui.BulletText("Be a valid value for the current (on its own application) state of the character.");
                ImGui.BulletText("Not have had anything applied to the same value before from a different design.");
                indent.Pop(1);
            });
    }

    private void NewSetButton(Vector2 size)
    {
        var id = _objects.Actors.GetCurrentPlayer();
        if (!id.IsValid)
            id = _objects.Actors.CreatePlayer(ByteString.FromSpanUnsafe("New Design"u8, true, false, true), ushort.MaxValue);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size,
                $"Create a new Automatic Design Set for {id}. The associated player can be changed later.", !id.IsValid, true))
            _manager.AddDesignSet("New Automation Set", id);
    }

    private void DuplicateSetButton(Vector2 size)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clone.ToIconString(), size, "Duplicate the current Automatic Design Set.",
                Selection == null, true))
            _manager.DuplicateDesignSet(Selection!);
    }


    private void DeleteSetButton(Vector2 size)
    {
        var keyValid = _config.DeleteDesignModifier.IsActive();
        var (disabled, tt) = HasSelection
            ? keyValid
                ? (false, "Delete the currently selected design set.")
                : (true, $"Delete the currently selected design set.\nHold {_config.DeleteDesignModifier} to delete.")
            : (true, "No Automatic Design Set selected.");
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), size, tt, disabled, true))
            _manager.DeleteDesignSet(SelectionIndex);
    }

    private void DrawDragDrop(AutoDesignSet set, int index)
    {
        const string dragDropLabel = "DesignSetDragDrop";
        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success)
            {
                if (ImGuiUtil.IsDropping(dragDropLabel))
                {
                    if (_dragIndex >= 0)
                    {
                        var idx = _dragIndex;
                        _endAction = () => _manager.MoveSet(idx, index);
                    }

                    _dragIndex = -1;
                }
                else if (ImGuiUtil.IsDropping("DesignDragDrop"))
                {
                    if (DragDesignIndex >= 0)
                    {
                        var idx     = DragDesignIndex;
                        var setTo   = set;
                        var setFrom = Selection!;
                        _endAction = () => _manager.MoveDesignToSet(setFrom, idx, setTo);
                    }

                    DragDesignIndex = -1;
                }
            }
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                ImGui.TextUnformatted($"Moving design set {GetSetName(set, index)} from position {index + 1}...");
                if (ImGui.SetDragDropPayload(dragDropLabel, null, 0))
                    _dragIndex = index;
            }
        }
    }
}
