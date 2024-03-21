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
    private StainId                         _stainId;
    private EquipSlot                       _slot             = EquipSlot.Head;
    private string                          _gameObjectName   = string.Empty;
    private string                          _base64Apply      = string.Empty;
    private string                          _designIdentifier = string.Empty;
    private GlamourerIpc.GlamourerErrorCode _setItemEc;
    private GlamourerIpc.GlamourerErrorCode _setItemOnceEc;
    private GlamourerIpc.GlamourerErrorCode _setItemByActorNameEc;
    private GlamourerIpc.GlamourerErrorCode _setItemOnceByActorNameEc;

    public void Draw()
    {
        ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        ImGui.InputTextWithHint("##gameObject", "Character Name...",    ref _gameObjectName,   64);
        ImGui.InputTextWithHint("##base64",     "Design Base64...",     ref _base64Apply,      2047);
        ImGui.InputTextWithHint("##identifier", "Design identifier...", ref _designIdentifier, 36);
        DrawItemInput();
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
            .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex));
        if (base64 != null)
            ImGuiUtil.CopyOnClickSelectable(base64);
        else
            ImGui.TextUnformatted("Error");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomizationFromLockedCharacter);
        ImGui.TableNextColumn();
        var base64Locked = GlamourerIpc.GetAllCustomizationFromLockedCharacterSubscriber(_pluginInterface).Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);
        if (base64Locked != null)
            ImGuiUtil.CopyOnClickSelectable(base64Locked);
        else
            ImGui.TextUnformatted("Error");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevert);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##Name"))
            GlamourerIpc.RevertSubscriber(_pluginInterface).Invoke(_gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##Character"))
            GlamourerIpc.RevertCharacterSubscriber(_pluginInterface).Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAll);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##AllName"))
            GlamourerIpc.ApplyAllSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllOnce);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply Once##AllName"))
            GlamourerIpc.ApplyAllOnceSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##AllCharacter"))
            GlamourerIpc.ApplyAllToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllOnceToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply Once##AllCharacter"))
            GlamourerIpc.ApplyAllOnceToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipment);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##EquipName"))
            GlamourerIpc.ApplyOnlyEquipmentSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipmentToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##EquipCharacter"))
            GlamourerIpc.ApplyOnlyEquipmentToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomization);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##CustomizeName"))
            GlamourerIpc.ApplyOnlyCustomizationSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomizationToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##CustomizeCharacter"))
            GlamourerIpc.ApplyOnlyCustomizationToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuid);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##ByGuidName") && Guid.TryParse(_designIdentifier, out var guid1))
            GlamourerIpc.ApplyByGuidSubscriber(_pluginInterface).Invoke(guid1, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuidOnce);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply Once##ByGuidName") && Guid.TryParse(_designIdentifier, out var guid1Once))
            GlamourerIpc.ApplyByGuidOnceSubscriber(_pluginInterface).Invoke(guid1Once, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuidToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##ByGuidCharacter") && Guid.TryParse(_designIdentifier, out var guid2))
            GlamourerIpc.ApplyByGuidToCharacterSubscriber(_pluginInterface)
                .Invoke(guid2, _objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuidOnceToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply Once##ByGuidCharacter") && Guid.TryParse(_designIdentifier, out var guid2Once))
            GlamourerIpc.ApplyByGuidOnceToCharacterSubscriber(_pluginInterface)
                .Invoke(guid2Once, _objectManager.GetDalamudCharacter(_gameObjectIndex));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllLock);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply With Lock##CustomizeCharacter"))
            GlamourerIpc.ApplyAllToCharacterLockSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelUnlock);
        ImGui.TableNextColumn();
        if (ImGui.Button("Unlock##CustomizeCharacter"))
            GlamourerIpc.UnlockSubscriber(_pluginInterface)
                .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelUnlockAll);
        ImGui.TableNextColumn();
        if (ImGui.Button("Unlock All##CustomizeCharacter"))
            GlamourerIpc.UnlockAllSubscriber(_pluginInterface)
                .Invoke(1337);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertToAutomation);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##CustomizeCharacter"))
            GlamourerIpc.RevertToAutomationCharacterSubscriber(_pluginInterface)
                .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);


        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetDesignList);
        ImGui.TableNextColumn();
        var designList = GlamourerIpc.GetDesignListSubscriber(_pluginInterface)
            .Invoke();
        if (ImGui.Button($"Copy {designList.Length} Designs to Clipboard###CopyDesignList"))
            ImGui.SetClipboardText(string.Join("\n", designList));

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItem);
        ImGui.TableNextColumn();
        if (ImGui.Button("Set##SetItem"))
            _setItemEc = (GlamourerIpc.GlamourerErrorCode)GlamourerIpc.SetItemSubscriber(_pluginInterface)
                .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        if (_setItemEc != GlamourerIpc.GlamourerErrorCode.Success)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(_setItemEc.ToString());
        }

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemOnce);
        ImGui.TableNextColumn();
        if (ImGui.Button("Set Once##SetItem"))
            _setItemOnceEc = (GlamourerIpc.GlamourerErrorCode)GlamourerIpc.SetItemOnceSubscriber(_pluginInterface)
                .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        if (_setItemOnceEc != GlamourerIpc.GlamourerErrorCode.Success)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(_setItemOnceEc.ToString());
        }

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemByActorName);
        ImGui.TableNextColumn();
        if (ImGui.Button("Set##SetItemByActorName"))
            _setItemByActorNameEc = (GlamourerIpc.GlamourerErrorCode)GlamourerIpc.SetItemByActorNameSubscriber(_pluginInterface)
                .Invoke(_gameObjectName, (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        if (_setItemByActorNameEc != GlamourerIpc.GlamourerErrorCode.Success)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(_setItemByActorNameEc.ToString());
        }

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemOnceByActorName);
        ImGui.TableNextColumn();
        if (ImGui.Button("Set Once##SetItemByActorName"))
            _setItemOnceByActorNameEc = (GlamourerIpc.GlamourerErrorCode)GlamourerIpc.SetItemOnceByActorNameSubscriber(_pluginInterface)
                .Invoke(_gameObjectName, (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        if (_setItemOnceByActorNameEc != GlamourerIpc.GlamourerErrorCode.Success)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(_setItemOnceByActorNameEc.ToString());
        }
    }

    private void DrawItemInput()
    {
        var tmp = _customItemId.Id;
        if (ImGuiUtil.InputUlong("Custom Item ID", ref tmp))
            _customItemId = tmp;
        var width = ImGui.GetContentRegionAvail().X;
        EquipSlotCombo.Draw("Equip Slot", string.Empty, ref _slot);
        var value = (int)_stainId.Id;
        ImGui.SameLine();
        width -= ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputInt("Stain ID", ref value, 1, 3))
        {
            value    = Math.Clamp(value, 0, byte.MaxValue);
            _stainId = (StainId)value;
        }
    }
}
