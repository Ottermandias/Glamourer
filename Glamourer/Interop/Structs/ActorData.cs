using System.Collections.Generic;

namespace Glamourer.Interop.Structs;

/// <summary>
/// A single actor with its label and the list of associated game objects.
/// </summary>
public readonly struct ActorData
{
    public readonly List<Actor> Objects;
    public readonly string Label;

    public bool Valid
        => Objects.Count > 0;

    public ActorData(Actor actor, string label)
    {
        Objects = new List<Actor> { actor };
        Label = label;
    }

    public static readonly ActorData Invalid = new(false);

    private ActorData(bool _)
    {
        Objects = new List<Actor>(0);
        Label = string.Empty;
    }
}
