using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Events;
using Luna;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

/// <summary>
/// Access the function the game uses to update customize data on the character screen.
/// Changes in Race, body type or Gender are probably ignored.
/// This operates on draw objects, not game objects.
/// </summary>
public sealed unsafe class ChangeCustomizeService : EventBase<ChangeCustomizeService.Arguments, ChangeCustomizeService.Priority>
{
    private readonly PenumbraReloaded                               _penumbraReloaded;
    private readonly IGameInteropProvider                           _interop;
    private readonly delegate* unmanaged<Human*, byte*, bool, bool> _original;
    private readonly Post                                           _postEvent;

    public ref struct Arguments(Model model, ref CustomizeArray customize)
    {
        public readonly Model          Model     = model;
        public ref      CustomizeArray Customize = ref customize;
    }


    /// <summary> Check whether we in a manual customize update, in which case we need to not toggle certain flags. </summary>
    public static readonly InMethodChecker InUpdate = new();

    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCustomizeChange"/>
        StateListener = 0,
    }

    public ChangeCustomizeService(PenumbraReloaded penumbraReloaded, IGameInteropProvider interop, Logger log)
        : base("ChangeCustomize", log)
    {
        _penumbraReloaded    = penumbraReloaded;
        _interop             = interop;
        _changeCustomizeHook = Create();
        _original            = Human.MemberFunctionPointers.UpdateDrawData;
        interop.InitializeFromAttributes(this);
        _penumbraReloaded.Subscribe(Restore, PenumbraReloaded.Priority.ChangeCustomizeService);
        _postEvent = new Post(log);
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

    public bool UpdateCustomize(Model model, CustomizeArray customize)
    {
        if (!model.IsHuman)
            return false;

        Glamourer.Log.Verbose($"[ChangeCustomize] Invoked on 0x{model.Address:X} with {customize}.");
        using var _   = InUpdate.EnterMethod();
        var       ret = _original(model.AsHuman, customize.Data, true);
        return ret;
    }

    public bool UpdateCustomize(Actor actor, CustomizeArray customize)
        => UpdateCustomize(actor.Model, customize);

    private bool ChangeCustomizeDetour(Human* human, byte* data, byte skipEquipment)
    {
        if (!InUpdate.InMethod)
            Invoke(new Arguments(human, ref *(CustomizeArray*)data));

        var ret = _changeCustomizeHook.Original(human, data, skipEquipment);
        _postEvent.Invoke(human);
        return ret;
    }

    public void Subscribe(InAction<Model> action, Post.Priority priority)
        => _postEvent.Subscribe(action, priority);

    public void Unsubscribe(InAction<Model> action)
        => _postEvent.Unsubscribe(action);

    public sealed class Post(Logger log) : EventBase<Model, Post.Priority>(nameof(ChangeCustomizeService) + '.' + nameof(Post), log)
    {
        public enum Priority
        {
            /// <seealso cref="State.StateListener.OnCustomizeChanged"/>
            StateListener = 0,
        }
    }
}
