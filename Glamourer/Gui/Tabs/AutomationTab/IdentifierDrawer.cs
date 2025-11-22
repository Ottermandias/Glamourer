using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Structs;
using Penumbra.String;
using Penumbra.GameData.Enums;

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

    private static bool IsWildcardPattern(string? name)
        => !string.IsNullOrEmpty(name) && name.Contains('*');

    private static T Create<T>(bool wildcard, Func<T> uncheckedFactory, Func<T> checkedFactory)
        => wildcard ? uncheckedFactory() : checkedFactory();

    private void UpdateIdentifiers()
    {
        var isWildcard = IsWildcardPattern(_characterName);
        ByteString byteName = default;
        
        // For wildcard patterns, use FromStringUnsafe to allow '*' characters
        if (isWildcard)
            byteName = ByteString.FromStringUnsafe(_characterName ?? string.Empty, false);
        else if (!ByteString.FromString(_characterName, out byteName))
        {
            PlayerIdentifier    = ActorIdentifier.Invalid;
            RetainerIdentifier  = ActorIdentifier.Invalid;
            MannequinIdentifier = ActorIdentifier.Invalid;
            OwnedIdentifier     = ActorIdentifier.Invalid;
            NpcIdentifier       = ActorIdentifier.Invalid;
            return;
        }

        // Create identifiers using a single helper to handle wildcard vs checked creation
        PlayerIdentifier = Create(isWildcard,
            () => _actors.CreatePlayerUnchecked(byteName, _worldCombo.CurrentSelection.Key),
            () => _actors.CreatePlayer(byteName, _worldCombo.CurrentSelection.Key));

        RetainerIdentifier = Create(isWildcard,
            () => _actors.CreateRetainerUnchecked(byteName, ActorIdentifier.RetainerType.Bell),
            () => _actors.CreateRetainer(byteName, ActorIdentifier.RetainerType.Bell));

        MannequinIdentifier = Create(isWildcard,
            () => _actors.CreateRetainerUnchecked(byteName, ActorIdentifier.RetainerType.Mannequin),
            () => _actors.CreateRetainer(byteName, ActorIdentifier.RetainerType.Mannequin));

        if (_humanNpcCombo.CurrentSelection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc)
            OwnedIdentifier = Create(isWildcard,
                () => _actors.CreateIndividualUnchecked(IdentifierType.Owned, byteName, _worldCombo.CurrentSelection.Key.Id,
                    _humanNpcCombo.CurrentSelection.Kind, _humanNpcCombo.CurrentSelection.Ids[0]),
                () => _actors.CreateOwned(byteName, _worldCombo.CurrentSelection.Key, _humanNpcCombo.CurrentSelection.Kind,
                    _humanNpcCombo.CurrentSelection.Ids[0]));
        else
            OwnedIdentifier = ActorIdentifier.Invalid;

        NpcIdentifier = _humanNpcCombo.CurrentSelection.Kind is ObjectKind.EventNpc or ObjectKind.BattleNpc
            ? _actors.CreateNpc(_humanNpcCombo.CurrentSelection.Kind, _humanNpcCombo.CurrentSelection.Ids[0])
            : ActorIdentifier.Invalid;
    }
}
