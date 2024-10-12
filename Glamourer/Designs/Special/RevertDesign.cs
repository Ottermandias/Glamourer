using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Special;

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

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks
    {
        get { yield return (this, ApplicationType.All, JobFlag.All); }
    }

    public void AddData(JObject jObj)
    { }

    public void ParseData(JObject jObj)
    { }

    public bool ChangeData(object data)
        => false;

    public bool ForcedRedraw
        => false;

    public bool ResetMaterials
        => false;
}
