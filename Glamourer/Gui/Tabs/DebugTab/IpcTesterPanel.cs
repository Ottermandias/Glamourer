using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public class IpcTesterPanel(DalamudPluginInterface _pluginInterface, ObjectManager _objectManager) : IGameDataDrawer
{
    public string Label
        => "IPC Tester";

    public bool Disabled
        => false;

    private int                             _gameObjectIndex;
    private CustomItemId                    _customItemId;
    private EquipSlot                       _slot           = EquipSlot.Head;
    private string                          _gameObjectName = string.Empty;
    private string                          _base64Apply    = string.Empty;
    private GlamourerIpc.GlamourerErrorCode _setItemEc;
    private GlamourerIpc.GlamourerErrorCode _setItemByActorNameEc;

    public unsafe void Draw()
    {
        ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        ImGui.InputTextWithHint("##gameObject", "Character Name...", ref _gameObjectName, 64);
        ImGui.InputTextWithHint("##base64",     "Design Base64...",  ref _base64Apply,    2047);
        DrawItemIdInput();
        using var table = ImRaii.Table("##ipc", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApiVersions);
        var (major, minor) = GlamourerIpc.ApiVersionsSubscriber(_pluginInterface).Invoke();
        ImGuiUtil.DrawTableColumn($"({major}, {minor})");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomization);
        ImGui.TableNextColumn();
        var base64 = GlamourerIpc.GetAllCustomizationSubscriber(_pluginInterface).Invoke(_gameObjectName);
        if (base64 != null)
            ImGuiUtil.CopyOnClickSelectable(base64);
        else
            ImGui.TextUnformatted("Error");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomizationFromCharacter);
        ImGui.TableNextColumn();
        base64 = GlamourerIpc.GetAllCustomizationFromCharacterSubscriber(_pluginInterface)
            .Invoke(_objectManager.Objects[_gameObjectIndex] as Character);
        if (base64 != null)
            ImGuiUtil.CopyOnClickSelectable(base64);
        else
            ImGui.TextUnformatted("Error");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevert);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##Name"))
            GlamourerIpc.RevertSubscriber(_pluginInterface).Invoke(_gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##Character"))
            GlamourerIpc.RevertCharacterSubscriber(_pluginInterface).Invoke(_objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAll);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##AllName"))
            GlamourerIpc.ApplyAllSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##AllCharacter"))
            GlamourerIpc.ApplyAllToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipment);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##EquipName"))
            GlamourerIpc.ApplyOnlyEquipmentSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipmentToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##EquipCharacter"))
            GlamourerIpc.ApplyOnlyEquipmentToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomization);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##CustomizeName"))
            GlamourerIpc.ApplyOnlyCustomizationSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomizationToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##CustomizeCharacter"))
            GlamourerIpc.ApplyOnlyCustomizationToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelUnlock);
        ImGui.TableNextColumn();
        if (ImGui.Button("Unlock##CustomizeCharacter"))
            GlamourerIpc.UnlockSubscriber(_pluginInterface)
                .Invoke(_objectManager.Objects[_gameObjectIndex] as Character, 1337);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertToAutomation);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##CustomizeCharacter"))
            GlamourerIpc.RevertToAutomationCharacterSubscriber(_pluginInterface)
                .Invoke(_objectManager.Objects[_gameObjectIndex] as Character, 1337);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItem);
        ImGui.TableNextColumn();
        if (ImGui.Button("Set##SetItem"))
            _setItemEc = (GlamourerIpc.GlamourerErrorCode)GlamourerIpc.SetItemSubscriber(_pluginInterface)
                .Invoke(_objectManager.Objects[_gameObjectIndex] as Character, (byte)_slot, _customItemId.Id, 1337);
        if (_setItemEc != GlamourerIpc.GlamourerErrorCode.Success)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(_setItemEc.ToString());
        }

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemByActorName);
        ImGui.TableNextColumn();
        if (ImGui.Button("Set##SetItemByActorName"))
            _setItemByActorNameEc = (GlamourerIpc.GlamourerErrorCode)GlamourerIpc.SetItemByActorNameSubscriber(_pluginInterface)
                .Invoke(_gameObjectName, (byte)_slot, _customItemId.Id, 1337);
        if (_setItemByActorNameEc != GlamourerIpc.GlamourerErrorCode.Success)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(_setItemByActorNameEc.ToString());
        }
    }

    private void DrawItemIdInput()
    {
        var tmp = _customItemId.Id;
        if (ImGuiUtil.InputUlong("Custom Item ID", ref tmp))
            _customItemId = (CustomItemId)tmp;
        ImGui.SameLine();
        EquipSlotCombo.Draw("Equip Slot", string.Empty, ref _slot);
    }
}
