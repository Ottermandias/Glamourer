using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Penumbra.GameData.Structs;

namespace Glamourer.Customization;

public unsafe struct NpcData
{
    public        string          Name;
    public        Customize       Customize;
    private fixed byte            _equip[40];
    public        CharacterWeapon Mainhand;
    public        CharacterWeapon Offhand;
    public        NpcId           Id;
    public        bool            VisorToggled;
    public        ObjectKind      Kind;

    public ReadOnlySpan<CharacterArmor> Equip
    {
        get
        {
            fixed (byte* ptr = _equip)
            {
                return new ReadOnlySpan<CharacterArmor>((CharacterArmor*)ptr, 10);
            }
        }
    }

    public string WriteGear()
    {
        var sb   = new StringBuilder(128);
        var span = Equip;
        for (var i = 0; i < 10; ++i)
        {
            sb.Append(span[i].Set.Id.ToString("D4"));
            sb.Append('-');
            sb.Append(span[i].Variant.Id.ToString("D3"));
            sb.Append('-');
            sb.Append(span[i].Stain.Id.ToString("D3"));
            sb.Append(",  ");
        }

        sb.Append(Mainhand.Skeleton.Id.ToString("D4"));
        sb.Append('-');
        sb.Append(Mainhand.Weapon.Id.ToString("D4"));
        sb.Append('-');
        sb.Append(Mainhand.Variant.Id.ToString("D3"));
        sb.Append('-');
        sb.Append(Mainhand.Stain.Id.ToString("D4"));
        sb.Append(",  ");
        sb.Append(Offhand.Skeleton.Id.ToString("D4"));
        sb.Append('-');
        sb.Append(Offhand.Weapon.Id.ToString("D4"));
        sb.Append('-');
        sb.Append(Offhand.Variant.Id.ToString("D3"));
        sb.Append('-');
        sb.Append(Offhand.Stain.Id.ToString("D3"));
        return sb.ToString();
    }

    internal void Set(int idx, uint value)
    {
        fixed (byte* ptr = _equip)
        {
            ((uint*)ptr)[idx] = value;
        }
    }

    public bool DataEquals(in NpcData other)
    {
        if (VisorToggled != other.VisorToggled)
            return false;

        if (!Customize.Equals(other.Customize))
            return false;

        if (!Mainhand.Equals(other.Mainhand))
            return false;

        if (!Offhand.Equals(other.Offhand))
            return false;

        fixed (byte* ptr1 = _equip, ptr2 = other._equip)
        {
            return new ReadOnlySpan<byte>(ptr1, 40).SequenceEqual(new ReadOnlySpan<byte>(ptr2, 40));
        }
    }
}
