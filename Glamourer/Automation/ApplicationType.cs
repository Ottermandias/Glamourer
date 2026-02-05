using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.GameData;
using ImSharp;
using Luna.Generators;
using Penumbra.GameData.Enums;

namespace Glamourer.Automation;

[Flags]
[TooltipEnum]
public enum ApplicationType : byte
{
    [Tooltip("Apply all armor piece changes that are enabled in this design and that are valid in a fixed design.")]
    Armor = 0x01,

    [Tooltip(
        "Apply all customization changes that are enabled in this design and that are valid in a fixed design and for the given race and gender.")]
    Customizations = 0x02,

    [Tooltip("Apply all weapon changes that are enabled in this design and that are valid with the current weapon worn.")]
    Weapons = 0x04,

    [Tooltip("Apply all dye and crest changes that are enabled in this design.")]
    GearCustomization = 0x08,

    [Tooltip("Apply all accessory changes that are enabled in this design and that are valid in a fixed design.")]
    Accessories = 0x10,

    All = Armor | Accessories | Customizations | Weapons | GearCustomization,
}

public static partial class ApplicationTypeExtensions
{
    public static readonly IReadOnlyList<(ApplicationType, StringU8)> Types =
    [
        (ApplicationType.Customizations, ApplicationType.Customizations.Tooltip()),
        (ApplicationType.Armor, ApplicationType.Armor.Tooltip()),
        (ApplicationType.Accessories, ApplicationType.Accessories.Tooltip()),
        (ApplicationType.GearCustomization, ApplicationType.GearCustomization.Tooltip()),
        (ApplicationType.Weapons, ApplicationType.Weapons.Tooltip()),
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
        var metaFlags = (type.HasFlag(ApplicationType.Armor) ? MetaFlag.HatState | MetaFlag.VisorState | MetaFlag.EarState : 0)
          | (type.HasFlag(ApplicationType.Weapons) ? MetaFlag.WeaponState : 0)
          | (type.HasFlag(ApplicationType.Customizations) ? MetaFlag.Wetness : 0);
        var bonusFlags = type.HasFlag(ApplicationType.Armor) ? BonusExtensions.All : 0;

        return new ApplicationCollection(equipFlags, bonusFlags, customizeFlags, crestFlags, parameterFlags, metaFlags);
    }

    public static ApplicationCollection ApplyWhat(this ApplicationType type, IDesignStandIn designStandIn)
    {
        if (designStandIn is not DesignBase design)
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
