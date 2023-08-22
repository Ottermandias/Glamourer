using System;
using Dalamud.Hooking;
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
    private readonly PenumbraReloaded _penumbraReloaded;

    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCustomizeChange"/>
        StateListener = 0,
    }

    public ChangeCustomizeService(PenumbraReloaded penumbraReloaded)
        : base("ChangeCustomize")
    {
        _penumbraReloaded    = penumbraReloaded;
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
        var ret = Hook<ChangeCustomizeDelegate>.FromAddress((nint)Human.MemberFunctionPointers.UpdateDrawData, ChangeCustomizeDetour);
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
        return _changeCustomizeHook.Original(model.AsHuman, customize.Data, 1);
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
