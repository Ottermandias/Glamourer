using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class ItemsIpcTester(IDalamudPluginInterface pluginInterface) : IUiService
{
    private int            _gameObjectIndex;
    private string         _gameObjectName = string.Empty;
    private uint           _key;
    private ApplyFlag      _flags = ApplyFlagEx.DesignDefault;
    private CustomItemId   _customItemId;
    private StainId        _stainId;
    private EquipSlot      _slot      = EquipSlot.Head;
    private BonusItemFlag  _bonusSlot = BonusItemFlag.Glasses;
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
        using var table = Im.Table.Begin("##table"u8, 2, TableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error");
        ImGui.TextUnformatted(_lastError.ToString());

        IpcTesterHelpers.DrawIntro(SetItem.Label);
        if (ImGui.Button("Set##Idx"))
            _lastError = new SetItem(pluginInterface).Invoke(_gameObjectIndex, (ApiEquipSlot)_slot, _customItemId.Id, [_stainId.Id], _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetItemName.Label);
        if (ImGui.Button("Set##Name"))
            _lastError = new SetItemName(pluginInterface).Invoke(_gameObjectName, (ApiEquipSlot)_slot, _customItemId.Id, [_stainId.Id], _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetBonusItem.Label);
        if (ImGui.Button("Set##BonusIdx"))
            _lastError = new SetBonusItem(pluginInterface).Invoke(_gameObjectIndex, ToApi(_bonusSlot), _customItemId.Id, _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetBonusItemName.Label);
        if (ImGui.Button("Set##BonusName"))
            _lastError = new SetBonusItemName(pluginInterface).Invoke(_gameObjectName, ToApi(_bonusSlot), _customItemId.Id, _key,
                _flags);
    }

    private void DrawItemInput()
    {
        var tmp   = _customItemId.Id;
        var width = Im.ContentRegion.Available.X / 2;
        ImGui.SetNextItemWidth(width);
        if (ImGuiUtil.InputUlong("Custom Item ID", ref tmp))
            _customItemId = tmp;
        EquipSlotCombo.Draw("Equip Slot"u8, StringU8.Empty, ref _slot, width);
        BonusSlotCombo.Draw("Bonus Slot"u8, StringU8.Empty, ref _bonusSlot, width);
        var value = (int)_stainId.Id;
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputInt("Stain ID", ref value, 1, 3))
        {
            value    = Math.Clamp(value, 0, byte.MaxValue);
            _stainId = (StainId)value;
        }
    }

    private static ApiBonusSlot ToApi(BonusItemFlag slot)
        => slot switch
        {
            BonusItemFlag.Glasses => ApiBonusSlot.Glasses,
            _                     => ApiBonusSlot.Unknown,
        };
}
