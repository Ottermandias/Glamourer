using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.ByteString;

namespace Glamourer;

public unsafe struct Actor : IEquatable<Actor>
{
    public interface IIdentifier : IEquatable<IIdentifier>
    {
        Utf8String Name { get; }

        public IIdentifier CreatePermanent();
    }

    public class InvalidIdentifier : IIdentifier
    {
        public Utf8String Name
            => Utf8String.Empty;

        public bool Equals(IIdentifier? other)
            => false;

        public override int GetHashCode()
            => 0;

        public override string ToString()
            => "Invalid";

        public IIdentifier CreatePermanent()
            => this;
    }

    public class PlayerIdentifier : IIdentifier
    {
        public          Utf8String Name { get; }
        public readonly ushort     HomeWorld;

        public PlayerIdentifier(Utf8String name, ushort homeWorld)
        {
            Name      = name;
            HomeWorld = homeWorld;
        }

        public bool Equals(IIdentifier? other)
            => other is PlayerIdentifier p && p.HomeWorld == HomeWorld && p.Name.Equals(Name);

        public override int GetHashCode()
            => HashCode.Combine(Name.Crc32, HomeWorld);

        public override string ToString()
            => $"{Name} ({HomeWorld})";

        public IIdentifier CreatePermanent()
            => new PlayerIdentifier(Name.Clone(), HomeWorld);
    }

    public class OwnedIdentifier : IIdentifier
    {
        public          Utf8String Name { get; }
        public readonly Utf8String OwnerName;
        public readonly uint       DataId;
        public readonly ushort     OwnerHomeWorld;
        public readonly ObjectKind Kind;

        public OwnedIdentifier(Utf8String name, Utf8String ownerName, ushort ownerHomeWorld, uint dataId, ObjectKind kind)
        {
            Name           = name;
            OwnerName      = ownerName;
            OwnerHomeWorld = ownerHomeWorld;
            DataId         = dataId;
            Kind           = kind;
        }

        public bool Equals(IIdentifier? other)
            => other is OwnedIdentifier p
             && p.DataId == DataId
             && p.OwnerHomeWorld == OwnerHomeWorld
             && p.Kind == Kind
             && p.OwnerName.Equals(OwnerName);

        public override int GetHashCode()
            => HashCode.Combine(OwnerName.Crc32, OwnerHomeWorld, DataId, Kind);

        public override string ToString()
            => $"{OwnerName}s {Name}";

        public IIdentifier CreatePermanent()
            => new OwnedIdentifier(Name.Clone(), OwnerName.Clone(), OwnerHomeWorld, DataId, Kind);
    }

    public class NpcIdentifier : IIdentifier
    {
        public          Utf8String Name { get; }
        public readonly uint       DataId;
        public readonly ushort     ObjectIndex;

        public NpcIdentifier(Utf8String actorName, ushort objectIndex = ushort.MaxValue, uint dataId = uint.MaxValue)
        {
            Name        = actorName;
            ObjectIndex = objectIndex;
            DataId      = dataId;
        }

        public bool Equals(IIdentifier? other)
            => other is NpcIdentifier p
             && p.Name.Equals(Name)
             && (p.DataId == uint.MaxValue || DataId == uint.MaxValue || p.DataId == DataId)
             && (p.ObjectIndex == ushort.MaxValue || ObjectIndex == ushort.MaxValue || p.ObjectIndex == ObjectIndex);

        public override int GetHashCode()
            => Name.Crc32;

        public override string ToString()
            => DataId == uint.MaxValue         ? ObjectIndex == ushort.MaxValue ? Name.ToString() : $"{Name} at {ObjectIndex}" :
                ObjectIndex == ushort.MaxValue ? $"{Name} ({DataId})" : $"{Name} ({DataId}) at {ObjectIndex}";

        public IIdentifier CreatePermanent()
            => new NpcIdentifier(Name.Clone(), ObjectIndex, DataId);
    }


    public static readonly Actor Null = new() { Pointer = null };

    public FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Pointer;

    public IntPtr Address
        => (IntPtr)Pointer;

    public static implicit operator Actor(IntPtr? pointer)
        => new() { Pointer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)pointer.GetValueOrDefault(IntPtr.Zero) };

    public static implicit operator IntPtr(Actor actor)
        => actor.Pointer == null ? IntPtr.Zero : (IntPtr)actor.Pointer;

    public IIdentifier GetIdentifier()
        => CreateIdentifier(this);

    public Character? Character
        => Pointer == null ? null : Dalamud.Objects[Pointer->GameObject.ObjectIndex] as Character;

    public bool IsAvailable
        => Pointer->GameObject.GetIsTargetable();

    public bool IsHuman
        => Pointer != null && Pointer->ModelCharaId == 0;

    public ref int ModelId
        => ref Pointer->ModelCharaId;

    public ObjectKind ObjectKind
    {
        get => (ObjectKind) Pointer->GameObject.ObjectKind;
        set => Pointer->GameObject.ObjectKind = (byte)value;
    }

    public Utf8String Utf8Name
        => new(Pointer->GameObject.Name);

    public Human* DrawObject
        => (Human*)Pointer->GameObject.DrawObject;

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

    private static IIdentifier CreateIdentifier(Actor actor)
    {
        switch (actor.ObjectKind)
        {
            case ObjectKind.Player: return new PlayerIdentifier(actor.Utf8Name, actor.Pointer->HomeWorld);

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
