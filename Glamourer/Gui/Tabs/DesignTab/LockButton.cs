using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class LockButton(DesignFileSystem fileSystem, DesignManager manager) : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => ((Design)fileSystem.Selection.Selection!.Value).WriteProtected()
            ? LunaStyle.LockedIcon
            : LunaStyle.UnlockedIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text(((Design)fileSystem.Selection.Selection!.Value).WriteProtected()
            ? "Make this design editable."u8
            : "Write-protect this design."u8);

    public override void OnClick()
        => manager.SetWriteProtection((Design)fileSystem.Selection.Selection!.Value,
            !((Design)fileSystem.Selection.Selection!.Value).WriteProtected());
}
