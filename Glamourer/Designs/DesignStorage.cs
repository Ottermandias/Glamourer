using Luna;

namespace Glamourer.Designs;

public class DesignStorage : List<Design>, IService
{
    public bool TryGetValue(Guid identifier, [NotNullWhen(true)] out Design? design)
    {
        design = ByIdentifier(identifier);
        return design != null;
    }

    public Design? ByIdentifier(Guid identifier)
        => this.FirstOrDefault(d => d.Identifier == identifier);

    public bool Contains(Guid identifier)
        => ByIdentifier(identifier) != null;
}
