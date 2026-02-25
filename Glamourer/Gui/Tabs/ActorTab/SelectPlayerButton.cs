using Dalamud.Interface;
using ImSharp;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class SelectPlayerButton(ActorObjectManager objects, ActorSelection selection) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => FontAwesomeIcon.UserCircle;

    public override void DrawTooltip()
        => Im.Text("Select the local player character."u8);

    public override bool HasTooltip
        => true;

    public override bool Enabled
        => objects.Player;

    public override void OnClick()
    {
        var (identifier, data) = objects.PlayerData;
        selection.Select(identifier, data);
    }
}
