using OtterGui.Classes;

namespace Glamourer.Designs.Special;

public interface IDesignPredicate
{
    public bool Invoke(Design design, string lowerName, string identifier, string lowerPath);

    public bool Invoke((Design Design, string LowerName, string Identifier, string LowerPath) args)
        => Invoke(args.Design, args.LowerName, args.Identifier, args.LowerPath);

    public IEnumerable<Design> Get(IEnumerable<Design> designs, DesignFileSystem fileSystem)
        => designs.Select(d => Transform(d, fileSystem))
            .Where(Invoke)
            .Select(t => t.Design);

    public static IEnumerable<Design> Get(IReadOnlyList<IDesignPredicate> predicates, IEnumerable<Design> designs, DesignFileSystem fileSystem)
        => predicates.Count > 0
            ? designs.Select(d => Transform(d, fileSystem))
                .Where(t => predicates.Any(p => p.Invoke(t)))
                .Select(t => t.Design)
            : designs;

    private static (Design Design, string LowerName, string Identifier, string LowerPath) Transform(Design d, DesignFileSystem fs)
        => (d, d.Name.Lower, d.Identifier.ToString(), fs.TryGetValue(d, out var l) ? l.FullName().ToLowerInvariant() : string.Empty);
}

public static class RandomPredicate
{
    public readonly struct StartsWith(string value) : IDesignPredicate
    {
        public LowerString Value { get; } = value;

        public bool Invoke(Design design, string lowerName, string identifier, string lowerPath)
            => lowerPath.StartsWith(Value.Lower);

        public override string ToString()
            => $"/{Value.Text}";
    }

    public readonly struct Contains(string value) : IDesignPredicate
    {
        public LowerString Value { get; } = value;

        public bool Invoke(Design design, string lowerName, string identifier, string lowerPath)
        {
            if (lowerName.Contains(Value.Lower))
                return true;
            if (identifier.Contains(Value.Lower))
                return true;
            if (lowerPath.Contains(Value.Lower))
                return true;

            return false;
        }

        public override string ToString()
            => Value.Text;
    }

    public readonly struct Exact(Exact.Type type, string value) : IDesignPredicate
    {
        public enum Type : byte
        {
            Name,
            Path,
            Identifier,
            Tag,
            Color,
        }

        public Type        Which { get; } = type;
        public LowerString Value { get; } = value;

        public bool Invoke(Design design, string lowerName, string identifier, string lowerPath)
            => Which switch
            {
                Type.Name       => lowerName == Value.Lower,
                Type.Path       => lowerPath == Value.Lower,
                Type.Identifier => identifier == Value.Lower,
                Type.Tag        => IsContained(Value, design.Tags),
                Type.Color      => design.Color == Value,
                _               => false,
            };

        private static bool IsContained(LowerString value, IEnumerable<string> data)
            => data.Any(t => t == value);

        public override string ToString()
            => $"\"{Which switch { Type.Name => 'n', Type.Identifier => 'i', Type.Path => 'p', Type.Tag => 't', Type.Color => 'c', _ => '?' }}?{Value.Text}\"";
    }

    public static IDesignPredicate CreateSinglePredicate(string restriction)
    {
        switch (restriction[0])
        {
            case '/': return new StartsWith(restriction[1..]);
            case '"':
                var end = restriction.IndexOf('"', 1);
                if (end < 3)
                    return new Contains(restriction);

                switch (restriction[1], restriction[2])
                {
                    case ('n', '?'):
                    case ('N', '?'):
                        return new Exact(Exact.Type.Name, restriction[3..end]);
                    case ('p', '?'):
                    case ('P', '?'):
                        return new Exact(Exact.Type.Path, restriction[3..end]);
                    case ('i', '?'):
                    case ('I', '?'):
                        return new Exact(Exact.Type.Identifier, restriction[3..end]);
                    case ('t', '?'):
                    case ('T', '?'):
                        return new Exact(Exact.Type.Tag, restriction[3..end]);
                    case ('c', '?'):
                    case ('C', '?'):
                        return new Exact(Exact.Type.Color, restriction[3..end]);
                    default: return new Contains(restriction);
                }
            default: return new Contains(restriction);
        }
    }

    public static List<IDesignPredicate> GeneratePredicates(string restrictions)
    {
        if (restrictions.Length == 0)
            return [];

        List<IDesignPredicate> predicates = new(1);
        if (restrictions[0] is '{')
        {
            var end = restrictions.IndexOf('}');
            if (end == -1)
            {
                predicates.Add(CreateSinglePredicate(restrictions));
            }
            else
            {
                restrictions = restrictions[1..end];
                var split = restrictions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                predicates.AddRange(split.Distinct().Select(CreateSinglePredicate));
            }
        }
        else
        {
            predicates.Add(CreateSinglePredicate(restrictions));
        }

        return predicates;
    }

    public static string GeneratePredicateString(IReadOnlyCollection<IDesignPredicate> predicates)
    {
        if (predicates.Count == 0)
            return string.Empty;
        if (predicates.Count == 1)
            return predicates.First()!.ToString()!;

        return $"{{{string.Join("; ", predicates)}}}";
    }
}
