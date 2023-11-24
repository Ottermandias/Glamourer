using System;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Interop.Structs;
using Glamourer.State;
using Glamourer.Structs;
using Newtonsoft.Json.Linq;

namespace Glamourer.Automation;

public class AutoDesign
{
    public const string RevertName = "Revert";

    [Flags]
    public enum Type : byte
    {
        Armor          = 0x01,
        Customizations = 0x02,
        Weapons        = 0x04,
        Stains         = 0x08,
        Accessories    = 0x10,

        All = Armor | Accessories | Customizations | Weapons | Stains,
    }

    public Design?  Design;
    public JobGroup Jobs;
    public Type     ApplicationType;
    public short    GearsetIndex = -1;

    public string Name(bool incognito)
        => Revert ? RevertName : incognito ? Design!.Incognito : Design!.Name.Text;

    public ref readonly DesignData GetDesignData(ActorState state)
        => ref Design == null ? ref state.BaseData : ref Design.DesignData;

    public bool Revert
        => Design == null;

    public AutoDesign Clone()
        => new()
        {
            Design          = Design,
            ApplicationType = ApplicationType,
            Jobs            = Jobs,
            GearsetIndex    = GearsetIndex,
        };

    public unsafe bool IsActive(Actor actor)
    {
        if (!actor.IsCharacter)
            return false;

        var ret = true;
        if (GearsetIndex < 0)
            ret &= Jobs.Fits(actor.AsCharacter->CharacterData.ClassJob);
        else
            ret &= AutoDesignApplier.CheckGearset(GearsetIndex);

        return ret;
    }

    public JObject Serialize()
        => new()
        {
            ["Design"]          = Design?.Identifier.ToString(),
            ["ApplicationType"] = (uint)ApplicationType,
            ["Conditions"]      = CreateConditionObject(),
        };

    private JObject CreateConditionObject()
    {
        var ret = new JObject
        {
            ["Gearset"]  = GearsetIndex,
            ["JobGroup"] = Jobs.Id,
        };

        return ret;
    }

    public (EquipFlag Equip, CustomizeFlag Customize, bool ApplyHat, bool ApplyVisor, bool ApplyWeapon, bool ApplyWet) ApplyWhat()
    {
        var equipFlags = (ApplicationType.HasFlag(Type.Weapons) ? WeaponFlags : 0)
          | (ApplicationType.HasFlag(Type.Armor) ? ArmorFlags : 0)
          | (ApplicationType.HasFlag(Type.Accessories) ? AccessoryFlags : 0)
          | (ApplicationType.HasFlag(Type.Stains) ? StainFlags : 0);
        var customizeFlags = ApplicationType.HasFlag(Type.Customizations) ? CustomizeFlagExtensions.All : 0;

        if (Revert)
            return (equipFlags, customizeFlags, ApplicationType.HasFlag(Type.Armor), ApplicationType.HasFlag(Type.Armor),
                ApplicationType.HasFlag(Type.Weapons), ApplicationType.HasFlag(Type.Customizations));

        return (equipFlags & Design!.ApplyEquip, customizeFlags & Design.ApplyCustomize,
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
