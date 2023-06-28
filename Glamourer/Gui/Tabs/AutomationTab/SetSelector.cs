using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Events;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Vector2 = FFXIVClientStructs.FFXIV.Common.Math.Vector2;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetSelector : IDisposable
{
    private readonly AutoDesignManager _manager;
    private readonly AutomationChanged _event;
    public           AutoDesignSet?    Selection { get; private set; }
    public           int               SelectionIndex = -1;

    private          bool                IncognitoMode;
    private readonly List<AutoDesignSet> _list = new();

    public SetSelector(AutoDesignManager manager, AutomationChanged @event)
    {
        _manager = manager;
        _event   = @event;
        _event.Subscribe(OnAutomationChanged, AutomationChanged.Priority.SetSelector);
    }

    public void Dispose()
    {
        _event.Unsubscribe(OnAutomationChanged);
    }

    private void OnAutomationChanged(AutomationChanged.Type type, AutoDesignSet? set, object? data)
    {
        switch (type)
        {
            case AutomationChanged.Type.DeletedSet:
                if (set == Selection)
                {
                    Selection      = null;
                    SelectionIndex = -1;
                }

                _dirty = true;
                break;
            case AutomationChanged.Type.AddedSet:
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

        ImGuiUtil.HoverTooltip("Filter to show only enabled or disabled sets.");

        DrawSelector();
        DrawSelectionButtons();
    }

    private void DrawSelector()
    {
        using var child = ImRaii.Child("##actorSelector", new Vector2(_width, -ImGui.GetFrameHeight()), true);
        if (!child)
            return;

        UpdateList();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, _defaultItemSpacing);
        _selectableSize = new Vector2(0, 2 * ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y);
        ImGuiClip.ClippedDraw(_list, DrawSetSelectable, _selectableSize.Y + 2 * ImGui.GetStyle().ItemSpacing.Y);
    }

    private void DrawSetSelectable(AutoDesignSet set, int index)
    {
        using var id = ImRaii.PushId(index);
        using (var color = ImRaii.PushColor(ImGuiCol.Text, set.Enabled ? ColorId.EnabledAutoSet.Value() : ColorId.DisabledAutoSet.Value()))
        {
            if (ImGui.Selectable(set.Name, set == Selection, ImGuiSelectableFlags.None, _selectableSize))
            {
                Selection      = set;
                SelectionIndex = index;
            }
        }

        var text = set.Identifier.ToString();
        if (IncognitoMode)
            text = set.Identifier.Incognito(text);
        var textSize = ImGui.CalcTextSize(text);
        ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().X - textSize.X,
            ImGui.GetCursorPosY() - ImGui.GetTextLineHeightWithSpacing()));
        ImGui.TextUnformatted(text);
    }

    private void DrawSelectionButtons()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        var buttonWidth = new Vector2(_width / 1, 0);
        // TODO
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth
            , "Select the local player character.", true, true);
    }
}
