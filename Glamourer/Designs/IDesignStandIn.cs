using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public interface IDesignStandIn : IEquatable<IDesignStandIn>
{
    public              string     ResolveName(bool incognito);
    public ref readonly DesignData GetDesignData(in DesignData baseRef);

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData();

    public string      SerializeName();
    public StateSource AssociatedSource();

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks { get; }

    public void AddData(JObject jObj);

    public void ParseData(JObject jObj);

    public bool ChangeData(object data);
}
