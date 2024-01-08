using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Penumbra.GameData.Structs;

namespace Glamourer.GameData;

/// <summary> A struct containing everything to replicate the appearance of a human NPC. </summary>
public unsafe struct NpcData
{
    /// <summary> The name of the NPC. </summary>
    public string Name;

    /// <summary> The customizations of the NPC. </summary>
    public CustomizeArray Customize;

    /// <summary> The equipment appearance of the NPC, 10 * CharacterArmor. </summary>
    private fixed byte _equip[40];

    /// <summary> The mainhand weapon appearance of the NPC. </summary>
    public CharacterWeapon Mainhand;

    /// <summary> The offhand weapon appearance of the NPC. </summary>
    public CharacterWeapon Offhand;

    /// <summary> The data ID of the NPC, either event NPC or battle NPC name. </summary>
    public NpcId Id;

    /// <summary> Whether the NPCs visor is toggled. </summary>
    public bool VisorToggled;

    /// <summary> Whether the NPC is an event NPC or a battle NPC. </summary>
    public ObjectKind Kind;

    /// <summary> Obtain the equipment as CharacterArmors. </summary>
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

    /// <summary> Write all the gear appearance to a single string. </summary>
    public string WriteGear()
    {
        var sb   = new StringBuilder(128);
        var span = Equip;
        for (var i = 0; i < 10; ++i)
        {
            sb.Append(span[i].Set.Id.ToString("D4"))
                .Append('-')
                .Append(span[i].Variant.Id.ToString("D3"))
                .Append('-')
                .Append(span[i].Stain.Id.ToString("D3"))
                .Append(",  ");
        }

        sb.Append(Mainhand.Skeleton.Id.ToString("D4"))
            .Append('-')
            .Append(Mainhand.Weapon.Id.ToString("D4"))
            .Append('-')
            .Append(Mainhand.Variant.Id.ToString("D3"))
            .Append('-')
            .Append(Mainhand.Stain.Id.ToString("D4"))
            .Append(",  ")
            .Append(Offhand.Skeleton.Id.ToString("D4"))
            .Append('-')
            .Append(Offhand.Weapon.Id.ToString("D4"))
            .Append('-')
            .Append(Offhand.Variant.Id.ToString("D3"))
            .Append('-')
            .Append(Offhand.Stain.Id.ToString("D3"));
        return sb.ToString();
    }

    /// <summary> Set an equipment piece to a given value. </summary>
    internal void Set(int idx, uint value)
    {
        fixed (byte* ptr = _equip)
        {
            ((uint*)ptr)[idx] = value;
        }
    }

    /// <summary> Check if the appearance data, excluding ID and Name, of two NpcData is equal. </summary>
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
