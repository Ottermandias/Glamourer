using Glamourer.Config;
using Glamourer.Interop.Penumbra;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.SettingsTab;

public sealed class CollectionCombo(Configuration config, PenumbraService penumbra)
    : FilterComboBase<CollectionCombo.CacheItem>(new CollectionFilter()), IUiService
{
    private Guid _selected = Guid.Empty;

    public readonly struct CacheItem(Guid id, string name)
    {
        public readonly StringPair Name      = new(name);
        public readonly Guid       Id        = id;
        public readonly StringU8   Incognito = id.ShortGuidU8();
        public readonly StringU8   ShortId   = new($"({id.ShortGuidU8()})");
    }

    protected override float ItemHeight
        => Im.Style.TextHeightWithSpacing;

    protected override IEnumerable<CacheItem> GetItems()
        => penumbra.GetCollections().Select(kvp => new CacheItem(kvp.Key, kvp.Value));

    public bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, Utf8StringHandler<HintStringHandlerBuffer> preview, out string newName,
        ref Guid id, float width)
    {
        _selected = id;
        if (!base.Draw(label, preview, StringU8.Empty, width, out var ret))
        {
            newName = string.Empty;
            return false;
        }

        newName = ret.Name.Utf16;
        id      = ret.Id;
        return true;
    }

    protected override bool DrawItem(in CacheItem item, int globalIndex, bool selected)
    {
        if (config.Ephemeral.IncognitoMode)
            using (Im.Font.PushMono())
            {
                return Im.Selectable(item.Incognito, selected);
            }

        var ret = Im.Selectable(item.Name.Utf8, selected);
        Im.Line.Same();

        using (Im.Font.PushMono())
        {
            using var color = ImGuiColor.Text.Push(ImGuiColor.TextDisabled.Get());
            ImEx.TextRightAligned(item.ShortId);
        }

        return ret;
    }

    protected override bool IsSelected(CacheItem item, int globalIndex)
        => item.Id == _selected;

    private sealed class CollectionFilter : Utf8FilterBase<CacheItem>
    {
        public override bool WouldBeVisible(in CacheItem item, int globalIndex)
            => base.WouldBeVisible(in item, globalIndex) || WouldBeVisible(item.Incognito);

        protected override ReadOnlySpan<byte> ToFilterString(in CacheItem item, int globalIndex)
            => item.Name;
    }

    protected override FilterComboBaseCache<CacheItem> CreateCache()
        => new Cache(this);

    private sealed class Cache(CollectionCombo parent) : FilterComboBaseCache<CacheItem>(parent)
    {
        protected override void ComputeWidth()
            => ComboWidth = AllItems.Max(i => i.Name.Utf8.CalculateSize().X + Im.Style.ItemSpacing.X * 2 + i.ShortId.CalculateSize().X);
    }
}
