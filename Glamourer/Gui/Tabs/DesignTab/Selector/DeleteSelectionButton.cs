using Glamourer.Config;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DeleteSelectionButton(DesignFileSystem fileSystem, DesignManager manager, Configuration config)
    : BaseIconButton<AwesomeIcon>
{
    /// <inheritdoc/>
    public override AwesomeIcon Icon
        => LunaStyle.DeleteIcon;

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip()
    {
        var anySelected = fileSystem.Selection.DataNodes.Count > 0;
        var modifier    = Enabled;

        Im.Text(anySelected
            ? "Delete the currently selected designs entirely from your drive\nThis can not be undone."u8
            : "No designs selected."u8);
        if (!modifier)
            Im.Text($"\nHold {config.DeleteDesignModifier} while clicking to delete the designs.");
    }

    /// <inheritdoc/>
    public override bool Enabled
        => config.DeleteDesignModifier.IsActive() && fileSystem.Selection.DataNodes.Count > 0;

    /// <inheritdoc/>
    public override void OnClick()
    {
        var designs = fileSystem.Selection.DataNodes.Select(n => n.Value).OfType<Design>().ToList();
        fileSystem.Selection.UnselectAll();
        foreach (var design in designs)
            manager.Delete(design);
    }
}
