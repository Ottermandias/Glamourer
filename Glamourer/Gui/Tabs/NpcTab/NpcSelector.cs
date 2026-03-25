using Glamourer.GameData;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcSelector(
    NpcCustomizeSet npcs,
    LocalNpcAppearanceData favorites,
    NpcFilter filter,
    DesignColors designColors,
    NpcSelection selection) : IPanel
{
    private readonly NpcCustomizeSet        _npcs         = npcs;
    private readonly LocalNpcAppearanceData _favorites    = favorites;
    private readonly NpcFilter              _filter       = filter;
    private readonly DesignColors           _designColors = designColors;

    public ReadOnlySpan<byte> Id
        => "NpcSelector"u8;

    public void Draw()
    {
        Im.Cursor.Y += Im.Style.FramePadding.Y;
        var       cache   = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new Cache(this));
        using var clipper = new Im.ListClipper(cache.Count, Im.Style.TextHeightWithSpacing);
        using var color   = new Im.ColorDisposable();
        foreach (var item in clipper.Iterate(cache))
        {
            Im.Cursor.X += Im.Style.FramePadding.X;
            using var id = Im.Id.Push(item.Npc.Id.Id);
            color.Push(ImGuiColor.Text, item.Color);
            if (Im.Selectable(item.Name.Utf8, item.Npc.Id == selection.Id))
                selection.Update(item);
            color.Pop();
            var size = item.Id.CalculateSize();
            Im.Line.Same();
            if (Im.ContentRegion.Available.X >= size.X)
            {
                color.Push(ImGuiColor.Text, Im.Style[ImGuiColor.TextDisabled]);
                ImEx.TextRightAligned(item.Id, 0, size.X);
                color.Pop();
            }
            else
            {
                Im.Tooltip.OnHover(item.Id);
                Im.Line.New();
            }
        }
    }

    private NpcCacheItem CreateItem(in NpcData data)
    {
        var colorText = _favorites.GetColor(data);
        var (color, favorite) = _favorites.GetData(data);
        return new NpcCacheItem(data, colorText, color, favorite);
    }

    private sealed class Cache : BasicFilterCache<NpcCacheItem>
    {
        private readonly NpcSelector _parent;

        public Cache(NpcSelector parent)
            : base(parent._filter)
        {
            _parent                            =  parent;
            _parent._favorites.DataChanged     += OnDataChange;
            _parent._designColors.ColorChanged += OnDataChange;
        }

        private void OnDataChange()
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        protected override void Dispose(bool disposing)
        {
            _parent._favorites.DataChanged     -= OnDataChange;
            _parent._designColors.ColorChanged -= OnDataChange;
            base.Dispose(disposing);
        }

        protected override IEnumerable<NpcCacheItem> GetItems()
            => _parent._npcs.Select(n => _parent.CreateItem(n)).OrderByDescending(n => n.Favorite);
    }
}
