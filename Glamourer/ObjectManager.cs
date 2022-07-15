using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace Glamourer;

public unsafe struct Actor : IEquatable<Actor>
{
    public static readonly Actor Null = new() { Pointer = null };

    public FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Pointer;

    public IntPtr Address
        => (IntPtr)Pointer;

    public static implicit operator Actor(IntPtr? pointer)
        => new() { Pointer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)pointer.GetValueOrDefault(IntPtr.Zero) };

    public static implicit operator IntPtr(Actor actor)
        => actor.Pointer == null ? IntPtr.Zero : (IntPtr)actor.Pointer;

    public Character? Character
        => Pointer == null ? null : Dalamud.Objects[Pointer->GameObject.ObjectIndex] as Character;

    public bool IsAvailable
        => Pointer->GameObject.GetIsTargetable();

    public bool IsHuman
        => Pointer != null && Pointer->ModelCharaId == 0;

    public int ModelId
        => Pointer != null ? Pointer->ModelCharaId : 0;

    public void SetModelId(int value)
    {
        if (Pointer != null)
            Pointer->ModelCharaId = value;
    }

    public static implicit operator bool(Actor actor)
        => actor.Pointer != null;

    public static bool operator true(Actor actor)
        => actor.Pointer != null;

    public static bool operator false(Actor actor)
        => actor.Pointer == null;

    public static bool operator !(Actor actor)
        => actor.Pointer == null;

    public bool Equals(Actor other)
        => Pointer == other.Pointer;

    public override bool Equals(object? obj)
        => obj is Actor other && Equals(other);

    public override int GetHashCode()
        => ((ulong)Pointer).GetHashCode();

    public static bool operator ==(Actor lhs, Actor rhs)
        => lhs.Pointer == rhs.Pointer;

    public static bool operator !=(Actor lhs, Actor rhs)
        => lhs.Pointer != rhs.Pointer;
}

public static class ObjectManager
{
    private const           int                       GPosePlayerIndex     = 201;
    private const           int                       CharacterScreenIndex = 240;
    private const           int                       ExamineScreenIndex   = 241;
    private const           int                       FittingRoomIndex     = 242;
    private const           int                       DyePreviewIndex      = 243;
    private static readonly Dictionary<string, int>   _nameCounters        = new();
    private static readonly Dictionary<string, Actor> _gPoseActors         = new(CharacterScreenIndex - GPosePlayerIndex);

    public static bool IsInGPose()
        => Dalamud.Objects[GPosePlayerIndex] != null;

    public static Actor GPosePlayer
        => Dalamud.Objects[GPosePlayerIndex]?.Address;

    public static Actor Player
        => Dalamud.ClientState.LocalPlayer?.Address;

    public record struct ActorData(string Label, string Name, Actor Actor, bool Modifiable, Actor GPose);

    public static IEnumerable<ActorData> GetEnumerator()
    {
        _nameCounters.Clear();
        _gPoseActors.Clear();
        for (var i = GPosePlayerIndex; i < CharacterScreenIndex; ++i)
        {
            var character = Dalamud.Objects[i];
            if (character == null)
                break;

            var name = character.Name.TextValue;
            _gPoseActors[name] = character.Address;
            yield return new ActorData(GetLabel(character, name, 0, true), name, character.Address, true, Actor.Null);
        }

        var actor = Dalamud.Objects[CharacterScreenIndex];
        if (actor != null)
            yield return new ActorData("Character Screen Actor", string.Empty, actor.Address, false, Actor.Null);

        actor = Dalamud.Objects[ExamineScreenIndex];
        if (actor != null)
            yield return new ActorData("Examine Screen Actor", string.Empty, actor.Address, false, Actor.Null);

        actor = Dalamud.Objects[FittingRoomIndex];
        if (actor != null)
            yield return new ActorData("Fitting Room Actor", string.Empty, actor.Address, false, Actor.Null);

        actor = Dalamud.Objects[DyePreviewIndex];
        if (actor != null)
            yield return new ActorData("Dye Preview Actor", string.Empty, actor.Address, false, Actor.Null);

        for (var i = 0; i < GPosePlayerIndex; ++i)
        {
            var character = Dalamud.Objects[i];
            if (character == null
             || character.ObjectKind is not (ObjectKind.Player or ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Companion
                    or ObjectKind.Retainer))
                continue;

            var name = character.Name.TextValue;
            if (name.Length == 0)
                continue;

            if (_nameCounters.TryGetValue(name, out var num))
                _nameCounters[name] = ++num;
            else
                _nameCounters[name] = num = 1;

            if (!_gPoseActors.TryGetValue(name, out var gPose))
                gPose = Actor.Null;

            yield return new ActorData(GetLabel(character, name, num, false), name, character.Address, true, gPose);
        }

        for (var i = DyePreviewIndex + 1; i < Dalamud.Objects.Length; ++i)
        {
            var character = Dalamud.Objects[i];
            if (character == null
             || !((Actor)character.Address).IsAvailable
             || character.ObjectKind is not (ObjectKind.Player or ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Companion
                    or ObjectKind.Retainer))
                continue;

            var name = character.Name.TextValue;
            if (name.Length == 0)
                continue;

            if (_nameCounters.TryGetValue(name, out var num))
                _nameCounters[name] = ++num;
            else
                _nameCounters[name] = num = 1;

            if (!_gPoseActors.TryGetValue(name, out var gPose))
                gPose = Actor.Null;

            yield return new ActorData(GetLabel(character, name, num, false), name, character.Address, true, gPose);
        }
    }

    private static unsafe string GetLabel(GameObject player, string playerName, int num, bool gPose)
    {
        if (player.ObjectKind == ObjectKind.Player)
            return gPose ? $"{playerName} (GPose)" : num == 1 ? playerName : $"{playerName} #{num}";

        if (((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player!.Address)->ModelCharaId == 0)
            return gPose ? $"{playerName} (GPose, NPC)" : num == 1 ? $"{playerName} (NPC)" : $"{playerName} #{num} (NPC)";

        return gPose ? $"{playerName} (GPose, Monster)" : num == 1 ? $"{playerName} (Monster)" : $"{playerName} #{num} (Monster)";
    }
}
