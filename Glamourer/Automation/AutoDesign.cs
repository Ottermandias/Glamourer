using System.Text.Json;
using Glamourer.Designs;
using Glamourer.Designs.Special;

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

    public void Serialize(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Design"u8, Design.SerializeName());
        j.WriteNumber("Type"u8, (uint)Type);
        Conditions.Data.Serialize(j, "Conditions"u8);
        Design.AddData(j);
        j.WriteEndObject();
    }

    public ApplicationCollection ApplyWhat()
        => Type.ApplyWhat(Design);
}
