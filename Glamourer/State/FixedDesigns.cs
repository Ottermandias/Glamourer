using System.Diagnostics.CodeAnalysis;
using Glamourer.Interop;
using Penumbra.GameData.Actors;

namespace Glamourer.State;

public class FixedDesigns
{
    public bool TryGetDesign(ActorIdentifier actor, [NotNullWhen(true)] out CharacterSave? save)
    {
        save = null;
        return false;
    }
}
