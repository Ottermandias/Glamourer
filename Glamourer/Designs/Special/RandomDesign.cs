using Glamourer.Automation;
using Glamourer.Interop.Material;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Special;

public class RandomDesign(RandomDesignGenerator rng) : IDesignStandIn
{
    public const string  SerializedName = "//Random";
    public const string  ResolvedName   = "Random";
    private      Design? _currentDesign;

    public IReadOnlyList<IDesignPredicate> Predicates { get; private set; } = [];

    public string ResolveName(bool _)
        => ResolvedName;

    public ref readonly DesignData GetDesignData(in DesignData baseRef)
    {
        _currentDesign ??= rng.Design(Predicates);
        if (_currentDesign == null)
            return ref baseRef;

        return ref _currentDesign.GetDesignDataRef();
    }

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData()
    {
        _currentDesign ??= rng.Design(Predicates);
        if (_currentDesign == null)
            return [];

        return _currentDesign.Materials;
    }

    public string SerializeName()
        => SerializedName;

    public bool Equals(IDesignStandIn? other)
        => other is RandomDesign r
         && string.Equals(RandomPredicate.GeneratePredicateString(r.Predicates), RandomPredicate.GeneratePredicateString(Predicates),
                StringComparison.OrdinalIgnoreCase);

    public StateSource AssociatedSource()
        => StateSource.Manual;

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks
    {
        get
        {
            _currentDesign = rng.Design(Predicates);
            if (_currentDesign == null)
                yield break;

            foreach (var (link, type, jobs) in _currentDesign.AllLinks)
                yield return (link, type, jobs);
        }
    }

    public void AddData(JObject jObj)
    {
        jObj["Restrictions"] = RandomPredicate.GeneratePredicateString(Predicates);
    }

    public void ParseData(JObject jObj)
    {
        var restrictions = jObj["Restrictions"]?.ToObject<string>() ?? string.Empty;
        Predicates = RandomPredicate.GeneratePredicates(restrictions);
    }

    public bool ChangeData(object data)
    {
        if (data is not List<IDesignPredicate> predicates)
            return false;

        Predicates = predicates;
        return true;
    }

    public bool ForcedRedraw
        => false;

    public bool ResetAdvancedDyes
        => false;
}
