using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Penumbra.GameData.Structs;

namespace Glamourer.GameData;

/// <summary> A struct containing everything to replicate the appearance of a human NPC. </summary>
public struct NpcData
{
    /// <summary> The name of the NPC. </summary>
    public string Name;

    /// <summary> The equipment appearance of the NPC, 10 * CharacterArmor. </summary>
    private EquipArray _equip;

    /// <summary> The mainhand weapon appearance of the NPC. </summary>
    public CharacterWeapon Mainhand;

    /// <summary> The offhand weapon appearance of the NPC. </summary>
    public CharacterWeapon Offhand;

    /// <summary> The customizations of the NPC. </summary>
    public CustomizeArray Customize;

    /// <summary> The data ID of the NPC, either event NPC or battle NPC name. </summary>
    public NpcId Id;

    /// <summary> The Model ID of the NPC. </summary>
    public uint ModelId;

    /// <summary> Whether the NPCs visor is toggled. </summary>
    public bool VisorToggled;

    /// <summary> Whether the NPC is an event NPC or a battle NPC. </summary>
    public ObjectKind Kind;

    /// <summary> Obtain an equipment piece. </summary>
    public readonly CharacterArmor Item(int i)
        => _equip[i];

    /// <summary> Obtain the equipment as CharacterArmors. </summary>
    public readonly CharacterArmor[] Equip()
        => ((ReadOnlySpan<CharacterArmor>)_equip).ToArray();

    /// <summary> Write all the gear appearance to a single string. </summary>
    public string WriteGear()
    {
        var sb = new StringBuilder(256);

        for (var i = 0; i < 10; ++i)
        {
            sb.Append($"{_equip[i].Set.Id:D4}")
                .Append('-')
                .Append($"{_equip[i].Variant.Id:D3}");
            foreach (var stain in _equip[i].Stains)
                sb.Append('-').Append($"{stain.Id:D3}");
        }

        sb.Append($"{Mainhand.Skeleton.Id:D4}")
            .Append('-')
            .Append($"{Mainhand.Weapon.Id:D4}")
            .Append('-')
            .Append($"{Mainhand.Variant.Id:D3}");
        foreach (var stain in Mainhand.Stains)
            sb.Append('-').Append($"{stain.Id:D3}");
        sb.Append(",  ")
            .Append($"{Offhand.Skeleton.Id:D4}")
            .Append('-')
            .Append($"{Offhand.Weapon.Id:D4}")
            .Append('-')
            .Append($"{Offhand.Variant.Id:D3}");
        foreach (var stain in Mainhand.Stains)
            sb.Append('-').Append($"{stain.Id:D3}");
        return sb.ToString();
    }

    /// <summary> Set an equipment piece to a given value. </summary>
    internal void Set(int idx, ulong value)
    {
        _equip[idx] = Unsafe.As<ulong, CharacterArmor>(ref value);
    }

    /// <summary> Check if the appearance data, excluding ID and Name, of two NpcData is equal. </summary>
    public bool DataEquals(in NpcData other)
    {
        if (ModelId != other.ModelId)
            return false;

        if (VisorToggled != other.VisorToggled)
            return false;

        if (!Customize.Equals(other.Customize))
            return false;

        if (!Mainhand.Equals(other.Mainhand))
            return false;

        if (!Offhand.Equals(other.Offhand))
            return false;

        return ((ReadOnlySpan<CharacterArmor>)_equip).SequenceEqual(other._equip);
    }

    [InlineArray(10)]
    private struct EquipArray
    {
        private CharacterArmor _element;
    }
}
