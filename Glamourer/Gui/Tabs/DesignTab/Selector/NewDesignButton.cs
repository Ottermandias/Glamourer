using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class NewDesignButton(DesignManager designManager) : BaseIconButton<AwesomeIcon>
{
    public override AwesomeIcon Icon
        => LunaStyle.AddObjectIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Create a new design with default configuration."u8);

    public override void OnClick()
        => Im.Popup.Open("##NewDesign"u8);

    protected override void PostDraw()
    {
        if (!InputPopup.OpenName("##NewDesign"u8, out var newName))
            return;

        designManager.CreateEmpty(newName, true);
    }
}
