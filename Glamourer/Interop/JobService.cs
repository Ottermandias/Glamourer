using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public sealed class JobService : IDisposable, IRequiredService
{
    private readonly HookManager _hooks;
    private readonly nint        _characterDataOffset;

    public readonly DictJob      Jobs;
    public readonly DictJobGroup JobGroups;

    public IReadOnlyList<JobGroup> AllJobGroups
        => JobGroups.AllJobGroups;

    public event Action<Actor, Job, Job>? JobChanged;

    public JobService(DictJob jobs, DictJobGroup jobGroups, HookManager hooks)
    {
        _characterDataOffset = Marshal.OffsetOf<Character>(nameof(Character.CharacterData));
        Jobs                 = jobs;
        JobGroups            = jobGroups;
        _hooks               = hooks;
        _changeJobHook       = _hooks.CreateHook<ChangeJobDelegate>("ChangeJob", Sigs.ChangeJob, ChangeJobDetour, true)!;
    }

    public void Dispose()
        => _hooks.DisposeHook("ChangeJob");

    private delegate void ChangeJobDelegate(nint data, byte oldJob, byte newJob);

    private readonly Task<Hook<ChangeJobDelegate>> _changeJobHook;

    private void ChangeJobDetour(nint data, byte oldJobIndex, byte newJobIndex)
    {
        _changeJobHook.Result.OriginalDisposeSafe(data, oldJobIndex, newJobIndex);

        // Do not trigger on creation (Adventurer -> Anything)
        if (oldJobIndex is 0)
            return;

        var actor  = (Actor)(data - _characterDataOffset);
        var newJob = Jobs.TryGetValue(newJobIndex, out var j) ? j : Jobs[0];
        var oldJob = Jobs.TryGetValue(oldJobIndex, out var o) ? o : Jobs[0];

        Glamourer.Log.Excessive($"{actor} changed job from {oldJob} to {newJob}.");
        JobChanged?.Invoke(actor, oldJob, newJob);
    }
}
