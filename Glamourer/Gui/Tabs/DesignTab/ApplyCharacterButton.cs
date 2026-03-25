using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class ApplyCharacterButton(
    DesignFileSystem fileSystem,
    DesignManager manager,
    ActorObjectManager objects,
    StateManager stateManager,
    DesignConverter converter) : BaseIconButton<AwesomeIcon>
{
    private static readonly AwesomeIcon UserIcon = FontAwesomeIcon.UserEdit;

    public override bool IsVisible
        => fileSystem.Selection.Selection is not null && objects.Player.Valid;

    public override AwesomeIcon Icon
        => UserIcon;

    public override bool Enabled
        => !((Design)fileSystem.Selection.Selection!.Value).WriteProtected();

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Overwrite this design with your character's current state."u8);

    public override void OnClick()
    {
        var selection = (Design)fileSystem.Selection.Selection!.Value;
        try
        {
            var (player, actor) = objects.PlayerData;
            if (!player.IsValid || !actor.Valid || !stateManager.GetOrCreate(player, actor.Objects[0], out var state))
                throw new Exception("No player state available.");

            var design = converter.Convert(state, ApplicationRules.FromModifiers(state))
             ?? throw new Exception("The clipboard did not contain valid data.");
            selection.GetMaterialDataRef().Clear();
            manager.ApplyDesign(selection, design);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply player state to {selection.Name}.",
                $"Could not apply player state to design {selection.Identifier}", NotificationType.Error, false);
        }
    }
}
