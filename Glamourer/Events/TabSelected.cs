using System;
using Glamourer.Designs;
using Glamourer.Gui;
using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when an automated design is changed in any way.
/// <list type="number">
///     <item>Parameter is the tab to select. </item>
///     <item>Parameter is the design to select if the tab is the designs tab. </item>
/// </list>
/// </summary>
public sealed class TabSelected : EventWrapper<Action<MainWindow.TabType, Design?>,
    TabSelected.Priority>
{
    public enum Priority
    {
        /// <seealso cref="Gui.Tabs.DesignTab.DesignFileSystemSelector.OnTabSelected"/>
        DesignSelector = 0,

        /// <seealso cref="Gui.MainWindow.OnTabSelected"/>
        MainWindow = 1,
    }

    public TabSelected()
        : base(nameof(TabSelected))
    { }

    public void Invoke(MainWindow.TabType type, Design? design)
        => Invoke(this, type, design);
}
