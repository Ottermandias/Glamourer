using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;

namespace Glamourer.Gui;

internal partial class Interface
{
    private const string FixDragDropLabel = "##FixDragDrop";

    private          List<string>?   _fullPathCache;
    private          string          _newFixCharacterName = string.Empty;
    private          string          _newFixDesignPath    = string.Empty;
    private          JobGroup?       _newFixDesignGroup;
    private          Design?         _newFixDesign;
    private          int             _fixDragDropIdx = -1;
    private readonly HashSet<string> _closedGroups   = new();

    private static unsafe bool IsDropping()
        => ImGui.AcceptDragDropPayload(FixDragDropLabel).NativePtr != null;

    private static string NormalizeIdentifier(string value)
        => value.Replace(" ", "_").Replace("#", "_");

    private void DrawFixedDesignsTab()
    {
        _newFixDesignGroup ??= _plugin.FixedDesigns.JobGroups[1];

        using var raii = new ImGuiRaii();
        if (!raii.Begin(() => ImGui.BeginTabItem("Fixed Designs"), ImGui.EndTabItem))
        {
            _fullPathCache     = null;
            _newFixDesign      = null;
            _newFixDesignPath  = string.Empty;
            _newFixDesignGroup = _plugin.FixedDesigns.JobGroups[1];
            return;
        }

        _fullPathCache ??= _plugin.FixedDesigns.Data.Select(d => d.Design.FullName()).ToList();

        Action? endAction   = null;
        var     buttonWidth = ImGui.GetFrameHeight();
        var groups = _plugin.FixedDesigns.Data.Select((d, i) => (d, i)).GroupBy(d => d.d.Name)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, g.ToList()))
            .ToList();


        raii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
        raii.PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero);
        if (ImGui.Button("Collapse All Groups", new Vector2(ImGui.GetContentRegionAvail().X / 2, 0)))
            _closedGroups.UnionWith(groups.Select(kvp => kvp.Key));
        ImGui.SameLine();
        if (ImGui.Button("Expand All Groups", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
            _closedGroups.Clear();
        raii.PopStyles(2);

        raii.Begin(() => ImGui.BeginTable("##FixedTable", 5, ImGuiTableFlags.RowBg), ImGui.EndTable);


        ImGui.TableSetupColumn("##DeleteColumn", ImGuiTableColumnFlags.WidthFixed, buttonWidth);
        ImGui.TableSetupColumn("##EnableColumn", ImGuiTableColumnFlags.WidthFixed, buttonWidth);
        ImGui.TableSetupColumn("Character",      ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Jobs",           ImGuiTableColumnFlags.WidthFixed, 175 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Design",         ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        void DrawDesign(FixedDesigns.FixedDesign design, int idx)
        {
            ImGui.PushID(idx);
            var path = _fullPathCache[idx];
            ImGui.TableNextColumn();
            raii.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
                endAction = () =>
                {
                    _fullPathCache.RemoveAt(idx);
                    _plugin.FixedDesigns.Remove(design);
                };
            raii.PopFonts();

            var tmp = design.Enabled;
            ImGui.TableNextColumn();
            if (ImGui.Checkbox("##Enabled", ref tmp))
                if (tmp && _plugin.FixedDesigns.EnableDesign(design)
                 || !tmp && _plugin.FixedDesigns.DisableDesign(design))
                {
                    Glamourer.Config.FixedDesigns[idx].Enabled = tmp;
                    Glamourer.Config.Save();
                }

            ImGui.TableNextColumn();
            ImGui.Selectable($"{design.Name}##Fix");
            if (ImGui.BeginDragDropSource())
            {
                _fixDragDropIdx = idx;
                ImGui.SetDragDropPayload("##FixDragDrop", IntPtr.Zero, 0);
                ImGui.Text($"Dragging {design.Name} ({path})...");
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                if (IsDropping() && _fixDragDropIdx >= 0)
                {
                    var i = _fixDragDropIdx;
                    var d = _plugin.FixedDesigns.Data[_fixDragDropIdx];
                    var p = _fullPathCache[_fixDragDropIdx];
                    endAction = () =>
                    {
                        _plugin.FixedDesigns.Move(d, idx);
                        _fullPathCache.RemoveAt(i);
                        _fullPathCache.Insert(idx, p);
                        _fixDragDropIdx = -1;
                    };
                }

                ImGui.EndDragDropTarget();
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(_plugin.FixedDesigns.Data[idx].Jobs.Name);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(path);
            ImGui.PopID();
        }

        foreach (var (playerName, designs) in groups)
        {
            if (designs.Count >= Glamourer.Config.GroupFixedWhen)
            {
                var isOpen = !_closedGroups.Contains(playerName);
                var color  = isOpen ? 0x40005000u : 0x40000050u;
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, color);
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, color);

                var groupIcon = isOpen ? FontAwesomeIcon.CaretDown : FontAwesomeIcon.CaretRight;
                ImGui.PushID(playerName);
                raii.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(groupIcon.ToIconString(), new Vector2(buttonWidth)))
                {
                    if (isOpen)
                        _closedGroups.Add(playerName);
                    else
                        _closedGroups.Remove(playerName);
                    isOpen = !isOpen;
                }

                raii.PopFonts();

                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, color);
                ImGui.TextUnformatted(playerName);
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, color);
                ImGui.TextUnformatted($"{designs.Count} Designs");
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, color);
                if (isOpen)
                    foreach (var (d, i) in designs)
                        DrawDesign(d, i);
                ImGui.PopID();
            }
            else
            {
                foreach (var (d, i) in designs)
                    DrawDesign(d, i);
            }
        }

        endAction?.Invoke();

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        raii.PushFont(UiBuilder.IconFont);

        if (_newFixDesign == null || _newFixCharacterName == string.Empty)
        {
            raii.PushStyle(ImGuiStyleVar.Alpha, 0.5f);
            ImGui.Button($"{FontAwesomeIcon.Plus.ToIconChar()}##NewFix");
            raii.PopStyles();
        }
        else if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconChar()}##NewFix"))
        {
            _fullPathCache.Add(_newFixDesignPath);
            _plugin.FixedDesigns.Add(_newFixCharacterName, _newFixDesign, _newFixDesignGroup.Value, false);
            _newFixCharacterName = string.Empty;
            _newFixDesignPath    = string.Empty;
            _newFixDesign        = null;
            _newFixDesignGroup   = _plugin.FixedDesigns.JobGroups[1];
        }

        if (_newFixCharacterName == string.Empty)
            _newFixCharacterName = Dalamud.ClientState.LocalPlayer?.Name.ToString() ?? string.Empty;

        raii.PopFonts();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##NewFix", "Enter new Character (current: {", ref _newFixCharacterName, 64);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (raii.Begin(() => ImGui.BeginCombo("##NewFixDesignGroup", _newFixDesignGroup.Value.Name), ImGui.EndCombo))
        {
            foreach (var (id, group) in _plugin.FixedDesigns.JobGroups)
            {
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Selectable($"{group.Name}##NewFixDesignGroup", group.Name == _newFixDesignGroup.Value.Name))
                    _newFixDesignGroup = group;
            }

            raii.End();
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (!raii.Begin(() => ImGui.BeginCombo("##NewFixPath", _newFixDesignPath), ImGui.EndCombo))
            return;

        foreach (var design in _plugin.Designs.FileSystem.Root.AllLeaves(SortMode.Lexicographical).Cast<Design>())
        {
            var fullName = design.FullName();
            ImGui.SetNextItemWidth(-1);
            if (!ImGui.Selectable($"{fullName}##NewFixDesign", fullName == _newFixDesignPath))
                continue;

            _newFixDesignPath = fullName;
            _newFixDesign     = design;
        }
    }
}
