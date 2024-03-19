using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.GameData;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Automation;

public class AutoDesign
{
    public IDesignStandIn  Design = new RevertDesign();
    public JobGroup        Jobs;
    public ApplicationType Type;
    public short           GearsetIndex = -1;

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
    {
        var ret = new JObject
        {
            ["Design"]     = Design.SerializeName(),
            ["Type"]       = (uint)Type,
            ["Conditions"] = CreateConditionObject(),
        };
        Design.AddData(ret);
        return ret;
    }

    private JObject CreateConditionObject()
    {
        var ret = new JObject
        {
            ["Gearset"]  = GearsetIndex,
            ["JobGroup"] = Jobs.Id.Id,
        };

        return ret;
    }

    public (EquipFlag Equip, CustomizeFlag Customize, CrestFlag Crest, CustomizeParameterFlag Parameters, MetaFlag Meta) ApplyWhat()
        => Type.ApplyWhat(Design);
}
