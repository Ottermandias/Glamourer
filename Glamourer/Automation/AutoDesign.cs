using Glamourer.Designs;
using Glamourer.Designs.Special;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Interop;

namespace Glamourer.Automation;

public class AutoDesign
{
    public IDesignStandIn   Design = new RevertDesign();
    public ApplicationType  Type;
    public DesignConditions Conditions;

    public AutoDesign Clone()
        => new()
        {
            Design     = Design,
            Type       = Type,
            Conditions = Conditions,
        };

    public JObject Serialize()
    {
        var ret = new JObject
        {
            ["Design"]     = Design.SerializeName(),
            ["Type"]       = (uint)Type,
            ["Conditions"] = Conditions.Data.Serialize(),
        };
        Design.AddData(ret);
        return ret;
    }

    public ApplicationCollection ApplyWhat()
        => Type.ApplyWhat(Design);
}
