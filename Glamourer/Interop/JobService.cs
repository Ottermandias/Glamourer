using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Interop.Structs;
using Glamourer.Structs;

namespace Glamourer.Interop;

public class JobService : IDisposable
{
    private readonly nint _characterDataOffset;

    public readonly IReadOnlyDictionary<byte, Job>        Jobs;
    public readonly IReadOnlyDictionary<ushort, JobGroup> JobGroups;

    public event Action<Actor, Job, Job>? JobChanged;

    public JobService(DataManager gameData)
    {
        SignatureHelper.Initialise(this);
        _characterDataOffset = Marshal.OffsetOf<Character>(nameof(Character.CharacterData));
        Jobs                 = GameData.Jobs(gameData);
        JobGroups            = GameData.JobGroups(gameData);
        _changeJobHook.Enable();
    }

    public void Dispose()
    {
        _changeJobHook.Dispose();
    }

    private delegate void ChangeJobDelegate(nint data, uint job);

    [Signature(Sigs.ChangeJob, DetourName = nameof(ChangeJobDetour))]
    private readonly Hook<ChangeJobDelegate> _changeJobHook = null!;

    private void ChangeJobDetour(nint data, uint jobIndex)
    {
        var old = ((Actor)(data - _characterDataOffset)).Job;
        _changeJobHook.Original(data, jobIndex);
        var actor  = (Actor)(data - _characterDataOffset);
        var job    = Jobs.TryGetValue((byte)jobIndex, out var j) ? j : Jobs[0];
        var oldJob = Jobs.TryGetValue(old,            out var o) ? o : Jobs[0];
        Glamourer.Log.Excessive($"{actor} changed job from {oldJob} to {job}");
        JobChanged?.Invoke(actor, oldJob, job);
    }
}
