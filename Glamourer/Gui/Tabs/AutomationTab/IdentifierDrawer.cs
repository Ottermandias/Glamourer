using Dalamud.Game.ClientState.Objects.Enums;
using ImSharp;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Gui;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class IdentifierDrawer(
    ActorManager actors,
    DictWorld dictWorld,
    DictModelChara dictModelChara,
    DictBNpcNames bNpcNames,
    DictBNpc bNpc,
    HumanModelList humans)
{
    private readonly WorldCombo    _worldCombo    = new(dictWorld);
    private readonly HumanNpcCombo _humanNpcCombo = new(bNpcNames, dictModelChara, humans, bNpc);

    private string _characterName = string.Empty;

    public ActorIdentifier NpcIdentifier       { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier PlayerIdentifier    { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier RetainerIdentifier  { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier MannequinIdentifier { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier OwnedIdentifier     { get; private set; } = ActorIdentifier.Invalid;

    public void DrawName(float width)
    {
        Im.Item.SetNextWidth(width);
        if (Im.Input.Text("##Name"u8, ref _characterName, "Character Name..."u8))
            UpdateIdentifiers();
    }

    public void DrawWorld(float width)
    {
        if (_worldCombo.Draw(width))
            UpdateIdentifiers();
    }

    public void DrawNpcs(float width)
    {
        if (_humanNpcCombo.Draw("##npcs"u8, width))
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
            PlayerIdentifier    = actors.CreatePlayer(byteName, _worldCombo.Selected.Key);
            RetainerIdentifier  = actors.CreateRetainer(byteName, ActorIdentifier.RetainerType.Bell);
            MannequinIdentifier = actors.CreateRetainer(byteName, ActorIdentifier.RetainerType.Mannequin);

            if (_humanNpcCombo.Selection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc)
                OwnedIdentifier = actors.CreateOwned(byteName, _worldCombo.Selected.Key, _humanNpcCombo.Selection.Kind,
                    _humanNpcCombo.Selection.Ids.First());
            else
                OwnedIdentifier = ActorIdentifier.Invalid;
        }

        NpcIdentifier = _humanNpcCombo.Selection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc
            ? actors.CreateNpc(_humanNpcCombo.Selection.Kind, _humanNpcCombo.Selection.Ids.First())
            : ActorIdentifier.Invalid;
    }
}
