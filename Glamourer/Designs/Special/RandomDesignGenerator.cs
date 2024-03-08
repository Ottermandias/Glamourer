using OtterGui.Services;

namespace Glamourer.Designs.Special;

public class RandomDesignGenerator(DesignStorage designs, DesignFileSystem fileSystem) : IService
{
    private readonly Random _rng = new();

    public Design? Design(IReadOnlyList<Design> localDesigns)
    {
        if (localDesigns.Count == 0)
            return null;

        var idx = _rng.Next(0, localDesigns.Count);
        Glamourer.Log.Verbose($"[Random Design] Chose design {idx + 1} out of {localDesigns.Count}: {localDesigns[idx].Incognito}.");
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
