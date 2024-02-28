using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Newtonsoft.Json.Linq;

namespace Glamourer.Designs;

public class RevertDesign : IDesignStandIn
{
    public const string SerializedName = "//Revert";
    public const string ResolvedName   = "Revert";

    public string ResolveName(bool _)
        => ResolvedName;

    public ref readonly DesignData GetDesignData(in DesignData baseRef)
        => ref baseRef;

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData()
        => [];

    public string SerializeName()
        => SerializedName;

    public bool Equals(IDesignStandIn? other)
        => other is RevertDesign;

    public StateSource AssociatedSource()
        => StateSource.Game;

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags)> AllLinks
    {
        get { yield return (this, ApplicationType.All); }
    }

    public void AddData(JObject jObj)
    { }

    public void ParseData(JObject jObj)
    { }
}
