using Dalamud.Game.ClientState.Objects.Enums;
using Glamourer.Automation;
using ImSharp;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.AutomationTab;

public readonly struct AutomationCacheItem(AutoDesignSet set, int index)
{
    public readonly AutoDesignSet Set                 = set;
    public readonly int           Index               = index;
    public readonly StringPair    Name                = new(set.Name);
    public readonly StringPair    IdentifierString    = GetIdentifierPair(set.Identifiers.First());
    public readonly StringU8      Incognito           = new($"Auto Design Set #{index + 1}");
    public readonly StringU8      IdentifierIncognito = GetIncognito(set.Identifiers.First());

    private static StringPair GetIdentifierPair(ActorIdentifier identifier)
        => identifier switch
        {
            { Type: IdentifierType.Npc, Kind: ObjectKind.BattleNpc } => new StringPair($"{identifier.ToName()} (BNPC)"),
            { Type: IdentifierType.Npc, Kind: ObjectKind.EventNpc }  => new StringPair($"{identifier.ToName()} (ENPC)"),
            _                                                        => new StringPair(identifier.ToString()),
        };

    private static StringU8 GetIncognito(ActorIdentifier identifier)
        => identifier switch
        {
            { Type: IdentifierType.Npc, Kind: ObjectKind.BattleNpc } => new StringU8($"{identifier.ToName()} (BNPC)"),
            { Type: IdentifierType.Npc, Kind: ObjectKind.EventNpc }  => new StringU8($"{identifier.ToName()} (ENPC)"),
            _                                                        => new StringU8(identifier.Incognito(null)),
        };
}
