using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Glamourer.Customization;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;
using Penumbra.String;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

public unsafe partial struct Actor : IEquatable<Actor>, IDesignable
{
    public static readonly Actor Null = new() { Pointer = null };

    public FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Pointer;

    public IntPtr Address
        => (IntPtr)Pointer;

    public static implicit operator Actor(IntPtr? pointer)
        => new() { Pointer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)(pointer ?? IntPtr.Zero) };

    public static implicit operator IntPtr(Actor actor)
        => actor.Pointer == null ? IntPtr.Zero : (IntPtr)actor.Pointer;

    public ActorIdentifier GetIdentifier(ActorManager actors)
        => actors.FromObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)Pointer, out _, true, true, false);

    public bool Identifier(ActorManager actors, out ActorIdentifier ident)
    {
        if (Valid)
        {
            ident = GetIdentifier(actors);
            return true;
        }

        ident = ActorIdentifier.Invalid;
        return false;
    }

    public override string ToString()
        => Pointer != null ? Utf8Name.ToString() : "Invalid";

    public bool IsAvailable
        => Pointer->GameObject.GetIsTargetable();

    public bool IsHuman
        => Pointer != null && Pointer->ModelCharaId == 0;

    public ObjectKind ObjectKind
    {
        get => (ObjectKind)Pointer->GameObject.ObjectKind;
        set => Pointer->GameObject.ObjectKind = (byte)value;
    }

    public ByteString Utf8Name
        => new(Pointer->GameObject.Name);

    public byte Job
        => Pointer->ClassJob;

    public DrawObject DrawObject
        => (IntPtr)Pointer->GameObject.DrawObject;

    public bool Valid
        => Pointer != null;

    public int Index
        => Pointer->GameObject.ObjectIndex;

    public uint ModelId
    {
        get => (uint)Pointer->ModelCharaId;
        set => Pointer->ModelCharaId = (int)value;
    }

    public ushort UsedMountId
        => !IsHuman ? (ushort)0 : *(ushort*)((byte*)Pointer + 0x668);

    public ushort CompanionId
        => ObjectKind == ObjectKind.Companion ? *(ushort*)((byte*)Pointer + 0x1AAC) : (ushort)0;

    public Customize Customize
        => new(*(CustomizeData*)&Pointer->DrawData.CustomizeData);

    public CharacterEquip Equip
        => new((CharacterArmor*)&Pointer->DrawData.Head);

    public CharacterWeapon MainHand
    {
        get => *(CharacterWeapon*)&Pointer->DrawData.MainHandModel;
        set => *(CharacterWeapon*)&Pointer->DrawData.MainHandModel = value;
    }

    public CharacterWeapon OffHand
    {
        get => *(CharacterWeapon*)&Pointer->DrawData.OffHandModel;
        set => *(CharacterWeapon*)&Pointer->DrawData.OffHandModel = value;
    }

    public unsafe bool VisorEnabled
    {
        get => (*(byte*)(Address + Offsets.Character.VisorToggled) & Offsets.Character.Flags.IsVisorToggled) != 0;
        set => *(byte*)(Address + Offsets.Character.VisorToggled) = (byte)(value
            ? *(byte*)(Address + Offsets.Character.VisorToggled) | Offsets.Character.Flags.IsVisorToggled
            : *(byte*)(Address + Offsets.Character.VisorToggled) & ~Offsets.Character.Flags.IsVisorToggled);
    }

    public unsafe bool WeaponEnabled
    {
        get => (*(byte*)(Address + Offsets.Character.WeaponHidden1) & Offsets.Character.Flags.IsWeaponHidden1) == 0;
        set
        {
            ref var w1 = ref *(byte*)(Address + Offsets.Character.WeaponHidden1);
            ref var w2 = ref *(byte*)(Address + Offsets.Character.WeaponHidden2);
            if (value)
            {
                w1 = (byte)(w1 & ~Offsets.Character.Flags.IsWeaponHidden1);
                w2 = (byte)(w2 & ~Offsets.Character.Flags.IsWeaponHidden2);
            }
            else
            {
                w1 = (byte)(w1 | Offsets.Character.Flags.IsWeaponHidden1);
                w2 = (byte)(w2 | Offsets.Character.Flags.IsWeaponHidden2);
            }
        }
    }

    public bool IsWet { get; set; }


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

    public string AddressString()
        => $"0x{Address:X}";
}
