using Glamourer.Automation;

namespace Glamourer.Designs.Links;

public record struct DesignLink(Design Link, ApplicationType Type, DesignConditions Conditions);

public readonly record struct LinkData(Guid Identity, ApplicationType Type, DesignConditionData Conditions, LinkOrder Order)
{
    public override string ToString()
        => Identity.ToString();
}

public enum LinkOrder : byte
{
    Self,
    After,
    Before,
    None,
};
