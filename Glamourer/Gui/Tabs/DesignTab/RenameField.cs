using Luna.Generators;

namespace Glamourer.Gui.Tabs.DesignTab;

[NamedEnum(Utf16: false)]
[TooltipEnum]
public enum RenameField
{
    [Name("None")]
    [Tooltip("Show no rename fields in the context menu for designs.")]
    None,

    [Name("Search Path")]
    [Tooltip("Show only the search path / move field in the context menu for designs.")]
    RenameSearchPath,

    [Name("Design Name")]
    [Tooltip("Show only the design name field in the context menu for designs.")]
    RenameData,

    [Name("Both (Focus Search Path)")]
    [Tooltip("Show both rename fields in the context menu for designs, but put the keyboard cursor on the search path field.")]
    BothSearchPathPrio,

    [Name("Both (Focus Design Name)")]
    [Tooltip("Show both rename fields in the context menu for designs, but put the keyboard cursor on the design name field")]
    BothDataPrio,
}
