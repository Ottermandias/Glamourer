using Dalamud.Hooking;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

public sealed class EnableDrawEvent : EventBase<Actor, EnableDrawEvent.Priority>
{
    private readonly HookManager          _hooks;
    private readonly Task<Hook<Delegate>> _enableDrawHook;

    public EnableDrawEvent(HookManager hooks, LunaLogger log)
        : base("Enable Draw", log)
    {
        _hooks          = hooks;
        _enableDrawHook = _hooks.CreateHook<Delegate>("EnableDraw", Sigs.EnableDraw, Detour, true)!;
    }

    private delegate void Delegate(Actor character);

    private void Detour(Actor character)
    {
        _enableDrawHook.Result.Original(character);
        Invoke(character);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _hooks.DisposeHook("EnableDraw");
    }

    public enum Priority
    {
        /// <seealso cref="State.StateListener.AfterEnableDraw"/>
        StateListener,
    }
}
