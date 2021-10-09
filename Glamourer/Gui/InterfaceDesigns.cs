using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private int _totalObject;

        private bool    _inDesignMode;
        private Design? _selection;
        private string  _newChildName = string.Empty;

        private void DrawDesignSelector()
        {
            _totalObject = 0;
            ImGui.BeginGroup();
            if (ImGui.BeginChild("##selector", new Vector2(SelectorWidth * ImGui.GetIO().FontGlobalScale, -ImGui.GetFrameHeight() - 1), true))
            {
                DrawFolderContent(_designs.FileSystem.Root, Glamourer.Config.FoldersFirst ? SortMode.FoldersFirst : SortMode.Lexicographical);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            DrawDesignSelectorButtons();
            ImGui.EndGroup();
        }

        private void DrawPasteClipboardButton()
        {
            if (_selection!.Data.WriteProtected)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);

            ImGui.PushFont(UiBuilder.IconFont);
            var applyButton = ImGui.Button(FontAwesomeIcon.Paste.ToIconString());
            ImGui.PopFont();
            if (_selection!.Data.WriteProtected)
                ImGui.PopStyleVar();

            ImGuiCustom.HoverTooltip("Overwrite with customization code from clipboard.");

            if (_selection!.Data.WriteProtected || !applyButton)
                return;

            var text = ImGui.GetClipboardText();
            if (!text.Any())
                return;

            try
            {
                _selection!.Data = CharacterSave.FromString(text);
                _designs.SaveToFile();
            }
            catch (Exception e)
            {
                PluginLog.Information($"{e}");
            }
        }

        private void DrawNewFolderButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.FolderPlus.ToIconString(), Vector2.UnitX * SelectorWidth / 5))
                OpenDesignNamePopup(DesignNameUse.NewFolder);
            ImGui.PopFont();
            ImGuiCustom.HoverTooltip("Create a new, empty Folder.");

            DrawDesignNamePopup(DesignNameUse.NewFolder);
        }

        private void DrawNewDesignButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), Vector2.UnitX * SelectorWidth / 5))
                OpenDesignNamePopup(DesignNameUse.NewDesign);
            ImGui.PopFont();
            ImGuiCustom.HoverTooltip("Create a new, empty Design.");

            DrawDesignNamePopup(DesignNameUse.NewDesign);
        }

        private void DrawClipboardDesignButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Paste.ToIconString(), Vector2.UnitX * SelectorWidth / 5))
                OpenDesignNamePopup(DesignNameUse.FromClipboard);
            ImGui.PopFont();
            ImGuiCustom.HoverTooltip("Create a new design from the customization string in your clipboard.");

            DrawDesignNamePopup(DesignNameUse.FromClipboard);
        }

        private void DrawDeleteDesignButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var style = _selection == null;
            if (style)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), Vector2.UnitX * SelectorWidth / 5) && _selection != null)
            {
                _designs.DeleteAllChildren(_selection, false);
                _selection = null;
            }

            ImGui.PopFont();
            if (style)
                ImGui.PopStyleVar();
            ImGuiCustom.HoverTooltip("Delete the currently selected Design.");
        }

        private void DrawDuplicateDesignButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (_selection == null)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            if (ImGui.Button(FontAwesomeIcon.Clone.ToIconString(), Vector2.UnitX * SelectorWidth / 5) && _selection != null)
                OpenDesignNamePopup(DesignNameUse.DuplicateDesign);
            ImGui.PopFont();
            if (_selection == null)
                ImGui.PopStyleVar();
            ImGuiCustom.HoverTooltip("Clone the currently selected Design.");

            DrawDesignNamePopup(DesignNameUse.DuplicateDesign);
        }

        private void DrawDesignSelectorButtons()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0f);

            DrawNewFolderButton();
            ImGui.SameLine();
            DrawNewDesignButton();
            ImGui.SameLine();
            DrawClipboardDesignButton();
            ImGui.SameLine();
            DrawDuplicateDesignButton();
            ImGui.SameLine();
            DrawDeleteDesignButton();
        }

        private void DrawDesignHeaderButtons()
        {
            DrawCopyClipboardButton(_selection!.Data);
            ImGui.SameLine();
            DrawPasteClipboardButton();
            ImGui.SameLine();
            DrawApplyToPlayerButton(_selection!.Data);
            if (!_inGPose)
            {
                ImGui.SameLine();
                DrawApplyToTargetButton(_selection!.Data);
            }

            ImGui.SameLine();
            DrawCheckbox("Write Protected", _selection!.Data.WriteProtected, v => _selection!.Data.WriteProtected = v, false);
        }

        private void DrawDesignPanel()
        {
            if (ImGui.BeginChild("##details", -Vector2.One * 0.001f, true))
            {
                DrawDesignHeaderButtons();
                var data = _selection!.Data;
                var prot = _selection!.Data.WriteProtected;
                if (prot)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.8f);
                    data = data.Copy();
                }

                DrawGeneralSettings(data, prot);
                var mask = data.WriteEquipment;
                if (DrawEquip(data.Equipment, ref mask) && !prot)
                {
                    data.WriteEquipment = mask;
                    _designs.SaveToFile();
                }

                if (DrawCustomization(ref data.Customizations) && !prot)
                    _designs.SaveToFile();

                if (DrawMiscellaneous(data, null) && !prot)
                    _designs.SaveToFile();

                if (prot)
                    ImGui.PopStyleVar();

                ImGui.EndChild();
            }
        }

        private void DrawSaves()
        {
            using var raii = new ImGuiRaii();
            raii.PushStyle(ImGuiStyleVar.IndentSpacing, 12.5f * ImGui.GetIO().FontGlobalScale);
            _inDesignMode = raii.Begin(() => ImGui.BeginTabItem("Designs"), ImGui.EndTabItem);
            if (!_inDesignMode)
                return;

            DrawDesignSelector();

            if (_selection != null)
            {
                ImGui.SameLine();
                DrawDesignPanel();
            }
        }

        private void DrawCheckbox(string label, bool value, Action<bool> setter, bool prot)
        {
            var tmp = value;
            if (ImGui.Checkbox(label, ref tmp) && tmp != value)
            {
                setter(tmp);
                if (!prot)
                    _designs.SaveToFile();
            }
        }

        private void DrawGeneralSettings(CharacterSave data, bool prot)
        {
            ImGui.BeginGroup();
            DrawCheckbox("Apply Customizations", data.WriteCustomizations, v => data.WriteCustomizations = v, prot);
            DrawCheckbox("Write Weapon State",   data.SetWeaponState,      v => data.SetWeaponState      = v, prot);
            ImGui.EndGroup();
            ImGui.SameLine();
            ImGui.BeginGroup();
            DrawCheckbox("Write Hat State",   data.SetHatState,   v => data.SetHatState   = v, prot);
            DrawCheckbox("Write Visor State", data.SetVisorState, v => data.SetVisorState = v, prot);
            ImGui.EndGroup();
        }

        private void RenameChildInput(IFileSystemBase child)
        {
            ImGui.SetNextItemWidth(150);
            if (!ImGui.InputTextWithHint("##fsNewName", "Rename...", ref _newChildName, 64,
                ImGuiInputTextFlags.EnterReturnsTrue))
                return;

            if (_newChildName.Any() && _newChildName != child.Name)
                try
                {
                    var oldPath = child.FullName();
                    if (_designs.FileSystem.Rename(child, _newChildName))
                        _designs.UpdateAllChildren(oldPath, child);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not rename {child.Name} to {_newChildName}:\n{e}");
                }
            else if (child is Folder f)
                try
                {
                    var oldPath = child.FullName();
                    if (_designs.FileSystem.Merge(f, f.Parent, true))
                        _designs.UpdateAllChildren(oldPath, f.Parent);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not merge folder {child.Name} into parent:\n{e}");
                }

            _newChildName = string.Empty;
        }

        private void ContextMenu(IFileSystemBase child)
        {
            var label = $"##fsPopup{child.FullName()}";
            if (ImGui.BeginPopup(label))
            {
                if (ImGui.MenuItem("Delete"))
                    _designs.DeleteAllChildren(child, false);

                RenameChildInput(child);

                if (child is Design d && ImGui.MenuItem("Copy to Clipboard"))
                    ImGui.SetClipboardText(d.Data.ToBase64());

                ImGui.EndPopup();
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _newChildName = child.Name;
                ImGui.OpenPopup(label);
            }
        }

        private static uint GetDesignColor(CharacterSave save)
        {
            const uint white = 0xFFFFFFFF;
            const uint grey  = 0xFF808080;
            if (!Glamourer.Config.ColorDesigns)
                return white;

            var changesStates = save.SetHatState || save.SetVisorState || save.SetWeaponState || save.IsWet || save.Alpha != 1.0f;
            if (save.WriteCustomizations)
                if (save.WriteEquipment != CharacterEquipMask.None)
                    return white;
                else
                    return changesStates ? white : Glamourer.Config.CustomizationColor;

            if (save.WriteEquipment != CharacterEquipMask.None)
                return changesStates ? white : Glamourer.Config.EquipmentColor;

            return changesStates ? Glamourer.Config.StateColor : grey;
        }

        private void DrawFolderContent(Folder folder, SortMode mode)
        {
            foreach (var child in folder.AllChildren(mode).ToArray())
            {
                if (child.IsFolder(out var subFolder))
                {
                    var treeNode = ImGui.TreeNodeEx($"{subFolder.Name}##{_totalObject}");
                    DrawOrnaments(child);

                    if (treeNode)
                    {
                        DrawFolderContent(subFolder, mode);
                        ImGui.TreePop();
                    }
                    else
                    {
                        _totalObject += subFolder.TotalDescendantLeaves();
                    }
                }
                else
                {
                    if (child is not Design d)
                        continue;

                    ++_totalObject;
                    var color = GetDesignColor(d.Data);
                    using var raii = new ImGuiRaii()
                        .PushColor(ImGuiCol.Text, color);

                    var selected = ImGui.Selectable($"{child.Name}##{_totalObject}", ReferenceEquals(child, _selection));
                    raii.PopColors();
                    DrawOrnaments(child);

                    if (Glamourer.Config.ShowLocks && d.Data.WriteProtected)
                    {
                        ImGui.SameLine();
                        raii.PushFont(UiBuilder.IconFont)
                            .PushColor(ImGuiCol.Text, color);
                        ImGui.Text(FontAwesomeIcon.Lock.ToIconString());
                    }

                    if (selected)
                        _selection = d;
                }
            }
        }

        private void DrawOrnaments(IFileSystemBase child)
        {
            FileSystemImGui.DragDropSource(child);
            if (FileSystemImGui.DragDropTarget(_designs.FileSystem, child, out var oldPath, out var draggedFolder))
                _designs.UpdateAllChildren(oldPath, draggedFolder!);
            ContextMenu(child);
        }
    }
}
