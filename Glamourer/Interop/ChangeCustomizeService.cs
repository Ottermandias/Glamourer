using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
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
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCustomizeChange"/>
        StateListener = 0,
    }

    public ChangeCustomizeService()
        : base("ChangeCustomize")
    {
        _changeCustomizeHook =
            Hook<ChangeCustomizeDelegate>.FromAddress((nint)Human.MemberFunctionPointers.UpdateDrawData, ChangeCustomizeDetour);
        _changeCustomizeHook.Enable();
    }

    public new void Dispose()
    {
        base.Dispose();
        _changeCustomizeHook.Dispose();
    }

    private delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature(Sigs.ChangeCustomize, DetourName = nameof(ChangeCustomizeDetour))]
    private readonly Hook<ChangeCustomizeDelegate> _changeCustomizeHook;

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
        fixed (byte* ptr = customize.Value.Data.Data)
        {
            return _changeCustomizeHook.Original(human, ptr, skipEquipment);
        }
    }
}
