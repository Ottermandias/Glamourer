using Glamourer.Automation;
using Glamourer.Gui;
using Glamourer.Interop.Material;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using OtterGui.Services;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Special;

public class QuickSelectedDesign(QuickDesignCombo combo) : IDesignStandIn, IService
{
    public const string SerializedName = "//QuickSelection";
    public const string ResolvedName   = "Quick Design Bar Selection";

    public bool Equals(IDesignStandIn? other)
        => other is QuickSelectedDesign;

    public string ResolveName(bool incognito)
        => ResolvedName;

    public Design? CurrentDesign
        => combo.Design as Design;

    public ref readonly DesignData GetDesignData(in DesignData baseRef)
    {
        if (combo.Design != null)
            return ref combo.Design.GetDesignData(baseRef);

        return ref baseRef;
    }

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData()
        => combo.Design?.GetMaterialData() ?? [];

    public string SerializeName()
        => SerializedName;

    public StateSource AssociatedSource()
        => StateSource.Manual;

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks
        => combo.Design?.AllLinks ?? [];

    public void AddData(JObject jObj)
    { }

    public void ParseData(JObject jObj)
    { }

    public bool ChangeData(object data)
        => false;

    public bool ForcedRedraw
        => combo.Design?.ForcedRedraw ?? false;

    public bool ResetAdvancedDyes
        => combo.Design?.ResetAdvancedDyes ?? false;
}
