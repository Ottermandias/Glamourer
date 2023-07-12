using Dalamud.Game.ClientState.Objects.Enums;
using Glamourer.Services;
using ImGuiNET;
using OtterGui.Custom;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class IdentifierDrawer
{
    private readonly WorldCombo    _worldCombo;
    private readonly HumanNpcCombo _humanNpcCombo;
    private readonly ActorService  _actors;

    private string _characterName = string.Empty;

    public ActorIdentifier NpcIdentifier      { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier PlayerIdentifier   { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier RetainerIdentifier { get; private set; } = ActorIdentifier.Invalid;

    public IdentifierDrawer(ActorService actors, IdentifierService identifier, HumanModelList humans)
    {
        _actors        = actors;
        _worldCombo    = new WorldCombo(actors.AwaitedService.Data.Worlds);
        _humanNpcCombo = new HumanNpcCombo("Human Event NPCs", identifier, humans);
    }

    public void DrawName(float width)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputTextWithHint("##Name", "Character Name...", ref _characterName, 32))
            UpdateIdentifiers();
    }

    public void DrawWorld(float width)
    {
        if (_worldCombo.Draw(width))
            UpdateIdentifiers();
    }

    public void DrawNpcs(float width)
    {
        if (_humanNpcCombo.Draw(width))
            UpdateIdentifiers();
    }

    public bool CanSetPlayer
        => PlayerIdentifier.IsValid;

    public bool CanSetRetainer
        => RetainerIdentifier.IsValid;

    public bool CanSetNpc
        => NpcIdentifier.IsValid;

    private void UpdateIdentifiers()
    {
        if (ByteString.FromString(_characterName, out var byteName))
        {
            PlayerIdentifier   = _actors.AwaitedService.CreatePlayer(byteName, _worldCombo.CurrentSelection.Key);
            RetainerIdentifier = _actors.AwaitedService.CreateRetainer(byteName, ActorIdentifier.RetainerType.Both);
        }

        NpcIdentifier = _humanNpcCombo.CurrentSelection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc
            ? _actors.AwaitedService.CreateNpc(_humanNpcCombo.CurrentSelection.Kind, _humanNpcCombo.CurrentSelection.Ids[0])
            : ActorIdentifier.Invalid;
    }
}
