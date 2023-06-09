using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Glamourer.Structs;

namespace Glamourer.Interop;

public class JobService : IDisposable
{
    public readonly IReadOnlyDictionary<byte, Job>        Jobs;
    public readonly IReadOnlyDictionary<ushort, JobGroup> JobGroups;

    public event Action<Actor, Job>? JobChanged;

    public JobService(DataManager gameData)
    {
        SignatureHelper.Initialise(this);
        Jobs      = GameData.Jobs(gameData);
        JobGroups = GameData.JobGroups(gameData);
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
        _changeJobHook.Original(data, jobIndex);
        var actor = (Actor)(data - Offsets.Character.ClassJobContainer);
        var job   = Jobs[(byte)jobIndex];
        Glamourer.Log.Excessive($"{actor} changed job to {job}");
        JobChanged?.Invoke(actor, job);
    }
}
