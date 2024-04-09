namespace Glamourer.Gui.Tabs.DesignTab;

public enum RenameField
{
    None,
    RenameSearchPath,
    RenameData,
    BothSearchPathPrio,
    BothDataPrio,
}

public static class RenameFieldExtensions
{
    public static (string Name, string Desc) GetData(this RenameField value)
        => value switch
        {
            RenameField.None             => ("None", "Show no rename fields in the context menu for designs."),
            RenameField.RenameSearchPath => ("Search Path", "Show only the search path / move field in the context menu for designs."),
            RenameField.RenameData       => ("Design Name", "Show only the design name field in the context menu for designs."),
            RenameField.BothSearchPathPrio => ("Both (Focus Search Path)",
                "Show both rename fields in the context menu for designs, but put the keyboard cursor on the search path field."),
            RenameField.BothDataPrio => ("Both (Focus Design Name)",
                "Show both rename fields in the context menu for designs, but put the keyboard cursor on the design name field"),
            _ => (string.Empty, string.Empty),
        };
}
