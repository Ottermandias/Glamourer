using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Glamourer.Api;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class IpcTesterPanel(
    DalamudPluginInterface pluginInterface,
    DesignIpcTester designs,
    ItemsIpcTester items,
    StateIpcTester state,
    IFramework framework) : IGameDataDrawer
{
    public string Label
        => "IPC Tester";

    public bool Disabled
        => false;

    private DateTime _lastUpdate;
    private bool     _subscribed = false;

    public void Draw()
    {
        try
        {
            _lastUpdate = framework.LastUpdateUTC.AddSeconds(1);
            Subscribe();
            ImGui.TextUnformatted(ApiVersion.Label);
            var (major, minor) = new ApiVersion(pluginInterface).Invoke();
            ImGui.SameLine();
            ImGui.TextUnformatted($"({major}.{minor:D4})");

            designs.Draw();
            items.Draw();
            state.Draw();
        }
        catch (Exception e)
        {
            Glamourer.Log.Error($"Error during IPC Tests:\n{e}");
        }
        //ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        //ImGui.InputTextWithHint("##gameObject", "Character Name...",    ref _gameObjectName,   64);
        //ImGui.InputTextWithHint("##base64",     "Design Base64...",     ref _base64Apply,      2047);
        //ImGui.InputTextWithHint("##identifier", "Design identifier...", ref _designIdentifier, 36);
        //DrawItemInput();
        //using var table = ImRaii.Table("##ipc", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        //if (!table)
        //    return;
        //
        //ImGuiUtil.DrawTableColumn();
        //ImGui.TableNextColumn();
        //var base64 = GlamourerIpc.GetAllCustomizationSubscriber(_pluginInterface).Invoke(_gameObjectName);
        //if (base64 != null)
        //    ImGuiUtil.CopyOnClickSelectable(base64);
        //else
        //    ImGui.TextUnformatted("Error");
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomizationFromCharacter);
        //ImGui.TableNextColumn();
        //base64 = GlamourerIpc.GetAllCustomizationFromCharacterSubscriber(_pluginInterface)
        //    .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex));
        //if (base64 != null)
        //    ImGuiUtil.CopyOnClickSelectable(base64);
        //else
        //    ImGui.TextUnformatted("Error");
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomizationFromLockedCharacter);
        //ImGui.TableNextColumn();
        //var base64Locked = GlamourerIpc.GetAllCustomizationFromLockedCharacterSubscriber(_pluginInterface)
        //    .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);
        //if (base64Locked != null)
        //    ImGuiUtil.CopyOnClickSelectable(base64Locked);
        //else
        //    ImGui.TextUnformatted("Error");
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevert);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Revert##Name"))
        //    GlamourerIpc.RevertSubscriber(_pluginInterface).Invoke(_gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Revert##Character"))
        //    GlamourerIpc.RevertCharacterSubscriber(_pluginInterface).Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAll);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##AllName"))
        //    GlamourerIpc.ApplyAllSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllOnce);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply Once##AllName"))
        //    GlamourerIpc.ApplyAllOnceSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllToCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##AllCharacter"))
        //    GlamourerIpc.ApplyAllToCharacterSubscriber(_pluginInterface)
        //        .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllOnceToCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply Once##AllCharacter"))
        //    GlamourerIpc.ApplyAllOnceToCharacterSubscriber(_pluginInterface)
        //        .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipment);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##EquipName"))
        //    GlamourerIpc.ApplyOnlyEquipmentSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipmentToCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##EquipCharacter"))
        //    GlamourerIpc.ApplyOnlyEquipmentToCharacterSubscriber(_pluginInterface)
        //        .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomization);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##CustomizeName"))
        //    GlamourerIpc.ApplyOnlyCustomizationSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomizationToCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##CustomizeCharacter"))
        //    GlamourerIpc.ApplyOnlyCustomizationToCharacterSubscriber(_pluginInterface)
        //        .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuid);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##ByGuidName") && Guid.TryParse(_designIdentifier, out var guid1))
        //    GlamourerIpc.ApplyByGuidSubscriber(_pluginInterface).Invoke(guid1, _gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuidOnce);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply Once##ByGuidName") && Guid.TryParse(_designIdentifier, out var guid1Once))
        //    GlamourerIpc.ApplyByGuidOnceSubscriber(_pluginInterface).Invoke(guid1Once, _gameObjectName);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuidToCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply##ByGuidCharacter") && Guid.TryParse(_designIdentifier, out var guid2))
        //    GlamourerIpc.ApplyByGuidToCharacterSubscriber(_pluginInterface)
        //        .Invoke(guid2, _objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyByGuidOnceToCharacter);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply Once##ByGuidCharacter") && Guid.TryParse(_designIdentifier, out var guid2Once))
        //    GlamourerIpc.ApplyByGuidOnceToCharacterSubscriber(_pluginInterface)
        //        .Invoke(guid2Once, _objectManager.GetDalamudCharacter(_gameObjectIndex));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllLock);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Apply With Lock##CustomizeCharacter"))
        //    GlamourerIpc.ApplyAllToCharacterLockSubscriber(_pluginInterface)
        //        .Invoke(_base64Apply, _objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelUnlock);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Unlock##CustomizeCharacter"))
        //    GlamourerIpc.UnlockSubscriber(_pluginInterface)
        //        .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelUnlockAll);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Unlock All##CustomizeCharacter"))
        //    GlamourerIpc.UnlockAllSubscriber(_pluginInterface)
        //        .Invoke(1337);
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertToAutomation);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Revert##CustomizeCharacter"))
        //    GlamourerIpc.RevertToAutomationCharacterSubscriber(_pluginInterface)
        //        .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), 1337);
        //
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetDesignList);
        //ImGui.TableNextColumn();
        //var designList = GlamourerIpc.GetDesignListSubscriber(_pluginInterface)
        //    .Invoke();
        //if (ImGui.Button($"Copy {designList.Length} Designs to Clipboard###CopyDesignList"))
        //    ImGui.SetClipboardText(string.Join("\n", designList));
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItem);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Set##SetItem"))
        //    _setItemEc = (GlamourerApiEc)GlamourerIpc.SetItemSubscriber(_pluginInterface)
        //        .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        //if (_setItemEc != GlamourerApiEc.Success)
        //{
        //    ImGui.SameLine();
        //    ImGui.TextUnformatted(_setItemEc.ToString());
        //}
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemOnce);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Set Once##SetItem"))
        //    _setItemOnceEc = (GlamourerApiEc)GlamourerIpc.SetItemOnceSubscriber(_pluginInterface)
        //        .Invoke(_objectManager.GetDalamudCharacter(_gameObjectIndex), (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        //if (_setItemOnceEc != GlamourerApiEc.Success)
        //{
        //    ImGui.SameLine();
        //    ImGui.TextUnformatted(_setItemOnceEc.ToString());
        //}
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemByActorName);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Set##SetItemByActorName"))
        //    _setItemByActorNameEc = (GlamourerApiEc)GlamourerIpc.SetItemByActorNameSubscriber(_pluginInterface)
        //        .Invoke(_gameObjectName, (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        //if (_setItemByActorNameEc != GlamourerApiEc.Success)
        //{
        //    ImGui.SameLine();
        //    ImGui.TextUnformatted(_setItemByActorNameEc.ToString());
        //}
        //
        //ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelSetItemOnceByActorName);
        //ImGui.TableNextColumn();
        //if (ImGui.Button("Set Once##SetItemByActorName"))
        //    _setItemOnceByActorNameEc = (GlamourerApiEc)GlamourerIpc.SetItemOnceByActorNameSubscriber(_pluginInterface)
        //        .Invoke(_gameObjectName, (byte)_slot, _customItemId.Id, _stainId.Id, 1337);
        //if (_setItemOnceByActorNameEc != GlamourerApiEc.Success)
        //{
        //    ImGui.SameLine();
        //    ImGui.TextUnformatted(_setItemOnceByActorNameEc.ToString());
        //}
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        Glamourer.Log.Debug("[IPCTester] Subscribed to IPC events for IPC tester.");
        state.GPoseChanged.Enable();
        state.StateChanged.Enable();
        framework.Update += CheckUnsubscribe;
        _subscribed      =  true;
    }

    private void CheckUnsubscribe(IFramework framework1)
    {
        if (_lastUpdate > framework.LastUpdateUTC)
            return;

        Unsubscribe();
        framework.Update -= CheckUnsubscribe;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        Glamourer.Log.Debug("[IPCTester] Unsubscribed from IPC events for IPC tester.");
        _subscribed = false;
        state.GPoseChanged.Disable();
        state.StateChanged.Disable();
    }
}
