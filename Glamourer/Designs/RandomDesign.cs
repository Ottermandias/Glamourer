using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Newtonsoft.Json.Linq;

namespace Glamourer.Designs;

public class RandomDesign(RandomDesignGenerator rng) : IDesignStandIn
{
    public const string  SerializedName = "//Random";
    public const string  ResolvedName   = "Random";
    private      Design? _currentDesign;

    public string Restrictions { get; internal set; } = string.Empty;

    public string ResolveName(bool _)
        => ResolvedName;

    public ref readonly DesignData GetDesignData(in DesignData baseRef)
    {
        _currentDesign ??= rng.Design(Restrictions);
        if (_currentDesign == null)
            return ref baseRef;

        return ref _currentDesign.GetDesignDataRef();
    }

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData()
    {
        _currentDesign ??= rng.Design(Restrictions);
        if (_currentDesign == null)
            return [];

        return _currentDesign.Materials;
    }

    public string SerializeName()
        => SerializedName;

    public bool Equals(IDesignStandIn? other)
        => other is RandomDesign r && string.Equals(r.Restrictions, Restrictions, StringComparison.OrdinalIgnoreCase);

    public StateSource AssociatedSource()
        => StateSource.Manual;

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags)> AllLinks
    {
        get
        {
            _currentDesign = rng.Design(Restrictions);
            if (_currentDesign == null)
                yield break;

            foreach (var (link, type) in _currentDesign.AllLinks)
                yield return (link, type);
        }
    }

    public void AddData(JObject jObj)
    {
        jObj["Restrictions"] = Restrictions;
    }

    public void ParseData(JObject jObj)
    {
        var restrictions = jObj["Restrictions"]?.ToObject<string>() ?? string.Empty;
        Restrictions = restrictions;
    }
}
