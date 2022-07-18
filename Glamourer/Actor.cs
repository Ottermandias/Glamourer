using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.ByteString;

namespace Glamourer;

public unsafe struct Actor : IEquatable<Actor>
{
    public record struct Identifier(Utf8String Name, uint Id, ushort HomeWorld, ushort Index);

    public static readonly Actor Null = new() { Pointer = null };

    public FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Pointer;

    public IntPtr Address
        => (IntPtr)Pointer;

    public static implicit operator Actor(IntPtr? pointer)
        => new() { Pointer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)pointer.GetValueOrDefault(IntPtr.Zero) };

    public static implicit operator IntPtr(Actor actor)
        => actor.Pointer == null ? IntPtr.Zero : (IntPtr)actor.Pointer;

    public Identifier GetIdentifier()
    {
        if (Pointer == null)
            return new Identifier(Utf8String.Empty, 0, 0, 0);

        return new Identifier(Utf8Name, Pointer->GameObject.ObjectID, Pointer->HomeWorld, Pointer->GameObject.ObjectIndex);
    }

    public Character? Character
        => Pointer == null ? null : Dalamud.Objects[Pointer->GameObject.ObjectIndex] as Character;

    public bool IsAvailable
        => Pointer->GameObject.GetIsTargetable();

    public bool IsHuman
        => Pointer != null && Pointer->ModelCharaId == 0;

    public int ModelId
        => Pointer->ModelCharaId;

    public ObjectKind ObjectKind
        => (ObjectKind)Pointer->GameObject.ObjectKind;

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
}
