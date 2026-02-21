namespace Glamourer.Designs.Special;

public interface IDesignPredicate
{
    bool Invoke(Design design, string name, string identifier, string path);

    bool Invoke((Design Design, string Name, string Identifier, string Path) args)
        => Invoke(args.Design, args.Name, args.Identifier, args.Path);

    IEnumerable<Design> Get(IEnumerable<Design> designs)
        => designs.Select(Transform)
            .Where(Invoke)
            .Select(t => t.Design);

    static IEnumerable<Design> Get(IReadOnlyList<IDesignPredicate> predicates, IEnumerable<Design> designs)
        => predicates.Count > 0
            ? designs.Select(Transform)
                .Where(t => predicates.Any(p => p.Invoke(t)))
                .Select(t => t.Design)
            : designs;

    private static (Design Design, string Name, string Identifier, string Path) Transform(Design d)
        => (d, d.Name, d.Identifier.ToString(), d.Path.CurrentPath);
}

public static class RandomPredicate
{
    public readonly struct StartsWith(string value) : IDesignPredicate
    {
        public string Value { get; } = value;

        public bool Invoke(Design design, string name, string identifier, string path)
            => path.StartsWith(Value, StringComparison.OrdinalIgnoreCase);

        public override string ToString()
            => $"/{Value}";
    }

    public readonly struct Contains(string value) : IDesignPredicate
    {
        public string Value { get; } = value;

        public bool Invoke(Design design, string name, string identifier, string path)
        {
            if (name.Contains(Value, StringComparison.OrdinalIgnoreCase))
                return true;
            if (identifier.Contains(Value, StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.Contains(Value, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public override string ToString()
            => Value;
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

        public Type   Which { get; } = type;
        public string Value { get; } = value;

        public bool Invoke(Design design, string name, string identifier, string path)
            => Which switch
            {
                Type.Name       => string.Equals(name,       Value, StringComparison.OrdinalIgnoreCase),
                Type.Path       => string.Equals(path,       Value, StringComparison.OrdinalIgnoreCase),
                Type.Identifier => string.Equals(identifier, Value, StringComparison.OrdinalIgnoreCase),
                Type.Tag        => IsContained(Value, design.Tags),
                Type.Color      => string.Equals(design.Color, Value, StringComparison.OrdinalIgnoreCase),
                _               => false,
            };

        private static bool IsContained(string value, IEnumerable<string> data)
            => data.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase));

        public override string ToString()
            => $"\"{Which switch { Type.Name => 'n', Type.Identifier => 'i', Type.Path => 'p', Type.Tag => 't', Type.Color => 'c', _ => '?' }}?{Value}\"";
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
        if (restrictions.Length is 0)
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
        if (predicates.Count is 0)
            return string.Empty;
        if (predicates.Count is 1)
            return predicates.First()!.ToString()!;

        return $"{{{string.Join("; ", predicates)}}}";
    }
}
