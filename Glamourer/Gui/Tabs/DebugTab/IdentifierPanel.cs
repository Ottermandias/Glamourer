using System.Linq;
using Dalamud.Interface.Utility;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public class IdentifierPanel(ItemManager _items, GamePathParser _gamePathParser) : IDebugTabTree
{
    public string Label
        => "Identifier Service";

    public bool Disabled
        => !_items.ObjectIdentification.Awaiter.IsCompletedSuccessfully;

    private string _gamePath = string.Empty;
    private int    _setId;
    private int    _secondaryId;
    private int    _variant;

    public void Draw()
    {
        static void Text(string text)
        {
            if (text.Length > 0)
                ImGui.TextUnformatted(text);
        }

        ImGui.TextUnformatted("Parse Game Path");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##gamePath", "Enter game path...", ref _gamePath, 256);
        var fileInfo = _gamePathParser.GetFileInfo(_gamePath);
        ImGui.TextUnformatted(
            $"{fileInfo.ObjectType} {fileInfo.EquipSlot} {fileInfo.PrimaryId} {fileInfo.SecondaryId} {fileInfo.Variant} {fileInfo.BodySlot} {fileInfo.CustomizationType}");
        Text(string.Join("\n", _items.ObjectIdentification.Identify(_gamePath).Keys));

        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Identify Model");
        ImGui.SameLine();
        DebugTab.DrawInputModelSet(true, ref _setId, ref _secondaryId, ref _variant);

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var identified = _items.Identify(slot, (PrimaryId)_setId, 0, (Variant)_variant);
            Text(identified.Name);
            ImGuiUtil.HoverTooltip(string.Join("\n",
                _items.ObjectIdentification.Identify((PrimaryId)_setId, 0, (Variant)_variant, slot)
                    .Select(i => $"{i.Name} {i.Id} {i.ItemId} {i.IconId}")));
        }

        var weapon = _items.Identify(EquipSlot.MainHand, (PrimaryId)_setId, (SecondaryId)_secondaryId, (Variant)_variant);
        Text(weapon.Name);
        ImGuiUtil.HoverTooltip(string.Join("\n",
            _items.ObjectIdentification.Identify((PrimaryId)_setId, (SecondaryId)_secondaryId, (Variant)_variant, EquipSlot.MainHand)));
    }
}
