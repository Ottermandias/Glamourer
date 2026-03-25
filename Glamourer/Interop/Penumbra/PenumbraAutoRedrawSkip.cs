using Luna;

namespace Glamourer.Interop.Penumbra;

public class PenumbraAutoRedrawSkip : IService
{
    private bool _skipAutoUpdates;

    public BoolSetter SkipAutoUpdates(bool skip)
        => new(ref _skipAutoUpdates, skip);

    public bool Skip
        => _skipAutoUpdates;
}
