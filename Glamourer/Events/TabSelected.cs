using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Gui.Tabs.DesignTab;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when an automated design is changed in any way.
/// <list type="number">
///     <item>Parameter is the tab to select. </item>
///     <item>Parameter is the design to select if the tab is the designs tab. </item>
/// </list>
/// </summary>
public sealed class TabSelected()
    : EventWrapper<MainTabType, Design?, TabSelected.Priority>(nameof(TabSelected))
{
    public enum Priority
    {
        /// <seealso cref="DesignFileSystemDrawer.OnTabSelected"/>
        DesignSelector = 0,

        /// <seealso cref="Gui.MainWindow.OnTabSelected"/>
        MainWindow = 1,
    }
}
