using System;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using OtterGui.Classes;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

/// <summary>
/// Access the function the game uses to update customize data on the character screen.
/// Changes in Race, body type or Gender are probably ignored.
/// This operates on draw objects, not game objects.
/// </summary>
public unsafe class ChangeCustomizeService : EventWrapper<Action<Model, Ref<Customize>>, ChangeCustomizeService.Priority>
{
    private readonly PenumbraReloaded     _penumbraReloaded;
    private readonly IGameInteropProvider _interop;

    /// <summary> Check whether we in a manual customize update, in which case we need to not toggle certain flags. </summary>
    public static readonly ThreadLocal<bool> InUpdate = new(() => false);

    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCustomizeChange"/>
        StateListener = 0,
    }

    public ChangeCustomizeService(PenumbraReloaded penumbraReloaded, IGameInteropProvider interop)
        : base("ChangeCustomize")
    {
        _penumbraReloaded    = penumbraReloaded;
        _interop             = interop;
        _changeCustomizeHook = Create();
        _penumbraReloaded.Subscribe(Restore, PenumbraReloaded.Priority.ChangeCustomizeService);
    }

    public new void Dispose()
    {
        base.Dispose();
        _changeCustomizeHook.Dispose();
        _penumbraReloaded.Unsubscribe(Restore);
    }

    private void Restore()
    {
        _changeCustomizeHook.Dispose();
        _changeCustomizeHook = Create();
    }

    private Hook<ChangeCustomizeDelegate> Create()
    {
        var ret = _interop.HookFromAddress<ChangeCustomizeDelegate>((nint)Human.MemberFunctionPointers.UpdateDrawData, ChangeCustomizeDetour);
        ret.Enable();
        return ret;
    }

    private delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature(Sigs.ChangeCustomize, DetourName = nameof(ChangeCustomizeDetour))]
    private Hook<ChangeCustomizeDelegate> _changeCustomizeHook;

    public bool UpdateCustomize(Model model, CustomizeData customize)
    {
        if (!model.IsHuman)
            return false;

        Glamourer.Log.Verbose($"[ChangeCustomize] Invoked on 0x{model.Address:X} with {customize}.");
        InUpdate.Value = true;
        var ret = _changeCustomizeHook.Original(model.AsHuman, customize.Data, 1);
        InUpdate.Value = false;
        return ret;
    }

    public bool UpdateCustomize(Actor actor, CustomizeData customize)
        => UpdateCustomize(actor.Model, customize);

    private bool ChangeCustomizeDetour(Human* human, byte* data, byte skipEquipment)
    {
        var customize = new Ref<Customize>(new Customize(*(CustomizeData*)data));
        Invoke(this, (Model)human, customize);
        ((Customize*)data)->Load(customize.Value);
        return _changeCustomizeHook.Original(human, data, skipEquipment);
    }
}
