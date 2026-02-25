using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using ImSharp;
using Luna;
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
        using var tree = Im.Tree.Node("Items"u8);
        if (!tree)
            return;

        IpcTesterHelpers.IndexInput(ref _gameObjectIndex);
        IpcTesterHelpers.KeyInput(ref _key);
        DrawItemInput();
        IpcTesterHelpers.NameInput(ref _gameObjectName);
        IpcTesterHelpers.DrawFlagInput(ref _flags);
        using var table = Im.Table.Begin("##table"u8, 2, TableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error"u8);
        Im.Text($"{_lastError}");

        IpcTesterHelpers.DrawIntro(SetItem.LabelU8);
        if (Im.Button("Set##Idx"u8))
            _lastError = new SetItem(pluginInterface).Invoke(_gameObjectIndex, (ApiEquipSlot)_slot, _customItemId.Id, [_stainId.Id], _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetItemName.LabelU8);
        if (Im.Button("Set##Name"u8))
            _lastError = new SetItemName(pluginInterface).Invoke(_gameObjectName, (ApiEquipSlot)_slot, _customItemId.Id, [_stainId.Id], _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetBonusItem.LabelU8);
        if (Im.Button("Set##BonusIdx"u8))
            _lastError = new SetBonusItem(pluginInterface).Invoke(_gameObjectIndex, ToApi(_bonusSlot), _customItemId.Id, _key,
                _flags);

        IpcTesterHelpers.DrawIntro(SetBonusItemName.LabelU8);
        if (Im.Button("Set##BonusName"u8))
            _lastError = new SetBonusItemName(pluginInterface).Invoke(_gameObjectName, ToApi(_bonusSlot), _customItemId.Id, _key,
                _flags);
    }

    private void DrawItemInput()
    {
        var tmp   = _customItemId.Id;
        var width = Im.ContentRegion.Available.X / 2;
        Im.Item.SetNextWidth(width);
        if (Im.Input.Scalar("Custom Item ID"u8, ref tmp))
            _customItemId = tmp;
        EquipSlotCombo.Draw("Equip Slot"u8, StringU8.Empty, ref _slot, width);
        BonusSlotCombo.Draw("Bonus Slot"u8, StringU8.Empty, ref _bonusSlot, width);
        var value = (int)_stainId.Id;
        Im.Item.SetNextWidth(width);
        if (Im.Input.Scalar("Stain ID"u8, ref value, 1, 3))
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
