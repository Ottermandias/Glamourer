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
    public static readonly IReadOnlyList<(ApplicationType, string)> Types = new[]
    {
        (ApplicationType.Customizations,
            "Apply all customization changes that are enabled in this design and that are valid in a fixed design and for the given race and gender."),
        (ApplicationType.Armor, "Apply all armor piece changes that are enabled in this design and that are valid in a fixed design."),
        (ApplicationType.Accessories, "Apply all accessory changes that are enabled in this design and that are valid in a fixed design."),
        (ApplicationType.GearCustomization, "Apply all dye and crest changes that are enabled in this design."),
        (ApplicationType.Weapons, "Apply all weapon changes that are enabled in this design and that are valid with the current weapon worn."),
    };

    public static (EquipFlag Equip, CustomizeFlag Customize, CrestFlag Crest, CustomizeParameterFlag Parameters, MetaFlag Meta) ApplyWhat(
        this ApplicationType type, DesignBase? design)
    {
        var equipFlags = (type.HasFlag(ApplicationType.Weapons) ? WeaponFlags : 0)
          | (type.HasFlag(ApplicationType.Armor) ? ArmorFlags : 0)
          | (type.HasFlag(ApplicationType.Accessories) ? AccessoryFlags : 0)
          | (type.HasFlag(ApplicationType.GearCustomization) ? StainFlags : 0);
        var customizeFlags = type.HasFlag(ApplicationType.Customizations) ? CustomizeFlagExtensions.All : 0;
        var parameterFlags = type.HasFlag(ApplicationType.Customizations) ? CustomizeParameterExtensions.All : 0;
        var crestFlag      = type.HasFlag(ApplicationType.GearCustomization) ? CrestExtensions.AllRelevant : 0;
        var metaFlag = (type.HasFlag(ApplicationType.Armor) ? MetaFlag.HatState | MetaFlag.VisorState : 0)
          | (type.HasFlag(ApplicationType.Weapons) ? MetaFlag.WeaponState : 0)
          | (type.HasFlag(ApplicationType.Customizations) ? MetaFlag.Wetness : 0);

        if (design == null)
            return (equipFlags, customizeFlags, crestFlag, parameterFlags, metaFlag);

        return (equipFlags & design!.ApplyEquip, customizeFlags & design.ApplyCustomize, crestFlag & design.ApplyCrest,
            parameterFlags & design.ApplyParameters, metaFlag & design.ApplyMeta);
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
