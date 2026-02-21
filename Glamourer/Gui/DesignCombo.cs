using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Designs.Special;
using Glamourer.Events;
using ImSharp;
using Luna;

namespace Glamourer.Gui;

public abstract class DesignComboBase(
    Config.EphemeralConfig config,
    DesignManager designs,
    DesignChanged designChanged,
    DesignColors designColors,
    TabSelected tabSelected,
    DesignFileSystem designFileSystem)
    : FilterComboBase<DesignComboBase.CacheItem>(new DesignFilter(), ConfigData.Default with { ComputeWidth = true })
{
    protected readonly Config.EphemeralConfig Config           = config;
    protected readonly DesignChanged          DesignChanged    = designChanged;
    protected readonly DesignColors           DesignColors     = designColors;
    protected readonly DesignFileSystem       DesignFileSystem = designFileSystem;
    protected readonly TabSelected            TabSelected      = tabSelected;
    protected readonly DesignManager          Designs          = designs;
    protected          IDesignStandIn?        CurrentDesign;

    protected CacheItem CreateItem(IDesignStandIn design)
    {
        var color = design is Design d1 ? DesignColors.GetColor(d1).ToVector() : ColorId.NormalDesign.Value().ToVector();
        var path  = design is Design d2 ? d2.Node!.FullPath : string.Empty;
        var name  = design.ResolveName(false);
        if (path == name)
            path = string.Empty;
        return new CacheItem(design, color, path, name);
    }

    protected override bool IsSelected(CacheItem item, int globalIndex)
        => item.Design == CurrentDesign;

    public virtual bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, IDesignStandIn? currentDesign, out IDesignStandIn? newSelection,
        float width)
    {
        CurrentDesign = currentDesign;
        bool ret;
        using (ImGuiColor.Text.Push(DesignColors.GetColor(CurrentDesign as Design)))
        {
            ret = currentDesign is null
                ? base.Draw(label, "Select Design Here..."u8,                       StringU8.Empty, width, out var result)
                : base.Draw(label, currentDesign.ResolveName(Config.IncognitoMode), StringU8.Empty, width, out result);
            newSelection = ret ? result.Design : currentDesign;
        }

        if (CurrentDesign is Design design)
        {
            if (Im.Item.RightClicked() && Im.Io.KeyControl)
                TabSelected.Invoke(MainTabType.Designs, design);
            Im.Tooltip.OnHover("Control + Right-Click to move to design."u8);
        }
        else
        {
            QuickSelectedDesignTooltip(CurrentDesign as QuickSelectedDesign);
        }

        CurrentDesign = null;
        return ret;
    }

    private void QuickSelectedDesignTooltip(QuickSelectedDesign? design)
    {
        if (design is null)
            return;

        if (!Im.Item.Hovered())
            return;

        using var tt           = Im.Tooltip.Begin();
        var       linkedDesign = design.CurrentDesign;
        if (linkedDesign is not null)
        {
            Im.Text("Currently resolving to "u8);
            using var color = ImGuiColor.Text.Push(DesignColors.GetColor(linkedDesign));
            Im.Line.NoSpacing();
            Im.Text(linkedDesign.Name.Text);
        }
        else
        {
            Im.Text("No design selected in the Quick Design Bar."u8);
        }
    }

    protected sealed class DesignFilter : Utf8FilterBase<CacheItem>
    {
        public override bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
        {
            using var _ = ImGuiColor.Text.PushDefault();
            return base.DrawFilter(label, availableRegion);
        }

        public override bool WouldBeVisible(in CacheItem item, int globalIndex)
            => WouldBeVisible(item.Name.Utf8) || WouldBeVisible(item.Incognito.Utf8) || WouldBeVisible(item.FullPath.Utf8);

        protected override ReadOnlySpan<byte> ToFilterString(in CacheItem item, int globalIndex)
            => item.Name.Utf8;
    }

    protected sealed class Cache : FilterComboBaseCache<CacheItem>
    {
        private new DesignComboBase Parent
            => (DesignComboBase)base.Parent;

        public Cache(DesignComboBase parent)
            : base(parent)
        {
            Parent.DesignColors.ColorChanged += OnDesignColorChanged;
            Parent.DesignChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignCombo);
        }

        protected override void ComputeWidth()
            => ComboWidth = UnfilteredItems.Max(d
                => d.Name.Utf8.CalculateSize(false).X
              + d.FullPath.Utf8.CalculateSize(false).X
              + 2 * Im.Style.ItemSpacing.X
              + Im.Style.ScrollbarSize);

        protected override void Dispose(bool disposing)
        {
            Parent.DesignColors.ColorChanged -= OnDesignColorChanged;
            Parent.DesignChanged.Unsubscribe(OnDesignChanged);
            base.Dispose(disposing);
        }

        private void OnDesignColorChanged()
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        private void OnDesignChanged(DesignChanged.Type type, Design? _1, ITransaction? _2 = null)
        {
            if (type switch
                {
                    DesignChanged.Type.Created        => true,
                    DesignChanged.Type.Renamed        => true,
                    DesignChanged.Type.ChangedColor   => true,
                    DesignChanged.Type.Deleted        => true,
                    DesignChanged.Type.QuickDesignBar => true,
                    _                                 => false,
                })
                Dirty |= IManagedCache.DirtyFlags.Custom;
        }
    }

    protected override FilterComboBaseCache<CacheItem> CreateCache()
        => new Cache(this);

    public readonly struct CacheItem(IDesignStandIn design, Vector4 color, string path, string name)
    {
        public readonly IDesignStandIn Design    = design;
        public readonly StringPair     Name      = new(name);
        public readonly StringPair     Incognito = new(design.ResolveName(true));
        public readonly StringPair     FullPath  = new(path);
        public readonly Vector4        Color     = color;

        public static string Ordering(CacheItem item)
            => item.FullPath.Utf16.Length > 0 ? item.FullPath.Utf16 : item.Name.Utf16;
    }


    protected override bool DrawItem(in CacheItem item, int globalIndex, bool selected)
    {
        using var color = ImGuiColor.Text.Push(item.Color);
        var       name  = Config.IncognitoMode ? item.Incognito.Utf8 : item.Name.Utf8;
        var       ret   = Im.Selectable(name, selected);
        if (!item.FullPath.IsEmpty && !Config.IncognitoMode)
        {
            Im.Line.Same();
            color.Push(ImGuiColor.Text, Im.Style[ImGuiColor.TextDisabled]);
            ImEx.TextRightAligned(item.FullPath.Utf8);
        }
        else if (item.Design is QuickSelectedDesign { CurrentDesign: { } d })
        {
            Im.Line.Same();
            color.Push(ImGuiColor.Text, DesignColors.GetColor(d));
            ImEx.TextRightAligned(d.ResolveName(Config.IncognitoMode));
        }

        return ret;
    }

    protected override float ItemHeight
        => Im.Style.TextHeightWithSpacing;
}

public sealed class QuickDesignCombo : DesignComboBase, IDisposable, IUiService
{
    public Design? QuickDesign
    {
        get;
        private set
        {
            if (field == value)
                return;

            field                      = value;
            Config.SelectedQuickDesign = field?.Identifier ?? Guid.Empty;
            Config.Save();
        }
    }


    public QuickDesignCombo(Config.EphemeralConfig config, DesignChanged designChanged, DesignColors designColors, TabSelected tabSelected,
        DesignFileSystem designFileSystem, DesignManager designs)
        : base(config, designs, designChanged, designColors, tabSelected, designFileSystem)
    {
        if (Designs.Designs.TryGetValue(config.SelectedQuickDesign, out var design) && design.QuickDesign)
            QuickDesign = design;
        DesignChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignCombo);
    }

    private void OnDesignChanged(DesignChanged.Type type, Design changedDesign, ITransaction? _)
    {
        switch (type)
        {
            case DesignChanged.Type.Created:
                // If the quick design bar has no selection, select the new design if it supports the bar.
                if (QuickDesign is null && changedDesign.QuickDesign)
                    QuickDesign = changedDesign;
                break;
            case DesignChanged.Type.Deleted:
                // If the deleted design was selected, select the first design that supports the bar, if any.
                if (QuickDesign == changedDesign)
                    QuickDesign = Designs.Designs.FirstOrDefault(d => d.QuickDesign);
                break;
            case DesignChanged.Type.ReloadedAll:
                // If all designs were reloaded, update the selection.
                QuickDesign = Designs.Designs.TryGetValue(Config.SelectedQuickDesign, out var design) && design.QuickDesign ? design : null;
                break;
            case DesignChanged.Type.QuickDesignBar:
                // If the quick design support of a design was changed, select the new design if the bar has no selection and the design now supports it,
                if (QuickDesign is null && changedDesign.QuickDesign)
                    QuickDesign = changedDesign;
                // or select the first design that supports the bar, if any, if the support was removed from the currently selected design.
                else if (QuickDesign == changedDesign && !changedDesign.QuickDesign)
                    QuickDesign = Designs.Designs.FirstOrDefault(d => d.QuickDesign);
                break;
        }
    }

    public bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, float width)
    {
        if (!base.Draw(label, QuickDesign, out var newDesign, width))
            return false;

        QuickDesign = newDesign as Design;
        return true;
    }

    protected override IEnumerable<CacheItem> GetItems()
        => Designs.Designs
            .Where(design => design.QuickDesign)
            .Select(CreateItem)
            .OrderBy(CacheItem.Ordering);

    public void Dispose()
        => DesignChanged.Unsubscribe(OnDesignChanged);
}

public sealed class LinkDesignCombo : DesignComboBase, IUiService, IDisposable
{
    public Design? NewSelection { get; private set; }

    public LinkDesignCombo(Config.EphemeralConfig config, DesignChanged designChanged, DesignColors designColors, TabSelected tabSelected,
        DesignFileSystem designFileSystem, DesignManager designs)
        : base(config, designs, designChanged, designColors, tabSelected, designFileSystem)
    {
        DesignChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignCombo);
    }

    public bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, float width)
    {
        if (!base.Draw(label, NewSelection, out var newSelection, width))
            return false;

        NewSelection = newSelection as Design;
        return true;
    }

    protected override IEnumerable<CacheItem> GetItems()
        => Designs.Designs.Select(CreateItem)
            .OrderBy(CacheItem.Ordering);

    public void Dispose()
        => DesignChanged.Unsubscribe(OnDesignChanged);

    private void OnDesignChanged(DesignChanged.Type type, Design design, ITransaction? _)
    {
        if (type is DesignChanged.Type.Deleted && design == NewSelection || type is DesignChanged.Type.ReloadedAll)
            NewSelection = null;
    }
}

public sealed class RandomDesignCombo(
    Config.EphemeralConfig config,
    DesignManager designs,
    DesignChanged designChanged,
    DesignColors designColors,
    TabSelected tabSelected,
    DesignFileSystem designFileSystem) : DesignComboBase(config, designs, designChanged, designColors, tabSelected, designFileSystem),
    IUiService
{
    private Design? GetDesign(RandomPredicate.Exact exact)
    {
        return exact.Which switch
        {
            RandomPredicate.Exact.Type.Name       => Designs.Designs.FirstOrDefault(d => d.Name == exact.Value),
            RandomPredicate.Exact.Type.Path       => Designs.Designs.FirstOrDefault(d => d.Node!.FullPath == exact.Value.Text),
            RandomPredicate.Exact.Type.Identifier => Designs.Designs.ByIdentifier(Guid.TryParse(exact.Value.Text, out var g) ? g : Guid.Empty),
            _                                     => null,
        };
    }

    public bool Draw(RandomPredicate.Exact exact, [NotNullWhen(true)] out Design? newDesign, float width)
    {
        var design = GetDesign(exact);
        if (Draw(StringU8.Empty, design?.ResolveName(Config.IncognitoMode) ?? $"Not Found [{exact.Value.Text}]", StringU8.Empty, width,
                out var newItem)
         && newItem.Design is Design d)
        {
            newDesign = d;
            return true;
        }

        newDesign = null;
        return false;
    }

    protected override IEnumerable<CacheItem> GetItems()
        => Designs.Designs.Select(CreateItem)
            .OrderBy(CacheItem.Ordering);
}

public sealed class SpecialDesignCombo : DesignComboBase, IUiService
{
    private readonly AutoDesignManager _autoDesigns;

    private readonly CacheItem _random;
    private readonly CacheItem _revert;
    private readonly CacheItem _quick;

    public SpecialDesignCombo(Config.EphemeralConfig config,
        DesignManager designs,
        DesignChanged designChanged,
        DesignColors designColors,
        TabSelected tabSelected,
        DesignFileSystem designFileSystem,
        AutoDesignManager autoDesigns,
        RandomDesignGenerator rng, QuickSelectedDesign quickSelectedDesign)
        : base(config, designs, designChanged, designColors, tabSelected, designFileSystem)
    {
        _autoDesigns = autoDesigns;
        _random      = CreateItem(new RandomDesign(rng));
        _revert      = CreateItem(new RevertDesign());
        _quick       = CreateItem(quickSelectedDesign);
    }

    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex)
    {
        if (!Draw(StringU8.Empty, design?.Design, out var newSelection, Im.ContentRegion.Available.X) || newSelection is null)
            return;

        if (autoDesignIndex >= 0)
            _autoDesigns.ChangeDesign(set, autoDesignIndex, newSelection);
        else
            _autoDesigns.AddDesign(set, newSelection);
    }

    protected override IEnumerable<CacheItem> GetItems()
        => Designs.Designs
            .Select(CreateItem)
            .OrderBy(CacheItem.Ordering)
            .Prepend(_random)
            .Prepend(_quick)
            .Prepend(_revert);
}
