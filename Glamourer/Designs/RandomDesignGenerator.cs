using OtterGui;
using OtterGui.Services;
using System;

namespace Glamourer.Designs;

public class RandomDesignGenerator(DesignStorage designs, DesignFileSystem fileSystem) : IService
{
    private readonly Random _rng = new();

    public Design? Design(IReadOnlyList<Design> localDesigns)
    {
        if (localDesigns.Count == 0)
            return null;

        var idx = _rng.Next(0, localDesigns.Count - 1);
        Glamourer.Log.Verbose($"[Random Design] Chose design {idx} out of {localDesigns.Count}: {localDesigns[idx].Incognito}.");
        return localDesigns[idx];
    }

    public Design? Design()
        => Design(designs);

    public Design? Design(string restrictions)
    {
        if (restrictions.Length == 0)
            return Design(designs);

        List<Func<string, string, string, bool>> predicates = [];

        switch (restrictions[0])
        {
            case '{':
                var end = restrictions.IndexOf('}');
                if (end == -1)
                    throw new ArgumentException($"The restriction group '{restrictions}' is not properly terminated.");

                restrictions = restrictions[1..end];
                var split = restrictions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var item in split.Distinct())
                    predicates.Add(item[0] == '/' ? CreatePredicateSlash(item) : CreatePredicate(item));
                break;
            case '/':
                predicates.Add(CreatePredicateSlash(restrictions));
                break;
            default:
                predicates.Add(CreatePredicate(restrictions));
                break;
        }

        if (predicates.Count == 1)
        {
            var p = predicates[0];
            return Design(designs.Select(Transform).Where(t => p(t.NameLower, t.Identifier, t.PathLower)).Select(t => t.Design).ToList());
        }

        return Design(designs.Select(Transform).Where(t => predicates.Any(p => p(t.NameLower, t.Identifier, t.PathLower))).Select(t => t.Design)
            .ToList());

        (Design Design, string NameLower, string Identifier, string PathLower) Transform(Design design)
        {
            var name       = design.Name.Lower;
            var identifier = design.Identifier.ToString();
            var path       = fileSystem.FindLeaf(design, out var leaf) ? leaf.FullName().ToLowerInvariant() : string.Empty;
            return (design, name, identifier, path);
        }

        Func<string, string, string, bool> CreatePredicate(string input)
        {
            var value = input.ToLowerInvariant();
            return (string nameLower, string identifier, string pathLower) =>
            {
                if (nameLower.Contains(value))
                    return true;
                if (identifier.Contains(value))
                    return true;
                if (pathLower.Contains(value))
                    return true;

                return false;
            };
        }

        Func<string, string, string, bool> CreatePredicateSlash(string input)
        {
            var value = input[1..].ToLowerInvariant();
            return (string nameLower, string identifier, string pathLower) 
                => pathLower.StartsWith(value);
        }
    }
}
