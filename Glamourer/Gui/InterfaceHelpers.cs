using System;
using System.Linq;
using Dalamud.Logging;
using Glamourer.Customization;
using Glamourer.Structs;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

//internal partial class Interface
//{
//    // Push the stain color to type and if it is too bright, turn the text color black.
//    // Return number of pushed styles.
//    private static int PushColor(Stain stain, ImGuiCol type = ImGuiCol.Button)
//    {
//        ImGui.PushStyleColor(type, stain.RgbaColor);
//        if (stain.Intensity > 127)
//        {
//            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF101010);
//            return 2;
//        }
//
//        return 1;
//    }
//

//

//
//
//    private enum DesignNameUse
//    {
//        SaveCurrent,
//        NewDesign,
//        DuplicateDesign,
//        NewFolder,
//        FromClipboard,
//    }
//
//    private void DrawDesignNamePopup(DesignNameUse use)
//    {
//        if (ImGui.BeginPopup($"{DesignNamePopupLabel}{use}"))
//        {
//            if (ImGui.InputText("##designName", ref _newDesignName, 64, ImGuiInputTextFlags.EnterReturnsTrue)
//             && _newDesignName.Any())
//            {
//                switch (use)
//                {
//                    case DesignNameUse.SaveCurrent:
//                        SaveNewDesign(ConditionalCopy(_currentSave, _holdShift, _holdCtrl));
//                        break;
//                    case DesignNameUse.NewDesign:
//                        var empty = new CharacterSave();
//                        empty.Load(CharacterCustomization.Default);
//                        empty.WriteCustomizations = false;
//                        SaveNewDesign(empty);
//                        break;
//                    case DesignNameUse.DuplicateDesign:
//                        SaveNewDesign(ConditionalCopy(_selection!.Data, _holdShift, _holdCtrl));
//                        break;
//                    case DesignNameUse.NewFolder:
//                        _designs.FileSystem
//                            .CreateAllFolders($"{_newDesignName}/a"); // Filename is just ignored, but all folders are created.
//                        break;
//                    case DesignNameUse.FromClipboard:
//                        try
//                        {
//                            var text = ImGui.GetClipboardText();
//                            var save = CharacterSave.FromString(text);
//                            SaveNewDesign(save);
//                        }
//                        catch (Exception e)
//                        {
//                            PluginLog.Information($"Could not save new Design from Clipboard:\n{e}");
//                        }
//
//                        break;
//                }
//
//                _newDesignName = string.Empty;
//                ImGui.CloseCurrentPopup();
//            }
//
//            if (_keyboardFocus)
//            {
//                ImGui.SetKeyboardFocusHere();
//                _keyboardFocus = false;
//            }
//
//            ImGui.EndPopup();
//        }
//    }
//
//    private void OpenDesignNamePopup(DesignNameUse use)
//    {
//        _newDesignName = string.Empty;
//        _keyboardFocus = true;
//        _holdCtrl      = ImGui.GetIO().KeyCtrl;
//        _holdShift     = ImGui.GetIO().KeyShift;
//        ImGui.OpenPopup($"{DesignNamePopupLabel}{use}");
//    }
//}
