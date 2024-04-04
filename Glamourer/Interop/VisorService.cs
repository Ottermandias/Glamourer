using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Events;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

public class VisorService : IDisposable
{
    public readonly VisorStateChanged Event;

    public unsafe VisorService(VisorStateChanged visorStateChanged, IGameInteropProvider interop)
    {
        Event           = visorStateChanged;
        _setupVisorHook = interop.HookFromAddress<UpdateVisorDelegateInternal>((nint)Human.MemberFunctionPointers.SetupVisor, SetupVisorDetour);
        _setupVisorHook.Enable();
    }

    public void Dispose()
        => _setupVisorHook.Dispose();

    /// <summary> Obtain the current state of the Visor for the given draw object (true: toggled). </summary>
    public static unsafe bool GetVisorState(Model characterBase)
        => characterBase.IsCharacterBase && characterBase.AsCharacterBase->VisorToggled;

    /// <summary> Manually set the state of the Visor for the given draw object. </summary>
    /// <param name="human"> The draw object. </param>
    /// <param name="on"> The desired state (true: toggled). </param>
    /// <returns> Whether the state was changed. </returns>
    public bool SetVisorState(Model human, bool on)
    {
        if (!human.IsHuman)
            return false;

        var oldState = GetVisorState(human);
        Glamourer.Log.Verbose($"[SetVisorState] Invoked manually on 0x{human.Address:X} switching from {oldState} to {on}.");
        if (oldState == on)
            return false;

        SetupVisorDetour(human, human.GetArmor(EquipSlot.Head).Set.Id, on);
        return true;
    }

    private delegate void UpdateVisorDelegateInternal(nint humanPtr, ushort modelId, bool on);

    private readonly Hook<UpdateVisorDelegateInternal> _setupVisorHook;

    private void SetupVisorDetour(nint human, ushort modelId, bool on)
    {
        var originalOn = on;
        // Invoke an event that can change the requested value
        // and also control whether the function should be called at all.
        Event.Invoke(human, false, ref on);

        Glamourer.Log.Excessive(
            $"[SetVisorState] Invoked from game on 0x{human:X} switching to {on} (original {originalOn}).");

        SetupVisorDetour((Model)human, modelId, on);
    }

    /// <summary>
    /// The SetupVisor function does not set the visor state for the draw object itself,
    /// it only sets the "visor is changing" state to false.
    /// So we wrap a manual change of that flag with the function call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private unsafe void SetupVisorDetour(Model human, ushort modelId, bool on)
    {
        human.AsCharacterBase->VisorToggled = on;
        _setupVisorHook.Original(human.Address, modelId, on);
    }
}
