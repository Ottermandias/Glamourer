using System.Text.Json;
using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public interface IDesignStandIn : IEquatable<IDesignStandIn>
{
    public              string     ResolveName(bool incognito);
    public ref readonly DesignData GetDesignData(in DesignData baseRef);

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData();

    public string      SerializeName();
    public StateSource AssociatedSource();

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks(bool newApplication,
        Predicate<DesignConditions>? condition);

    public void AddData(Utf8JsonWriter jObj);

    public void ParseData(in JsonElement jObj);

    public bool ChangeData(object data);

    public bool ForcedRedraw { get; }

    public ModelCombinedSlots ResetAdvancedDyes      { get; }
    public ModelCombinedSlots RevertAdvancedDyes     { get; }
    public bool               ResetTemporarySettings { get; }
}
