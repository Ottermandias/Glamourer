using System.Text.Json;
using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Penumbra.GameData.Enums;
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

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks(bool _, Predicate<DesignConditions>? _2)
    {
        yield return (this, ApplicationType.All, JobFlag.All);
    }

    public void AddData(Utf8JsonWriter _)
    { }

    public void ParseData(in JsonElement _)
    { }

    public bool ChangeData(object data)
        => false;

    public bool ForcedRedraw
        => false;

    public ModelCombinedSlots ResetAdvancedDyes
        => ModelCombinedSlotsExtensions.All;

    public bool ResetTemporarySettings
        => true;

    public ModelCombinedSlots RevertAdvancedDyes
        => 0; // Not sure whether All makes more sense here. 0 is backwards-compatible.
}
