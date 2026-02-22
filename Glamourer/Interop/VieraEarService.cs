using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

public unsafe sealed class VieraEarService : IDisposable, IRequiredService
{
    private readonly PenumbraReloaded     _penumbra;
    private readonly IGameInteropProvider _interop;
    public readonly  VieraEarStateChanged Event;

    public VieraEarService(VieraEarStateChanged visorStateChanged, IGameInteropProvider interop, PenumbraReloaded penumbra)
    {
        _interop           = interop;
        _penumbra          = penumbra;
        Event              = visorStateChanged;
        _setupVieraEarHook = Create();
        _penumbra.Subscribe(Restore, PenumbraReloaded.Priority.VieraEarService);
    }

    public void Dispose()
    {
        _setupVieraEarHook.Dispose();
        _penumbra.Unsubscribe(Restore);
    }

    /// <summary> Obtain the current state of viera ears for the given draw object (true: toggled). </summary>
    public static unsafe bool GetVieraEarState(Model characterBase)
        => characterBase is { IsCharacterBase: true, VieraEarsVisible: true };

    /// <summary> Manually set the state of the Visor for the given draw object. </summary>
    /// <param name="human"> The draw object. </param>
    /// <param name="on"> The desired state (true: toggled). </param>
    /// <returns> Whether the state was changed. </returns>
    public bool SetVieraEarState(Model human, bool on)
    {
        if (!human.IsHuman)
            return false;

        var oldState = GetVieraEarState(human);
        Glamourer.Log.Verbose($"[SetVieraEarState] Invoked manually on 0x{human.Address:X} switching from {oldState} to {on}.");
        if (oldState == on)
            return false;

        human.VieraEarsVisible = on;
        return true;
    }

    private delegate void UpdateVieraEarDelegateInternal(DrawDataContainer* drawData, byte on);

    private Hook<UpdateVieraEarDelegateInternal> _setupVieraEarHook;

    private void SetupVieraEarDetour(DrawDataContainer* drawData, byte value)
    {
        Actor actor      = drawData->OwnerObject;
        var originalOn = value is not 0;
        var on         = originalOn;
        // Invoke an event that can change the requested value
        Event.Invoke(new VieraEarStateChanged.Arguments(actor, ref on));

        Glamourer.Log.Verbose(
            $"[SetVieraEarState] Invoked from game on 0x{actor.Address:X} switching to {on} (original {originalOn} from {value}).");

        _setupVieraEarHook.Original(drawData, on ? (byte)1 : (byte)0);
    }

    private unsafe Hook<UpdateVieraEarDelegateInternal> Create()
    {
        var hook = _interop.HookFromSignature<UpdateVieraEarDelegateInternal>(Sigs.SetupVieraEars, SetupVieraEarDetour);
        hook.Enable();
        return hook;
    }

    private void Restore()
    {
        _setupVieraEarHook.Dispose();
        _setupVieraEarHook = Create();
    }
}
