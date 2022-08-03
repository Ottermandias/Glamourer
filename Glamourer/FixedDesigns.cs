using System.Diagnostics.CodeAnalysis;

namespace Glamourer;

public class FixedDesigns
{
    public bool TryGetDesign(Actor.IIdentifier actor, [NotNullWhen(true)] out CharacterSave? save)
    {
        save = null;
        return false;
    }
}
