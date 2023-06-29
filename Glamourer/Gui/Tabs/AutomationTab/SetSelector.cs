using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetSelector : IDisposable
{
    private readonly Configuration       _config;
    private readonly AutoDesignManager   _manager;
    private readonly AutomationChanged   _event;
    private readonly ActorService        _actors;
    private readonly ObjectManager       _objects;
    private readonly List<AutoDesignSet> _list = new();

    public AutoDesignSet? Selection      { get; private set; }
    public int            SelectionIndex { get; private set; } = -1;

    public bool IncognitoMode
    {
        get => _config.IncognitoMode;
        set
        {
            _config.IncognitoMode = value;
            _config.Save();
        }
    }

    private int     _dragIndex = -1;
    private Action? _endAction;

    public SetSelector(AutoDesignManager manager, AutomationChanged @event, Configuration config, ActorService actors, ObjectManager objects)
    {
        _manager = manager;
        _event   = @event;
        _config  = config;
        _actors  = actors;
        _objects = objects;
        _event.Subscribe(OnAutomationChanged, AutomationChanged.Priority.SetSelector);
    }

    public void Dispose()
    {
        _event.Unsubscribe(OnAutomationChanged);
    }

    public string SelectionName
        => GetSetName(Selection, SelectionIndex);

    public string GetSetName(AutoDesignSet? set, int index)
        => set == null ? "No Selection" : IncognitoMode ? $"Auto Design Set #{index + 1}" : set.Name;

    private void OnAutomationChanged(AutomationChanged.Type type, AutoDesignSet? set, object? data)
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
            case AutomationChanged.Type.RenamedSet:
            case AutomationChanged.Type.MovedSet:
            case AutomationChanged.Type.ChangeIdentifier:
            case AutomationChanged.Type.ToggleSet:
                _dirty = true;
                break;
        }
    }

    private LowerString _filter        = LowerString.Empty;
    private uint        _enabledFilter = 0;
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
        foreach (var set in _manager)
        {
            var id = set.Identifier.ToString();
            if (CheckFilters(set, id))
                _list.Add(set);
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
        ImGui.SameLine();
        var f = _enabledFilter;

        if (ImGui.CheckboxFlags("##enabledFilter", ref f, 3))
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
        _objects.Update();
        ImGuiClip.ClippedDraw(_list, DrawSetSelectable, _selectableSize.Y + 2 * ImGui.GetStyle().ItemSpacing.Y);
        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawSetSelectable(AutoDesignSet set, int index)
    {
        using var id = ImRaii.PushId(index);
        using (var color = ImRaii.PushColor(ImGuiCol.Text, set.Enabled ? ColorId.EnabledAutoSet.Value() : ColorId.DisabledAutoSet.Value()))
        {
            if (ImGui.Selectable(GetSetName(set, index), set == Selection, ImGuiSelectableFlags.None, _selectableSize))
            {
                Selection      = set;
                SelectionIndex = index;
            }
        }

        var lineEnd   = ImGui.GetItemRectMax();
        var lineStart = new Vector2(ImGui.GetItemRectMin().X, lineEnd.Y);
        ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.Border), ImGuiHelpers.GlobalScale);

        DrawDragDrop(set, index);

        var text = set.Identifier.ToString();
        if (IncognitoMode)
            text = set.Identifier.Incognito(text);
        var textSize  = ImGui.CalcTextSize(text);
        var textColor = _objects.ContainsKey(set.Identifier) ? ColorId.AutomationActorAvailable : ColorId.AutomationActorUnavailable;
        ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().X - textSize.X,
            ImGui.GetCursorPosY() - ImGui.GetTextLineHeightWithSpacing()));
        ImGuiUtil.TextColored(textColor.Value(), text);
    }

    private void DrawSelectionButtons()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        var buttonWidth = new Vector2(_width / 3, 0);
        NewSetButton(buttonWidth);
        ImGui.SameLine();
        DuplicateSetButton(buttonWidth);
        ImGui.SameLine();
        DeleteSetButton(buttonWidth);
    }

    private void NewSetButton(Vector2 size)
    {
        var id = _actors.AwaitedService.GetCurrentPlayer();
        if (!id.IsValid)
            id = _actors.AwaitedService.CreatePlayer(ByteString.FromSpanUnsafe("New Design"u8, true, false, true), ushort.MaxValue);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size,
                $"Create a new Automatic Design Set for {id}. The associated player can be changed later.", !id.IsValid, true))
            _manager.AddDesignSet("New Design", id);
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
                : (true, $"Delete the currently selected design set.\n{_config.DeleteDesignModifier.ToString()}")
            : (true, "No Automatic Design Set selected.");
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), size, tt, disabled, true))
            _manager.DeleteDesignSet(SelectionIndex);
    }

    private void DrawDragDrop(AutoDesignSet set, int index)
    {
        const string dragDropLabel = "DesignSetDragDrop";
        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success && ImGuiUtil.IsDropping(dragDropLabel))
            {
                if (_dragIndex >= 0)
                {
                    var idx = _dragIndex;
                    _endAction = () => _manager.MoveSet(idx, index);
                }

                _dragIndex = -1;
            }
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source.Success && ImGui.SetDragDropPayload(dragDropLabel, nint.Zero, 0))
            {
                _dragIndex = index;
                ImGui.TextUnformatted($"Moving design set {GetSetName(set, index)} from position {index + 1}...");
            }
        }
    }
}
