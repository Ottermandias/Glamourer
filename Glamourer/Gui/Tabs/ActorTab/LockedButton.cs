using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class LockedButton(ActorSelection selection) : BaseIconButton<AwesomeIcon>, IUiService
{
    private readonly Im.ColorDisposable _color = new();

    public override AwesomeIcon Icon
        => LunaStyle.LockedIcon;

    public override bool IsVisible
        => selection.State?.IsLocked ?? false;

    public override bool Enabled
        => true;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("The current state of this actor is locked by external tools."u8);

    protected override void PreDraw()
    {
        var color = ColorId.ActorAvailable.Value();
        _color.Push(ImGuiColor.Border, color)
            .Push(ImGuiColor.Text, color);
    }

    protected override void PostDraw()
        => _color.Dispose();
}
