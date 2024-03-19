using OtterGui.Log;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Structs;

/// <summary>
/// A single actor with its label and the list of associated game objects.
/// </summary>
public readonly struct ActorData
{
    public readonly List<Actor> Objects;
    public readonly string      Label;

    public bool Valid
        => Objects.Count > 0;

    public ActorData(Actor actor, string label)
    {
        Objects = [actor];
        Label   = label;
    }

    public static readonly ActorData Invalid = new(false);

    private ActorData(bool _)
    {
        Objects = [];
        Label   = string.Empty;
    }

    public LazyString ToLazyString(string invalid)
    {
        var objects = Objects;
        return Valid
            ? new LazyString(() => string.Join(", ", objects.Select(o => o.ToString())))
            : new LazyString(() => invalid);
    }

    private ActorData(List<Actor> objects, string label)
    {
        Objects = objects;
        Label   = label;
    }

    public ActorData OnlyGPose()
        => new(Objects.Where(o => o.IsGPoseOrCutscene).ToList(), Label);
}
