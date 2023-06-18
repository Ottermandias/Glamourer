using Glamourer.Designs;
using Penumbra.GameData.Actors;

namespace Glamourer.State;

public class ActorState
{
    public ActorIdentifier Identifier { get; internal init; }
    public DesignData      Data       { get; internal set; }

    internal ActorState(ActorIdentifier identifier)
        => Identifier = identifier;
}
