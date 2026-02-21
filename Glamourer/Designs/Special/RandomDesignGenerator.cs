using Glamourer.Config;
using Luna;

namespace Glamourer.Designs.Special;

public class RandomDesignGenerator(DesignStorage designs, DesignFileSystem fileSystem, Configuration config) : IService
{
    private readonly Random                _rng        = new();
    private readonly WeakReference<Design> _lastDesign = new(null!, false);

    public Design? Design(IReadOnlyList<Design> localDesigns)
    {
        if (localDesigns.Count is 0)
            return null;

        var idx = _rng.Next(0, localDesigns.Count);
        if (localDesigns.Count is 1)
        {
            _lastDesign.SetTarget(localDesigns[idx]);
            return localDesigns[idx];
        }

        if (config.PreventRandomRepeats && _lastDesign.TryGetTarget(out var lastDesign))
            while (lastDesign == localDesigns[idx])
                idx = _rng.Next(0, localDesigns.Count);

        var design = localDesigns[idx];
        Glamourer.Log.Verbose($"[Random Design] Chose design {idx + 1} out of {localDesigns.Count}: {design.Incognito}.");
        _lastDesign.SetTarget(design);
        return design;
    }

    public Design? Design()
        => Design(designs);

    public Design? Design(IDesignPredicate predicate)
        => Design(predicate.Get(designs, fileSystem).ToList());

    public Design? Design(IReadOnlyList<IDesignPredicate> predicates)
    {
        return predicates.Count switch
        {
            0 => Design(),
            1 => Design(predicates[0]),
            _ => Design(IDesignPredicate.Get(predicates, designs, fileSystem).ToList()),
        };
    }

    public Design? Design(string restrictions)
        => Design(RandomPredicate.GeneratePredicates(restrictions));
}
