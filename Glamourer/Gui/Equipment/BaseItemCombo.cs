using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public abstract class BaseItemCombo(FavoriteManager favorites, ItemManager items) : FilterComboBase<BaseItemCombo.CacheItem>(new ItemFilter())
{
    public abstract StringU8 Label { get; }

    protected readonly FavoriteManager Favorites = favorites;
    protected readonly ItemManager     Items     = items;
    protected          EquipItem       CurrentItem;
    protected          PrimaryId       CustomSetId;
    protected          SecondaryId     CustomWeaponId;
    protected          Variant         CustomVariant;

    public bool Draw(in EquipItem item, out EquipItem newItem, float width)
    {
        using var id = Im.Id.Push(Label);
        CurrentItem   = item;
        CustomVariant = 0;
        if (Draw(StringU8.Empty, item.Name, StringU8.Empty, width, out var cache))
        {
            newItem = cache.Item;
            return true;
        }

        if (CustomVariant.Id is not 0 && Identify(out newItem))
            return true;

        newItem = item;
        return false;
    }

    public readonly struct CacheItem(EquipItem item) : IDisposable
    {
        public readonly EquipItem       Item  = item;
        public readonly StringPair      Name  = new(item.Name);
        public readonly SizedStringPair Model = new($"({item.PrimaryId.Id}-{item.Variant.Id})");

        public void Dispose()
            => Model.Dispose();
    }

    protected sealed class ItemFilter : PartwiseFilterBase<CacheItem>
    {
        public override bool WouldBeVisible(in CacheItem item, int globalIndex)
            => base.WouldBeVisible(in item, globalIndex) || WouldBeVisible(item.Model.Utf16);

        protected override string ToFilterString(in CacheItem item, int globalIndex)
            => item.Name.Utf16;
    }

    protected override FilterComboBaseCache<CacheItem> CreateCache()
        => new Cache(this);

    protected sealed class Cache(FilterComboBase<CacheItem> parent) : FilterComboBaseCache<CacheItem>(parent)
    {
        private static EquipItem _longestItem;

        protected override void ComputeWidth()
        {
            if (!_longestItem.Valid)
            {
                var data = ((BaseItemCombo)Parent).Items.ItemData;
                _longestItem = data.AllItems(true).Concat(data.AllItems(false))
                    .MaxBy(i => Im.Font.CalculateSize($"{i.Item2.Name} ({i.Item2.ModelString})").X).Item2;
            }

            ComboWidth = Im.Font.CalculateSize($"{_longestItem.Name} ({_longestItem.Name})").X
              + Im.Style.FrameHeight
              + Im.Style.ItemSpacing.X * 3;
        }
    }

    protected override float ItemHeight
        => Im.Style.FrameHeightWithSpacing;

    protected override bool DrawItem(in CacheItem item, int globalIndex, bool selected)
    {
        UiHelpers.DrawFavoriteStar(Favorites, item.Item);
        Im.Line.Same();
        var ret = Im.Selectable(item.Name.Utf8, selected);
        Im.Line.Same();
        using var color = ImGuiColor.Text.Push(Rgba32.Gray);
        ImEx.TextRightAligned(item.Model);
        return ret;
    }

    protected override void EnterPressed()
    {
        if (!Im.Io.KeyControl)
            return;

        var split = ((ItemFilter)Filter).Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        switch (split.Length)
        {
            case 2 when ushort.TryParse(split[0], out var setId) && byte.TryParse(split[1], out var variant):
                CustomSetId   = setId;
                CustomVariant = variant;
                break;
            case 3 when ushort.TryParse(split[0], out var setId)
             && ushort.TryParse(split[1],         out var weaponId)
             && byte.TryParse(split[2], out var variant):
                CustomSetId    = setId;
                CustomWeaponId = weaponId;
                CustomVariant  = variant;
                break;
            default: return;
        }
    }

    protected abstract bool Identify(out EquipItem item);

    protected override bool IsSelected(CacheItem item, int globalIndex)
        => item.Item.Id == CurrentItem.Id;
}
