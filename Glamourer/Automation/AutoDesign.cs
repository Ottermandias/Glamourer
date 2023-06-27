using System;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Interop.Structs;
using Glamourer.Structs;
using Newtonsoft.Json.Linq;

namespace Glamourer.Automation;

public class AutoDesign
{
    [Flags]
    public enum Type : uint
    {
        Armor          = 0x01,
        Customizations = 0x02,
        Meta           = 0x04,
        Weapons        = 0x08,
        Stains         = 0x10,
        Accessories    = 0x20,

        All = Armor | Accessories | Customizations | Meta | Weapons | Stains,
    }

    public Design   Design;
    public JobGroup Jobs;
    public Type     ApplicationType;

    public unsafe bool IsActive(Actor actor)
        => actor.IsCharacter && Jobs.Fits(actor.AsCharacter->CharacterData.ClassJob);

    public JObject Serialize()
        => new()
        {
            ["Design"]          = Design.Identifier.ToString(),
            ["ApplicationType"] = (uint)ApplicationType,
            ["Conditions"]      = CreateConditionObject(),
        };

    private JObject CreateConditionObject()
    {
        var ret = new JObject();
        if (Jobs.Id != 0)
            ret["JobGroup"] = Jobs.Id;
        return ret;
    }

    public (EquipFlag Equip, CustomizeFlag Customize, bool ApplyHat, bool ApplyVisor, bool ApplyWeapon, bool ApplyWet) ApplyWhat()
    {
        var equipFlags = (ApplicationType.HasFlag(Type.Weapons) ? WeaponFlags : 0)
          | (ApplicationType.HasFlag(Type.Armor) ? ArmorFlags : 0)
          | (ApplicationType.HasFlag(Type.Accessories) ? AccessoryFlags : 0)
          | (ApplicationType.HasFlag(Type.Stains) ? StainFlags : 0);
        var customizeFlags = ApplicationType.HasFlag(Type.Customizations) ? CustomizeFlagExtensions.All : 0;
        return (equipFlags & Design.ApplyEquip, customizeFlags & Design.ApplyCustomize,
            ApplicationType.HasFlag(Type.Armor) && Design.DoApplyHatVisible(),
            ApplicationType.HasFlag(Type.Armor) && Design.DoApplyVisorToggle(),
            ApplicationType.HasFlag(Type.Weapons) && Design.DoApplyWeaponVisible(),
            ApplicationType.HasFlag(Type.Customizations) && Design.DoApplyWetness());
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
