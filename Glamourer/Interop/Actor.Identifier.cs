using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.ByteString;

namespace Glamourer.Interop;

public unsafe partial struct Actor
{
    public interface IIdentifier : IEquatable<IIdentifier>
    {
        Utf8String  Name    { get; }
        public bool IsValid { get; }

        public IIdentifier CreatePermanent();

        public static readonly InvalidIdentifier Invalid = new();

        public void ToJson(JsonTextWriter j);

        public static IIdentifier? FromJson(JObject j)
        {
            switch (j["Type"]?.Value<string>() ?? string.Empty)
            {
                case nameof(PlayerIdentifier):
                {
                    var name = j[nameof(Name)]?.Value<string>();
                    if (name.IsNullOrEmpty())
                        return null;

                    var serverId = j[nameof(PlayerIdentifier.HomeWorld)]?.Value<ushort>() ?? ushort.MaxValue;
                    return new PlayerIdentifier(Utf8String.FromStringUnsafe(name, false), serverId);
                }
                case nameof(SpecialIdentifier):
                {
                    var index = j[nameof(SpecialIdentifier.Index)]?.Value<ushort>() ?? ushort.MaxValue;
                    return new SpecialIdentifier(index);
                }
                case nameof(OwnedIdentifier):
                {
                    var name = j[nameof(Name)]?.Value<string>();
                    if (name.IsNullOrEmpty())
                        return null;

                    var ownerName = j[nameof(OwnedIdentifier.OwnerName)]?.Value<string>();
                    if (ownerName.IsNullOrEmpty())
                        return null;

                    var ownerHomeWorld = j[nameof(OwnedIdentifier.OwnerHomeWorld)]?.Value<ushort>() ?? ushort.MaxValue;
                    var dataId         = j[nameof(OwnedIdentifier.DataId)]?.Value<ushort>() ?? ushort.MaxValue;
                    var kind           = j[nameof(OwnedIdentifier.Kind)]?.Value<ObjectKind>() ?? ObjectKind.Player;

                    return new OwnedIdentifier(Utf8String.FromStringUnsafe(name, false), Utf8String.FromStringUnsafe(ownerName, false),
                        ownerHomeWorld, dataId, kind);
                }
                case nameof(NpcIdentifier):
                {
                    var name = j[nameof(Name)]?.Value<string>();
                    if (name.IsNullOrEmpty())
                        return null;

                    var dataId = j[nameof(NpcIdentifier.DataId)]?.Value<uint>() ?? uint.MaxValue;

                    return new NpcIdentifier(Utf8String.FromStringUnsafe(name, false), ushort.MaxValue, dataId);
                }
                default: return null;
            }
        }
    }

    public class InvalidIdentifier : IIdentifier
    {
        public Utf8String Name
            => Utf8String.Empty;

        public bool IsValid
            => false;

        public bool Equals(IIdentifier? other)
            => false;

        public override int GetHashCode()
            => 0;

        public override string ToString()
            => "Invalid";

        public IIdentifier CreatePermanent()
            => this;

        public void ToJson(JsonTextWriter j)
        { }
    }

    public class PlayerIdentifier : IIdentifier, IEquatable<PlayerIdentifier>
    {
        public          Utf8String Name { get; }
        public readonly ushort     HomeWorld;

        public bool IsValid
            => true;

        public PlayerIdentifier(Utf8String name, ushort homeWorld)
        {
            Name      = name;
            HomeWorld = homeWorld;
        }

        public bool Equals(IIdentifier? other)
            => Equals(other as PlayerIdentifier);

        public bool Equals(PlayerIdentifier? other)
            => other?.HomeWorld == HomeWorld && other.Name.Equals(Name);

        public override int GetHashCode()
            => HashCode.Combine(Name.Crc32, HomeWorld);

        public override string ToString()
            => $"{Name} ({HomeWorld})";

        public IIdentifier CreatePermanent()
            => new PlayerIdentifier(Name.Clone(), HomeWorld);

        public void ToJson(JsonTextWriter j)
        {
            j.WriteStartObject();
            j.WritePropertyName("Type");
            j.WriteValue(GetType().Name);
            j.WritePropertyName(nameof(Name));
            j.WriteValue(Name);
            j.WritePropertyName(nameof(HomeWorld));
            j.WriteValue(HomeWorld);
            j.WriteEndObject();
        }
    }

    public class SpecialIdentifier : IIdentifier, IEquatable<SpecialIdentifier>
    {
        public Utf8String Name
            => Utf8String.Empty;

        public readonly ushort Index;

        public bool IsValid
            => true;

        public SpecialIdentifier(ushort index)
            => Index = index;

        public bool Equals(IIdentifier? other)
            => Equals(other as SpecialIdentifier);

        public bool Equals(SpecialIdentifier? other)
            => other?.Index == Index;

        public override int GetHashCode()
            => Index;

        public override string ToString()
            => $"Special Actor {Index}";

        public IIdentifier CreatePermanent()
            => this;

        public void ToJson(JsonTextWriter j)
        {
            j.WriteStartObject();
            j.WritePropertyName("Type");
            j.WriteValue(GetType().Name);
            j.WritePropertyName(nameof(Index));
            j.WriteValue(Index);
            j.WriteEndObject();
        }
    }


    public class OwnedIdentifier : IIdentifier, IEquatable<OwnedIdentifier>
    {
        public          Utf8String Name { get; }
        public readonly Utf8String OwnerName;
        public readonly uint       DataId;
        public readonly ushort     OwnerHomeWorld;
        public readonly ObjectKind Kind;

        public bool IsValid
            => true;

        public OwnedIdentifier(Utf8String name, Utf8String ownerName, ushort ownerHomeWorld, uint dataId, ObjectKind kind)
        {
            Name           = name;
            OwnerName      = ownerName;
            OwnerHomeWorld = ownerHomeWorld;
            DataId         = dataId;
            Kind           = kind;
        }

        public bool Equals(IIdentifier? other)
            => Equals(other as OwnedIdentifier);

        public bool Equals(OwnedIdentifier? other)
            => other?.DataId == DataId
             && other.OwnerHomeWorld == OwnerHomeWorld
             && other.Kind == Kind
             && other.OwnerName.Equals(OwnerName);


        public override int GetHashCode()
            => HashCode.Combine(OwnerName.Crc32, OwnerHomeWorld, DataId, Kind);

        public override string ToString()
            => $"{OwnerName}s {Name}";

        public IIdentifier CreatePermanent()
            => new OwnedIdentifier(Name.Clone(), OwnerName.Clone(), OwnerHomeWorld, DataId, Kind);

        public void ToJson(JsonTextWriter j)
        {
            j.WriteStartObject();
            j.WritePropertyName("Type");
            j.WriteValue(GetType().Name);
            j.WritePropertyName(nameof(Name));
            j.WriteValue(Name);
            j.WritePropertyName(nameof(OwnerName));
            j.WriteValue(OwnerName);
            j.WritePropertyName(nameof(OwnerHomeWorld));
            j.WriteValue(OwnerHomeWorld);
            j.WritePropertyName(nameof(Kind));
            j.WriteValue(Kind);
            j.WritePropertyName(nameof(DataId));
            j.WriteValue(DataId);
            j.WriteEndObject();
        }
    }

    public class NpcIdentifier : IIdentifier, IEquatable<NpcIdentifier>
    {
        public          Utf8String Name { get; }
        public readonly uint       DataId;
        public readonly ushort     ObjectIndex;

        public bool IsValid
            => true;

        public NpcIdentifier(Utf8String actorName, ushort objectIndex = ushort.MaxValue, uint dataId = uint.MaxValue)
        {
            Name        = actorName;
            ObjectIndex = objectIndex;
            DataId      = dataId;
        }

        public bool Equals(IIdentifier? other)
            => Equals(other as NpcIdentifier);

        public bool Equals(NpcIdentifier? other)
            => (other?.Name.Equals(Name) ?? false)
             && (other.DataId == uint.MaxValue || DataId == uint.MaxValue || other.DataId == DataId)
             && (other.ObjectIndex == ushort.MaxValue || ObjectIndex == ushort.MaxValue || other.ObjectIndex == ObjectIndex);

        public override int GetHashCode()
            => Name.Crc32;

        public override string ToString()
            => DataId == uint.MaxValue         ? ObjectIndex == ushort.MaxValue ? Name.ToString() : $"{Name} at {ObjectIndex}" :
                ObjectIndex == ushort.MaxValue ? $"{Name} ({DataId})" : $"{Name} ({DataId}) at {ObjectIndex}";

        public IIdentifier CreatePermanent()
            => new NpcIdentifier(Name.Clone(), ObjectIndex, DataId);

        public void ToJson(JsonTextWriter j)
        {
            j.WriteStartObject();
            j.WritePropertyName("Type");
            j.WriteValue(GetType().Name);
            j.WritePropertyName(nameof(Name));
            j.WriteValue(Name);
            j.WritePropertyName(nameof(DataId));
            j.WriteValue(DataId);
            j.WriteEndObject();
        }
    }

    private static IIdentifier CreateIdentifier(Actor actor)
    {
        if (!actor.Valid)
            return IIdentifier.Invalid;

        var objectIdx = actor.Pointer->GameObject.ObjectIndex;
        if (objectIdx is >= 200 and < 240)
        {
            var parentIdx = Glamourer.Penumbra.CutsceneParent(objectIdx);
            if (parentIdx >= 0)
            {
                var parent = (Actor)Dalamud.Objects.GetObjectAddress(parentIdx);
                if (!parent)
                    return IIdentifier.Invalid;

                return CreateIdentifier(parent);
            }
        }

        switch (actor.ObjectKind)
        {
            case ObjectKind.Player:
            {
                var name = actor.Utf8Name;
                if (name.Length > 0 && actor.Pointer->HomeWorld is > 0 and < ushort.MaxValue)
                    return new PlayerIdentifier(actor.Utf8Name, actor.Pointer->HomeWorld);

                return IIdentifier.Invalid;
            }
            case ObjectKind.BattleNpc:
            {
                var ownerId = actor.Pointer->GameObject.OwnerID;
                if (ownerId != 0xE0000000)
                {
                    var owner = (Actor)Dalamud.Objects.SearchById(ownerId)?.Address;
                    if (!owner)
                        return new InvalidIdentifier();

                    return new OwnedIdentifier(actor.Utf8Name, owner.Utf8Name, owner.Pointer->HomeWorld,
                        actor.Pointer->GameObject.DataID, ObjectKind.BattleNpc);
                }

                return new NpcIdentifier(actor.Utf8Name, actor.Pointer->GameObject.ObjectIndex,
                    actor.Pointer->GameObject.DataID);
            }
            case ObjectKind.Retainer:
            case ObjectKind.EventNpc:
                return new NpcIdentifier(actor.Utf8Name, actor.Pointer->GameObject.ObjectIndex,
                    actor.Pointer->GameObject.DataID);
            case ObjectKind.MountType:
            case ObjectKind.Companion:
            {
                var idx = actor.Pointer->GameObject.ObjectIndex;
                if (idx % 2 == 0)
                    return new InvalidIdentifier();

                var owner = (Actor)Dalamud.Objects[idx - 1]?.Address;
                if (!owner)
                    return new InvalidIdentifier();

                return new OwnedIdentifier(actor.Utf8Name, owner.Utf8Name, owner.Pointer->HomeWorld,
                    actor.Pointer->GameObject.DataID, actor.ObjectKind);
            }
            default: return new InvalidIdentifier();
        }
    }
}
