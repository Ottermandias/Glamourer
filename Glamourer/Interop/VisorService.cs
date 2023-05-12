using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public class VisorService : IDisposable
{
    public VisorService()
    {
        SignatureHelper.Initialise(this);
        _setupVisorHook.Enable();
    }

    public void Dispose()
        => _setupVisorHook.Dispose();

    public static unsafe bool GetVisorState(nint humanPtr)
    {
        if (humanPtr == IntPtr.Zero)
            return false;

        var data  = (Human*)humanPtr;
        var flags = &data->CharacterBase.UnkFlags_01;
        return (*flags & Offsets.DrawObjectVisorStateFlag) != 0;
    }

    public unsafe void SetVisorState(nint humanPtr, bool on)
    {
        if (humanPtr == IntPtr.Zero)
            return;

        var data  = (Human*)humanPtr;
        _setupVisorHook.Original(humanPtr, (ushort) data->HeadSetID, on);
    }

    private delegate void UpdateVisorDelegateInternal(nint humanPtr, ushort modelId, bool on);
    public delegate  void UpdateVisorDelegate(DrawObject human, SetId modelId, ref bool on);

    [Signature(Penumbra.GameData.Sigs.SetupVisor, DetourName = nameof(SetupVisorDetour))]
    private readonly Hook<UpdateVisorDelegateInternal> _setupVisorHook = null!;

    public event UpdateVisorDelegate? VisorUpdate;

    private void SetupVisorDetour(nint humanPtr, ushort modelId, bool on)
    {
        InvokeVisorEvent(humanPtr, modelId, ref on);
        _setupVisorHook.Original(humanPtr, modelId, on);
    }

    private void InvokeVisorEvent(DrawObject drawObject, SetId modelId, ref bool on)
    {
        if (VisorUpdate == null)
        {
            Glamourer.Log.Excessive($"Visor setup on 0x{drawObject.Address:X} with {modelId.Value}, setting to {on}.");
            return;
        }

        var initialValue = on;
        foreach (var del in VisorUpdate.GetInvocationList().OfType<UpdateVisorDelegate>())
        {
            try
            {
                del(drawObject, modelId, ref on);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not invoke {nameof(VisorUpdate)} Subscriber:\n{ex}");
            }
        }

        Glamourer.Log.Excessive(
            $"Visor setup on 0x{drawObject.Address:X} with {modelId.Value}, setting to {on}, initial call was {initialValue}.");
    }
}
