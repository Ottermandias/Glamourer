using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.GameData.ByteString;

namespace Glamourer;

public static class ObjectManager
{
    private const int GPosePlayerIndex     = 201;
    private const int CharacterScreenIndex = 240;
    private const int ExamineScreenIndex   = 241;
    private const int FittingRoomIndex     = 242;
    private const int DyePreviewIndex      = 243;

    private static readonly Dictionary<Utf8String, int>         NameCounters = new();
    private static readonly Dictionary<Actor.Identifier, Actor> GPoseActors  = new(CharacterScreenIndex - GPosePlayerIndex);

    public static bool IsInGPose()
        => Dalamud.Objects[GPosePlayerIndex] != null;

    public static Actor GPosePlayer
        => Dalamud.Objects[GPosePlayerIndex]?.Address;

    public static Actor Player
        => Dalamud.ClientState.LocalPlayer?.Address;

    public record struct ActorData(string Label, Actor.Identifier Identifier, Actor Actor, bool Modifiable, Actor GPose);

    public static IEnumerable<ActorData> GetEnumerator()
    {
        NameCounters.Clear();
        GPoseActors.Clear();
        for (var i = GPosePlayerIndex; i < CharacterScreenIndex; ++i)
        {
            Actor character = Dalamud.Objects[i]?.Address;
            if (!character)
                break;

            var identifier = character.GetIdentifier();
            GPoseActors[identifier] = character.Address;
            yield return new ActorData(GetLabel(character, identifier.Name.ToString(), 0, true), identifier, character.Address, true,
                Actor.Null);
        }

        Actor actor = Dalamud.Objects[CharacterScreenIndex]?.Address;
        if (actor)
            yield return new ActorData("Character Screen Actor", actor.GetIdentifier(), actor.Address, false, Actor.Null);

        actor = Dalamud.Objects[ExamineScreenIndex]?.Address;
        if (actor)
            yield return new ActorData("Examine Screen Actor", actor.GetIdentifier(), actor.Address, false, Actor.Null);

        actor = Dalamud.Objects[FittingRoomIndex]?.Address;
        if (actor)
            yield return new ActorData("Fitting Room Actor", actor.GetIdentifier(), actor.Address, false, Actor.Null);

        actor = Dalamud.Objects[DyePreviewIndex]?.Address;
        if (actor)
            yield return new ActorData("Dye Preview Actor", actor.GetIdentifier(), actor.Address, false, Actor.Null);

        for (var i = 0; i < GPosePlayerIndex; ++i)
        {
            actor = Dalamud.Objects[i]?.Address;
            if (!actor
             || actor.ObjectKind is not (ObjectKind.Player or ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Companion
                    or ObjectKind.Retainer))
                continue;

            var identifier = actor.GetIdentifier();
            if (identifier.Name.Length == 0)
                continue;

            if (NameCounters.TryGetValue(identifier.Name, out var num))
                NameCounters[identifier.Name] = ++num;
            else
                NameCounters[identifier.Name] = num = 1;

            if (!GPoseActors.TryGetValue(identifier, out var gPose))
                gPose = Actor.Null;

            yield return new ActorData(GetLabel(actor, identifier.Name.ToString(), num, false), identifier, actor.Address, true, gPose);
        }

        for (var i = DyePreviewIndex + 1; i < Dalamud.Objects.Length; ++i)
        {
            actor = Dalamud.Objects[i]?.Address;
            if (!actor
             || actor.ObjectKind is not (ObjectKind.Player or ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Companion
                    or ObjectKind.Retainer))
                continue;

            var identifier = actor.GetIdentifier();
            if (identifier.Name.Length == 0)
                continue;

            if (NameCounters.TryGetValue(identifier.Name, out var num))
                NameCounters[identifier.Name] = ++num;
            else
                NameCounters[identifier.Name] = num = 1;

            if (!GPoseActors.TryGetValue(identifier, out var gPose))
                gPose = Actor.Null;

            yield return new ActorData(GetLabel(actor, identifier.Name.ToString(), num, false), identifier, actor.Address, true, gPose);
        }
    }

    private static unsafe string GetLabel(Actor player, string playerName, int num, bool gPose)
    {
        if (player.ObjectKind == ObjectKind.Player)
            return gPose ? $"{playerName} (GPose)" : num == 1 ? playerName : $"{playerName} #{num}";

        if (((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player!.Address)->ModelCharaId == 0)
            return gPose ? $"{playerName} (GPose, NPC)" : num == 1 ? $"{playerName} (NPC)" : $"{playerName} #{num} (NPC)";

        return gPose ? $"{playerName} (GPose, Monster)" : num == 1 ? $"{playerName} (Monster)" : $"{playerName} #{num} (Monster)";
    }
}
