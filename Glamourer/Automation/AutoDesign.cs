using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Interop.Structs;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Automation;

public class AutoDesign
{
    public const string RevertName = "Revert";

    public Design?         Design;
    public JobGroup        Jobs;
    public ApplicationType Type;
    public short           GearsetIndex = -1;

    public string Name(bool incognito)
        => Revert ? RevertName : incognito ? Design!.Incognito : Design!.Name.Text;

    public ref readonly DesignData GetDesignData(ActorState state)
        => ref Design == null ? ref state.BaseData : ref Design.DesignData;

    public bool Revert
        => Design == null;

    public AutoDesign Clone()
        => new()
        {
            Design       = Design,
            Type         = Type,
            Jobs         = Jobs,
            GearsetIndex = GearsetIndex,
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
            ["Design"]     = Design?.Identifier.ToString(),
            ["Type"]       = (uint)Type,
            ["Conditions"] = CreateConditionObject(),
        };

    private JObject CreateConditionObject()
    {
        var ret = new JObject
        {
            ["Gearset"]  = GearsetIndex,
            ["JobGroup"] = Jobs.Id.Id,
        };

        return ret;
    }

    public (EquipFlag Equip, CustomizeFlag Customize, CrestFlag Crest, CustomizeParameterFlag Parameters, bool ApplyHat, bool ApplyVisor, bool
        ApplyWeapon, bool ApplyWet) ApplyWhat()
        => Type.ApplyWhat(Design);
}
