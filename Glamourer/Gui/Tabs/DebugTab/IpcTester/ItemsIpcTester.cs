using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class ItemsIpcTester(DalamudPluginInterface pluginInterface) : IUiService
{
    private int            _gameObjectIndex;
    private string         _gameObjectName = string.Empty;
    private uint           _key;
    private ApplyFlag      _flags = ApplyFlagEx.DesignDefault;
    private CustomItemId   _customItemId;
    private StainId        _stainId;
    private EquipSlot      _slot = EquipSlot.Head;
    private GlamourerApiEc _lastError;

    public void Draw()
    {
        using var tree = ImRaii.TreeNode("Items");
        if (!tree)
            return;

        IpcTesterHelpers.IndexInput(ref _gameObjectIndex);
        IpcTesterHelpers.KeyInput(ref _key);
        DrawItemInput();
        IpcTesterHelpers.NameInput(ref _gameObjectName);
        IpcTesterHelpers.DrawFlagInput(ref _flags);
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error");
        ImGui.TextUnformatted(_lastError.ToString());

        IpcTesterHelpers.DrawIntro(SetItem.Label);
        if (ImGui.Button("Set##Idx"))
            _lastError = new SetItem(pluginInterface).Invoke(_gameObjectIndex, (ApiEquipSlot)_slot, _customItemId.Id, _stainId.Id, _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetItemName.Label);
        if (ImGui.Button("Set##Name"))
            _lastError = new SetItemName(pluginInterface).Invoke(_gameObjectName, (ApiEquipSlot)_slot, _customItemId.Id, _stainId.Id, _key,
                _flags);
    }

    private void DrawItemInput()
    {
        var tmp   = _customItemId.Id;
        var width = ImGui.GetContentRegionAvail().X / 2;
        ImGui.SetNextItemWidth(width);
        if (ImGuiUtil.InputUlong("Custom Item ID", ref tmp))
            _customItemId = tmp;
        EquipSlotCombo.Draw("Equip Slot", string.Empty, ref _slot, width);
        var value = (int)_stainId.Id;
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputInt("Stain ID", ref value, 1, 3))
        {
            value    = Math.Clamp(value, 0, byte.MaxValue);
            _stainId = (StainId)value;
        }
    }
}
