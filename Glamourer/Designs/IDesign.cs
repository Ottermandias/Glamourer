using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public interface IDesign
{
    public uint GetModelId();
    public bool SetModelId(uint modelId);

    public EquipItem GetEquipItem(EquipSlot slot);
    public bool      SetEquipItem(EquipItem item);

    public StainId GetStain(EquipSlot slot);
    public bool    SetStain(EquipSlot slot, StainId stain);

    public CustomizeValue GetCustomizeValue(CustomizeIndex type);
    public bool           SetCustomizeValue(CustomizeIndex type);

    public bool DoApplyEquip(EquipSlot slot);
    public bool DoApplyStain(EquipSlot slot);
    public bool DoApplyCustomize(CustomizeIndex index);

    public bool SetApplyEquip(EquipSlot slot, bool value);
    public bool SetApplyStain(EquipSlot slot, bool value);
    public bool SetApplyCustomize(CustomizeIndex slot, bool value);

    public bool IsWet();
    public bool SetIsWet(bool value);

    public bool IsHatVisible();
    public bool DoApplyHatVisible();
    public bool SetHatVisible(bool value);
    public bool SetApplyHatVisible(bool value);

    public bool IsVisorToggled();
    public bool DoApplyVisorToggle();
    public bool SetVisorToggle(bool value);
    public bool SetApplyVisorToggle(bool value);

    public bool IsWeaponVisible();
    public bool DoApplyWeaponVisible();
    public bool SetWeaponVisible(bool value);
    public bool SetApplyWeaponVisible(bool value);
}
