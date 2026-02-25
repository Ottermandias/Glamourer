using Glamourer.Designs;
using Glamourer.Gui;
using Luna;

namespace Glamourer.Events;

/// <summary> Triggered to select a tab or design. </summary>
public sealed class TabSelected(Logger log)
    : EventBase<TabSelected.Arguments, TabSelected.Priority>(nameof(TabSelected), log)
{
    public enum Priority
    {
        /// <seealso cref="DesignFileSystem.OnTabSelected"/>
        DesignSelector = 0,

        /// <seealso cref="Gui.MainTabBar.OnEvent"/>
        MainWindow = 1,
    }

    /// <summary> Arguments for the TabSelected event. </summary>
    /// <param name="Type"> The tab to be selected. </param>
    /// <param name="Design"> The design to be selected in the Designs tab. </param>
    public readonly record struct Arguments(MainTabType Type, Design? Design = null);
}
