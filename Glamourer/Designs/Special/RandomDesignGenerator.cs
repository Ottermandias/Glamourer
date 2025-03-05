using OtterGui;
using OtterGui.Services;

namespace Glamourer.Designs.Special;

public class RandomDesignGenerator(DesignStorage designs, DesignFileSystem fileSystem, Configuration config) : IService
{
    private readonly Random _rng = new();
    private WeakReference<Design?> _lastDesign = new(null, false);

    public Design? Design(IReadOnlyList<Design> localDesigns)
    {
        if (localDesigns.Count == 0)
            return null;

        int idx;
        do
            idx = _rng.Next(0, localDesigns.Count);
        while (config.PreventRandomRepeats && localDesigns.Count > 1 && _lastDesign.TryGetTarget(out var lastDesign) && lastDesign == localDesigns[idx]);
        
        Glamourer.Log.Verbose($"[Random Design] Chose design {idx + 1} out of {localDesigns.Count}: {localDesigns[idx].Incognito}.");
        _lastDesign.SetTarget(localDesigns[idx]);
        return localDesigns[idx];
    }

    public Design? Design()
        => Design(designs);

    public Design? Design(IDesignPredicate predicate)
        => Design(predicate.Get(designs, fileSystem).ToList());

    public Design? Design(IReadOnlyList<IDesignPredicate> predicates)
    {
        if (predicates.Count == 0)
            return Design();
        if (predicates.Count == 1)
            return Design(predicates[0]);

        return Design(IDesignPredicate.Get(predicates, designs, fileSystem).ToList());
    }

    public Design? Design(string restrictions)
        => Design(RandomPredicate.GeneratePredicates(restrictions));
}
