using System;
using System.Linq;
using Glamourer.Services;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public class RestrictedGearPanel(ItemManager _items) : IDebugTabTree
{
    public string Label
        => "Restricted Gear Service";

    public bool Disabled
        => false;

    private int _setId;
    private int _secondaryId;
    private int _variant;

    public void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Resolve Model");
        DebugTab.DrawInputModelSet(false, ref _setId, ref _secondaryId, ref _variant);
        foreach (var race in Enum.GetValues<Race>().Skip(1))
        {
            ReadOnlySpan<Gender> genders = [Gender.Male, Gender.Female];
            foreach (var gender in genders)
            {
                foreach (var slot in EquipSlotExtensions.EqdpSlots)
                {
                    var (replaced, model) =
                        _items.RestrictedGear.ResolveRestricted(new CharacterArmor((PrimaryId)_setId, (Variant)_variant, 0), slot, race, gender);
                    if (replaced)
                        ImGui.TextUnformatted($"{race.ToName()} - {gender} - {slot.ToName()} resolves to {model}.");
                }
            }
        }
    }
}
