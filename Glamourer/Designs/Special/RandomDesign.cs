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

    public IReadOnlyList<IDesignPredicate> Predicates    { get; private set; } = [];
    public bool                            ResetOnRedraw { get; set; }         = false;

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
         && r.ResetOnRedraw == ResetOnRedraw
         && string.Equals(RandomPredicate.GeneratePredicateString(r.Predicates), RandomPredicate.GeneratePredicateString(Predicates),
                StringComparison.OrdinalIgnoreCase);

    public StateSource AssociatedSource()
        => StateSource.Manual;

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks(bool newApplication)
    {
        if (newApplication || ResetOnRedraw)
            _currentDesign = rng.Design(Predicates);
        else
            _currentDesign ??= rng.Design(Predicates);
        if (_currentDesign == null)
            yield break;

        foreach (var (link, type, jobs) in _currentDesign.AllLinks(newApplication))
            yield return (link, type, jobs);
    }

    public void AddData(JObject jObj)
    {
        jObj["Restrictions"]  = RandomPredicate.GeneratePredicateString(Predicates);
        jObj["ResetOnRedraw"] = ResetOnRedraw;
    }

    public void ParseData(JObject jObj)
    {
        var restrictions = jObj["Restrictions"]?.ToObject<string>() ?? string.Empty;
        Predicates    = RandomPredicate.GeneratePredicates(restrictions);
        ResetOnRedraw = jObj["ResetOnRedraw"]?.ToObject<bool>() ?? false;
    }

    public bool ChangeData(object data)
    {
        if (data is List<IDesignPredicate> predicates)
        {
            Predicates = predicates;
            return true;
        }

        if (data is bool resetOnRedraw)
        {
            ResetOnRedraw = resetOnRedraw;
            return true;
        }

        return false;
    }

    public bool ForcedRedraw
        => _currentDesign?.ForcedRedraw ?? false;

    public bool ResetAdvancedDyes
        => _currentDesign?.ResetAdvancedDyes ?? false;

    public bool ResetTemporarySettings
        => _currentDesign?.ResetTemporarySettings ?? false;
}
