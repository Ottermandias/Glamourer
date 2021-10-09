using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;

namespace Glamourer.Gui
{
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

            raii.Begin(() => ImGui.BeginTable("##FixedTable", 4), ImGui.EndTable);

            var buttonWidth = 23.5f * ImGuiHelpers.GlobalScale;


            ImGui.TableSetupColumn("##DeleteColumn", ImGuiTableColumnFlags.WidthFixed, 2 * buttonWidth);
            ImGui.TableSetupColumn("Character",      ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Jobs",           ImGuiTableColumnFlags.WidthFixed, 175 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Design",         ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            var xPos = 0f;
            for (var i = 0; i < _fullPathCache.Count; ++i)
            {
                var path = _fullPathCache[i];
                var name = _plugin.FixedDesigns.Data[i];

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                raii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing / 2);
                raii.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconChar()}##{i}"))
                {
                    _fullPathCache.RemoveAt(i);
                    _plugin.FixedDesigns.Remove(name);
                }

                var tmp = name.Enabled;
                ImGui.SameLine();
                xPos = ImGui.GetCursorPosX();
                if (ImGui.Checkbox($"##Enabled{i}", ref tmp))
                    if (tmp && _plugin.FixedDesigns.EnableDesign(name)
                     || !tmp && _plugin.FixedDesigns.DisableDesign(name))
                        Glamourer.Config.Save();
                raii.PopStyles();
                raii.PopFonts();
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
                        var d = _plugin.FixedDesigns.Data[_fixDragDropIdx];
                        _plugin.FixedDesigns.Move(d, i);
                        var p = _fullPathCache[_fixDragDropIdx];
                        _fullPathCache.RemoveAt(_fixDragDropIdx);
                        _fullPathCache.Insert(i, p);
                        _fixDragDropIdx = -1;
                    }
                    ImGui.EndDragDropTarget();
                }

                ImGui.TableNextColumn();
                ImGui.Text(_plugin.FixedDesigns.Data[i].Jobs.Name);
                ImGui.TableNextColumn();
                ImGui.Text(path);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            raii.PushFont(UiBuilder.IconFont);

            ImGui.SetCursorPosX(xPos);
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

            raii.PopFonts();
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##NewFix", "Enter new Character", ref _newFixCharacterName, 32);
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
}
