using Glamourer.Interop.Penumbra;
using ImSharp;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class ModCombo(PenumbraService penumbra, DesignSelection selection) : ImSharp.FilterComboBase<ModCombo.CacheItem>(new ModFilter())
{
    public readonly struct CacheItem(in Mod mod, in ModSettings settings, int count)
    {
        public readonly StringPair  Name      = new(mod.Name);
        public readonly StringPair  Directory = new(mod.DirectoryName);
        public readonly ModSettings Settings  = settings;
        public readonly int         Count     = count;

        public readonly Vector4 Color = settings.Enabled
            ? count > 0
                ? ColorId.ContainsItemsEnabled.Value().ToVector()
                : Im.Style[ImGuiColor.Text]
            : count > 0
                ? ColorId.ContainsItemsDisabled.Value().ToVector()
                : Im.Style[ImGuiColor.TextDisabled];

        public readonly bool DifferingNames = string.Equals(mod.Name, mod.DirectoryName, StringComparison.CurrentCultureIgnoreCase);
    }

    protected override float ItemHeight
        => Im.Style.TextHeightWithSpacing;

    protected override IEnumerable<CacheItem> GetItems()
        => penumbra.GetMods(selection.Design?.FilteredItemNames.ToArray() ?? []).Select(t => new CacheItem(t.Mod, t.Settings, t.Count));

    protected override bool DrawItem(in CacheItem item, int globalIndex, bool selected)
    {
        bool ret;
        using (ImGuiColor.Text.Push(item.Color))
        {
            ret = Im.Selectable(item.Name.Utf8, selected);
        }

        if (Im.Item.Hovered())
            DrawTooltip(item);

        return ret;
    }

    private static void DrawTooltip(in CacheItem item)
    {
        using var style = ImStyleSingle.PopupBorderThickness.Push(2 * Im.Style.GlobalScale);
        using var tt    = Im.Tooltip.Begin();

        Im.Dummy(new Vector2(300 * Im.Style.GlobalScale, 0));
        using (Im.Group())
        {
            if (item.DifferingNames)
                Im.Text("Directory Name"u8);
            Im.Text("Enabled"u8);
            Im.Text("Priority"u8);
            Im.Text("Affected Design Items"u8);
            DrawSettingsLeft(item.Settings);
        }

        Im.Line.Same(Math.Max(Im.Item.Size.X + 3 * Im.Style.ItemSpacing.X, 150 * Im.Style.GlobalScale));
        using (Im.Group())
        {
            if (item.DifferingNames)
                Im.Text(item.Directory.Utf8);
            Im.Text($"{item.Settings.Enabled}");
            Im.Text($"{item.Settings.Priority}");
            Im.Text($"{item.Count}");
            DrawSettingsRight(item.Settings);
        }
    }

    private static void DrawSettingsLeft(in ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            Im.Text(setting.Key);
            for (var i = 1; i < setting.Value.Count; ++i)
                Im.Line.New();
        }
    }

    private static void DrawSettingsRight(in ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            if (setting.Value.Count == 0)
                Im.Text("<None Enabled>"u8);
            else
                foreach (var option in setting.Value)
                    Im.Text(option);
        }
    }

    protected override bool IsSelected(CacheItem item, int globalIndex)
        => throw new NotImplementedException();

    private sealed class ModFilter : TextFilterBase<CacheItem>
    {
        public override bool WouldBeVisible(in CacheItem item, int globalIndex)
            => base.WouldBeVisible(in item, globalIndex) || WouldBeVisible(item.Directory.Utf16);

        protected override string ToFilterString(in CacheItem item, int globalIndex)
            => item.Name.Utf16;
    }
}
