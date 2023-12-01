using System;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
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
    private readonly PenumbraReloaded                                        _penumbraReloaded;
    private readonly IGameInteropProvider                                    _interop;
    private readonly delegate* unmanaged[Stdcall]<Human*, byte*, bool, bool> _original;

    /// <summary> Check whether we in a manual customize update, in which case we need to not toggle certain flags. </summary>
    public static readonly InMethodChecker InUpdate = new();

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
        _original            = Human.MemberFunctionPointers.UpdateDrawData;
        _penumbraReloaded.Subscribe(Restore, PenumbraReloaded.Priority.ChangeCustomizeService);
    }

    protected override void Dispose(bool _)
    {
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

    private Hook<ChangeCustomizeDelegate> _changeCustomizeHook;

    public bool UpdateCustomize(Model model, CustomizeData customize)
    {
        if (!model.IsHuman)
            return false;

        Glamourer.Log.Verbose($"[ChangeCustomize] Invoked on 0x{model.Address:X} with {customize}.");
        using var _   = InUpdate.EnterMethod();
        var       ret = _original(model.AsHuman, customize.Data, true);
        return ret;
    }

    public bool UpdateCustomize(Actor actor, CustomizeData customize)
        => UpdateCustomize(actor.Model, customize);

    private bool ChangeCustomizeDetour(Human* human, byte* data, byte skipEquipment)
    {
        if (!InUpdate.InMethod)
        {
            var customize = new Ref<Customize>(new Customize(*(CustomizeData*)data));
            Invoke(this, (Model)human, customize);
            ((Customize*)data)->Load(customize.Value);
        }
        return _changeCustomizeHook.Original(human, data, skipEquipment);
    }
}
