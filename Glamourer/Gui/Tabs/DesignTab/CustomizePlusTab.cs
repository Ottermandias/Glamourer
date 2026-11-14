using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.CustomizePlus;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class CustomizePlusTab(CustomizePlusIpc ipc, DesignFileSystem fileSystem, DesignManager manager, Configuration config)
    : IUiService
{
    private Design Selection
        => (Design)fileSystem.Selection.Selection!.Value;

    public void Draw()
    {
        using var header = DesignPanelFlag.CustomizePlus.Header(config);
        if (!header)
            return;

        if (Im.Button("Refresh Profiles"u8))
            ipc.Refresh(true);

        using var table = Im.Table.Begin("CustomizePlusProfiles"u8, 4, TableFlags.RowBackground);
        if (!table)
            return;

        table.SetupColumn("Profile"u8, TableColumnFlags.WidthStretch);
        table.SetupColumn("Enable"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Enable"u8).X + Im.Style.FrameHeight);
        table.SetupColumn("Disable"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Disable"u8).X + Im.Style.FrameHeight);
        table.SetupColumn("Ignore"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Ignore"u8).X + Im.Style.FrameHeight);
        table.HeaderRow();

        foreach (var profile in ipc.Profiles.OrderBy(p => p.Path).ThenBy(p => p.Name))
        {
            using var id = Im.Id.Push($"{profile.Id}");
            var action = Selection.CustomizePlusActions.GetValueOrDefault(profile.Id);
            table.NextColumn();
            Im.Text(profile.Name);
            Im.Tooltip.OnHover(profile.Path);
            table.NextColumn();
            if (Im.RadioButton("##enable"u8, action is CustomizePlusAction.Enable))
                manager.SetCustomizePlusAction(Selection, profile.Id, CustomizePlusAction.Enable);
            table.NextColumn();
            if (Im.RadioButton("##disable"u8, action is CustomizePlusAction.Disable))
                manager.SetCustomizePlusAction(Selection, profile.Id, CustomizePlusAction.Disable);
            table.NextColumn();
            if (Im.RadioButton("##ignore"u8, action is CustomizePlusAction.Ignore))
                manager.SetCustomizePlusAction(Selection, profile.Id, CustomizePlusAction.Ignore);
        }
    }
}
