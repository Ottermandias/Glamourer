using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.GameData;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public class JobService : IDisposable
{
    private readonly nint _characterDataOffset;

    public readonly DictJob      Jobs;
    public readonly DictJobGroup JobGroups;

    public IReadOnlyList<JobGroup> AllJobGroups
        => JobGroups.AllJobGroups;

    public event Action<Actor, Job, Job>? JobChanged;

    public JobService(DictJob jobs, DictJobGroup jobGroups, IGameInteropProvider interop)
    {
        interop.InitializeFromAttributes(this);
        _characterDataOffset = Marshal.OffsetOf<Character>(nameof(Character.CharacterData));
        Jobs                 = jobs;
        JobGroups            = jobGroups;
        _changeJobHook.Enable();
    }

    public void Dispose()
        => _changeJobHook.Dispose();

    private delegate void ChangeJobDelegate(nint data, byte oldJob, byte newJob);

    [Signature(Sigs.ChangeJob, DetourName = nameof(ChangeJobDetour))]
    private readonly Hook<ChangeJobDelegate> _changeJobHook = null!;

    private void ChangeJobDetour(nint data, byte oldJobIndex, byte newJobIndex)
    {
        _changeJobHook.OriginalDisposeSafe(data, oldJobIndex, newJobIndex);

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
