using Glamourer.Automation;
using Glamourer.Gui;
using Glamourer.Interop.Material;
using Glamourer.State;
using Luna;
using Newtonsoft.Json.Linq;
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
        => combo.QuickDesign as Design;

    public ref readonly DesignData GetDesignData(in DesignData baseRef)
    {
        if (combo.QuickDesign is not null)
            return ref combo.QuickDesign.GetDesignData(baseRef);

        return ref baseRef;
    }

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData()
        => combo.QuickDesign?.GetMaterialData() ?? [];

    public string SerializeName()
        => SerializedName;

    public StateSource AssociatedSource()
        => StateSource.Manual;

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks(bool newApplication)
        => combo.QuickDesign?.AllLinks(newApplication) ?? [];

    public void AddData(JObject jObj)
    { }

    public void ParseData(JObject jObj)
    { }

    public bool ChangeData(object data)
        => false;

    public bool ForcedRedraw
        => combo.QuickDesign?.ForcedRedraw ?? false;

    public bool ResetAdvancedDyes
        => combo.QuickDesign?.ResetAdvancedDyes ?? false;

    public bool ResetTemporarySettings
        => combo.QuickDesign?.ResetTemporarySettings ?? false;
}
