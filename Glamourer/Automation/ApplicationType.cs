using Glamourer.Api.Enums;
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
    public static readonly IReadOnlyList<(ApplicationType, string)> Types =
    [
        (ApplicationType.Customizations,
            "Apply all customization changes that are enabled in this design and that are valid in a fixed design and for the given race and gender."),
        (ApplicationType.Armor, "Apply all armor piece changes that are enabled in this design and that are valid in a fixed design."),
        (ApplicationType.Accessories, "Apply all accessory changes that are enabled in this design and that are valid in a fixed design."),
        (ApplicationType.GearCustomization, "Apply all dye and crest changes that are enabled in this design."),
        (ApplicationType.Weapons, "Apply all weapon changes that are enabled in this design and that are valid with the current weapon worn."),
    ];

    public static ApplicationCollection Collection(this ApplicationType type)
    {
        var equipFlags = (type.HasFlag(ApplicationType.Weapons) ? WeaponFlags : 0)
          | (type.HasFlag(ApplicationType.Armor) ? ArmorFlags : 0)
          | (type.HasFlag(ApplicationType.Accessories) ? AccessoryFlags : 0)
          | (type.HasFlag(ApplicationType.GearCustomization) ? StainFlags : 0);
        var customizeFlags = type.HasFlag(ApplicationType.Customizations) ? CustomizeFlagExtensions.All : 0;
        var parameterFlags = type.HasFlag(ApplicationType.Customizations) ? CustomizeParameterExtensions.All : 0;
        var crestFlags     = type.HasFlag(ApplicationType.GearCustomization) ? CrestExtensions.AllRelevant : 0;
        var metaFlags = (type.HasFlag(ApplicationType.Armor) ? MetaFlag.HatState | MetaFlag.VisorState : 0)
          | (type.HasFlag(ApplicationType.Weapons) ? MetaFlag.WeaponState : 0)
          | (type.HasFlag(ApplicationType.Customizations) ? MetaFlag.Wetness : 0);
        var bonusFlags = type.HasFlag(ApplicationType.Armor) ? BonusExtensions.All : 0;

        return new ApplicationCollection(equipFlags, bonusFlags, customizeFlags, crestFlags, parameterFlags, metaFlags);
    }

    public static ApplicationCollection ApplyWhat(this ApplicationType type, IDesignStandIn designStandIn)
    {
        if(designStandIn is not DesignBase design)
            return type.Collection();
        var ret = type.Collection().Restrict(design.Application); 
        ret.CustomizeRaw = ret.CustomizeRaw.FixApplication(design.CustomizeSet);
        return ret;
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
