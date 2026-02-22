using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public readonly struct UnlockCacheItem(in EquipItem item, in EquipItem offhand, in EquipItem gauntlets, in JobGroup jobs)
{
    private static readonly StringU8 Always = new("Always"u8);

    [Flags]
    public enum Dyability : byte
    {
        No  = 1,
        Yes = 2,
        Two = 4,
    }

    public readonly EquipItem  Item = item;
    public readonly StringPair Name = new(item.Name);
    public readonly EquipFlag  Slot = item.Type.ToSlot().ToFlag();

    public required DateTimeOffset UnlockTimestamp
    {
        get;
        init
        {
            field = value;
            UnlockText = value == DateTimeOffset.MinValue ? Always :
                value == DateTimeOffset.MaxValue          ? StringU8.Empty : new StringU8($"{value.LocalDateTime:g}");
        }
    }

    public readonly StringU8 UnlockText;

    public readonly StringPair         ItemId              = new($"{item.ItemId.Id}");
    public readonly StringPair         ModelString         = new(item.ModelString);
    public readonly StringPair         OffhandModelString  = offhand.Valid ? new StringPair(offhand.ModelString) : StringPair.Empty;
    public readonly StringPair         GauntletModelString = gauntlets.Valid ? new StringPair(gauntlets.ModelString) : StringPair.Empty;
    public readonly StringPair         RequiredLevel       = new($"{item.Level.Value}");
    public required (string, string)[] Mods         { get; init; }
    public          int                RelevantMods { get; init; }
    public readonly JobFlag            Jobs    = jobs.Flags;
    public readonly StringU8           JobText = jobs.Name.IsEmpty ? new StringU8($"Unknown {jobs.Id.Id}") : jobs.Name;
    public required bool               Favorite { get; init; }
    public readonly bool               Tradable = item.Flags.HasFlag(ItemFlags.IsTradable);
    public readonly bool               Crest    = item.Flags.HasFlag(ItemFlags.IsCrestWorthy);

    public readonly Dyability Dyable = item.Flags.HasFlag(ItemFlags.IsDyable1)
        ? item.Flags.HasFlag(ItemFlags.IsDyable2) ? Dyability.Two : Dyability.Yes
        : Dyability.No;
}
