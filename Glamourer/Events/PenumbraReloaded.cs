using Luna;

namespace Glamourer.Events;

/// <summary>
/// Triggered when Penumbra is reloaded.
/// </summary>
public sealed class PenumbraReloaded(Logger log)
    : EventBase<PenumbraReloaded.Priority>(nameof(PenumbraReloaded), log)
{
    public enum Priority
    {
        /// <seealso cref="global::Glamourer.Interop.ChangeCustomizeService.Restore"/>
        ChangeCustomizeService = 0,

        /// <seealso cref="global::Glamourer.Interop.VisorService.Restore"/>
        VisorService = 0,

        /// <seealso cref="global::Glamourer.Interop.VieraEarService.Restore"/>
        VieraEarService = 0,
    }
}
