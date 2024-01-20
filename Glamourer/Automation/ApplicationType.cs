using Glamourer.Designs;
using Glamourer.GameData;
using Penumbra.GameData.Enums;

namespace Glamourer.Automation;

[Flags]
public enum ApplicationType : byte
{
    Armor             = 0x01,
    Customizations    = 0x02,
    Weapons           = 0x04,
    GearCustomization = 0x08,
    Accessories       = 0x10,

    All = Armor | Accessories | Customizations | Weapons | GearCustomization,
}

public static class ApplicationTypeExtensions
{
    public static (EquipFlag Equip, CustomizeFlag Customize, CrestFlag Crest, CustomizeParameterFlag Parameters, bool ApplyHat, bool ApplyVisor,
        bool
        ApplyWeapon, bool ApplyWet) ApplyWhat(this ApplicationType type, DesignBase? design)
    {
        var equipFlags = (type.HasFlag(ApplicationType.Weapons) ? WeaponFlags : 0)
          | (type.HasFlag(ApplicationType.Armor) ? ArmorFlags : 0)
          | (type.HasFlag(ApplicationType.Accessories) ? AccessoryFlags : 0)
          | (type.HasFlag(ApplicationType.GearCustomization) ? StainFlags : 0);
        var customizeFlags = type.HasFlag(ApplicationType.Customizations) ? CustomizeFlagExtensions.All : 0;
        var parameterFlags = type.HasFlag(ApplicationType.Customizations) ? CustomizeParameterExtensions.All : 0;
        var crestFlag      = type.HasFlag(ApplicationType.GearCustomization) ? CrestExtensions.AllRelevant : 0;

        if (design == null)
            return (equipFlags, customizeFlags, crestFlag, parameterFlags, type.HasFlag(ApplicationType.Armor),
                type.HasFlag(ApplicationType.Armor),
                type.HasFlag(ApplicationType.Weapons), type.HasFlag(ApplicationType.Customizations));

        return (equipFlags & design!.ApplyEquip, customizeFlags & design.ApplyCustomize, crestFlag & design.ApplyCrest,
            parameterFlags & design.ApplyParameters,
            type.HasFlag(ApplicationType.Armor) && design.DoApplyHatVisible(),
            type.HasFlag(ApplicationType.Armor) && design.DoApplyVisorToggle(),
            type.HasFlag(ApplicationType.Weapons) && design.DoApplyWeaponVisible(),
            type.HasFlag(ApplicationType.Customizations) && design.DoApplyWetness());
    }

    public const EquipFlag WeaponFlags    = EquipFlag.Mainhand | EquipFlag.Offhand;
    public const EquipFlag ArmorFlags     = EquipFlag.Head | EquipFlag.Body | EquipFlag.Hands | EquipFlag.Legs | EquipFlag.Feet;
    public const EquipFlag AccessoryFlags = EquipFlag.Ears | EquipFlag.Neck | EquipFlag.Wrist | EquipFlag.RFinger | EquipFlag.LFinger;

    public const EquipFlag StainFlags = EquipFlag.MainhandStain
      | EquipFlag.OffhandStain
      | EquipFlag.HeadStain
      | EquipFlag.BodyStain
      | EquipFlag.HandsStain
      | EquipFlag.LegsStain
      | EquipFlag.FeetStain
      | EquipFlag.EarsStain
      | EquipFlag.NeckStain
      | EquipFlag.WristStain
      | EquipFlag.RFingerStain
      | EquipFlag.LFingerStain;
}