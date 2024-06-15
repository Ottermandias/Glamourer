using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when Penumbra is reloaded.
/// </summary>
public sealed class PenumbraReloaded()
    : EventWrapper<PenumbraReloaded.Priority>(nameof(PenumbraReloaded))
{
    public enum Priority
    {
        /// <seealso cref="Interop.ChangeCustomizeService.Restore"/>
        ChangeCustomizeService = 0,

        /// <seealso cref="Interop.VisorService.Restore"/>
        VisorService = 0,
    }
}
