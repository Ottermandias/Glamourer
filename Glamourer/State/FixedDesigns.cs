using System.Diagnostics.CodeAnalysis;
using Glamourer.Designs;
using Glamourer.Interop;
using Penumbra.GameData.Actors;

namespace Glamourer.State;

public class FixedDesigns
{
    public bool TryGetDesign(ActorIdentifier actor, [NotNullWhen(true)] out Design? save)
    {
        save = null;
        return false;
    }
}
