using System.Diagnostics.CodeAnalysis;
using Glamourer.Interop;

namespace Glamourer.State;

public class FixedDesigns
{
    public bool TryGetDesign(Actor.IIdentifier actor, [NotNullWhen(true)] out CharacterSave? save)
    {
        save = null;
        return false;
    }
}
