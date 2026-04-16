using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationTesterButton(AutomationTestWindow window) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => LunaStyle.TestIcon;

    public override void DrawTooltip()
        => Im.Text("Open a window to test your currently selected automation set against different jobs."u8);

    public override void OnClick()
        => window.IsOpen = true;
}
