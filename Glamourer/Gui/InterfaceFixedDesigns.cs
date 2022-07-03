using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui;

internal partial class Interface
{
    private const string FixDragDropLabel = "##FixDragDrop";

    private List<string>? _fullPathCache;
    private string        _newFixCharacterName = string.Empty;
    private string        _newFixDesignPath    = string.Empty;
    private JobGroup?     _newFixDesignGroup;
    private Design?       _newFixDesign;
    private int           _fixDragDropIdx = -1;

    private static unsafe bool IsDropping()
        => ImGui.AcceptDragDropPayload(FixDragDropLabel).NativePtr != null;

    private void DrawFixedDesignsTab()
    {
        _newFixDesignGroup ??= Glamourer.FixedDesignManager.FixedDesigns.JobGroups[1];

        using var tabItem = ImRaii.TabItem("Fixed Designs");
        if (!tabItem)
        {
            _fullPathCache     = null;
            _newFixDesign      = null;
            _newFixDesignPath  = string.Empty;
            _newFixDesignGroup = Glamourer.FixedDesignManager.FixedDesigns.JobGroups[1];
            return;
        }

        _fullPathCache ??= Glamourer.FixedDesignManager.FixedDesigns.Data.Select(d => d.Design.FullName()).ToList();

        using var table       = ImRaii.Table("##FixedTable", 4);
        var       buttonWidth = 23.5f * ImGuiHelpers.GlobalScale;

        ImGui.TableSetupColumn("##DeleteColumn", ImGuiTableColumnFlags.WidthFixed, 2 * buttonWidth);
        ImGui.TableSetupColumn("Character",      ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Jobs",           ImGuiTableColumnFlags.WidthFixed, 175 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Design",         ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
        var xPos = 0f;

        using var style = new ImRaii.Style();
        using var font  = new ImRaii.Font();
        for (var i = 0; i < _fullPathCache.Count; ++i)
        {
            var path = _fullPathCache[i];
            var name = Glamourer.FixedDesignManager.FixedDesigns.Data[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            style.Push(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing / 2);
            font.Push(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}##{i}"))
            {
                _fullPathCache.RemoveAt(i--);
                Glamourer.FixedDesignManager.FixedDesigns.Remove(name);
                continue;
            }

            var tmp = name.Enabled;
            ImGui.SameLine();
            xPos = ImGui.GetCursorPosX();
            if (ImGui.Checkbox($"##Enabled{i}", ref tmp))
                if (tmp && Glamourer.FixedDesignManager.FixedDesigns.EnableDesign(name)
                 || !tmp && Glamourer.FixedDesignManager.FixedDesigns.DisableDesign(name))
                {
                    Glamourer.Config.FixedDesigns[i].Enabled = tmp;
                    Glamourer.Config.Save();
                }

            style.Pop();
            font.Pop();
            ImGui.TableNextColumn();
            ImGui.Selectable($"{name.Name}##Fix{i}");
            if (ImGui.BeginDragDropSource())
            {
                _fixDragDropIdx = i;
                ImGui.SetDragDropPayload("##FixDragDrop", IntPtr.Zero, 0);
                ImGui.Text($"Dragging {name.Name} ({path})...");
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                if (IsDropping() && _fixDragDropIdx >= 0)
                {
                    var d = Glamourer.FixedDesignManager.FixedDesigns.Data[_fixDragDropIdx];
                    Glamourer.FixedDesignManager.FixedDesigns.Move(d, i);
                    var p = _fullPathCache[_fixDragDropIdx];
                    _fullPathCache.RemoveAt(_fixDragDropIdx);
                    _fullPathCache.Insert(i, p);
                    _fixDragDropIdx = -1;
                }

                ImGui.EndDragDropTarget();
            }

            ImGui.TableNextColumn();
            ImGui.Text(Glamourer.FixedDesignManager.FixedDesigns.Data[i].Jobs.Name);
            ImGui.TableNextColumn();
            ImGui.Text(path);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        font.Push(UiBuilder.IconFont);

        ImGui.SetCursorPosX(xPos);
        if (_newFixDesign == null || _newFixCharacterName == string.Empty)
        {
            style.Push(ImGuiStyleVar.Alpha, 0.5f);
            ImGui.Button($"{FontAwesomeIcon.Plus.ToIconChar()}##NewFix");
            style.Pop();
        }
        else if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconChar()}##NewFix"))
        {
            _fullPathCache.Add(_newFixDesignPath);
            Glamourer.FixedDesignManager.FixedDesigns.Add(_newFixCharacterName, _newFixDesign, _newFixDesignGroup.Value, false);
            _newFixCharacterName = string.Empty;
            _newFixDesignPath    = string.Empty;
            _newFixDesign        = null;
            _newFixDesignGroup   = Glamourer.FixedDesignManager.FixedDesigns.JobGroups[1];
        }

        font.Pop();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##NewFix", "Enter new Character", ref _newFixCharacterName, 32);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        using var combo = ImRaii.Combo("##NewFixDesignGroup", _newFixDesignGroup.Value.Name);
        if (combo)
            foreach (var (id, group) in Glamourer.FixedDesignManager.FixedDesigns.JobGroups)
            {
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Selectable($"{group.Name}##NewFixDesignGroup", group.Name == _newFixDesignGroup.Value.Name))
                    _newFixDesignGroup = group;
            }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        using var combo2 = ImRaii.Combo("##NewFixPath", _newFixDesignPath);
        if (!combo2)
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
