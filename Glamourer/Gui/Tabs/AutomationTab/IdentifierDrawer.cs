using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class IdentifierDrawer
{
    private readonly WorldCombo    _worldCombo;
    private readonly HumanNpcCombo _humanNpcCombo;
    private readonly ActorManager  _actors;

    private string _characterName = string.Empty;

    public ActorIdentifier NpcIdentifier       { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier PlayerIdentifier    { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier RetainerIdentifier  { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier MannequinIdentifier { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier OwnedIdentifier     { get; private set; } = ActorIdentifier.Invalid;

    public IdentifierDrawer(ActorManager actors, DictWorld dictWorld, DictModelChara dictModelChara, DictBNpcNames bNpcNames, DictBNpc bNpc,
        HumanModelList humans)
    {
        _actors        = actors;
        _worldCombo    = new WorldCombo(dictWorld, Glamourer.Log);
        _humanNpcCombo = new HumanNpcCombo("##npcs", dictModelChara, bNpcNames, bNpc, humans, Glamourer.Log);
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

    public bool CanSetMannequin
        => MannequinIdentifier.IsValid;

    public bool CanSetNpc
        => NpcIdentifier.IsValid;

    public bool CanSetOwned
        => OwnedIdentifier.IsValid;

    private void UpdateIdentifiers()
    {
        if (ByteString.FromString(_characterName, out var byteName))
        {
            PlayerIdentifier    = _actors.CreatePlayer(byteName, _worldCombo.CurrentSelection.Key);
            RetainerIdentifier  = _actors.CreateRetainer(byteName, ActorIdentifier.RetainerType.Bell);
            MannequinIdentifier = _actors.CreateRetainer(byteName, ActorIdentifier.RetainerType.Mannequin);

            if (_humanNpcCombo.CurrentSelection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc)
                OwnedIdentifier = _actors.CreateOwned(byteName, _worldCombo.CurrentSelection.Key, _humanNpcCombo.CurrentSelection.Kind, _humanNpcCombo.CurrentSelection.Ids[0]);
            else
                OwnedIdentifier = ActorIdentifier.Invalid;
        }

        NpcIdentifier = _humanNpcCombo.CurrentSelection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc
            ? _actors.CreateNpc(_humanNpcCombo.CurrentSelection.Kind, _humanNpcCombo.CurrentSelection.Ids[0])
            : ActorIdentifier.Invalid;
    }
}
