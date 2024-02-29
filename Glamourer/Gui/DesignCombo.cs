using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Events;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Widgets;

namespace Glamourer.Gui;

public abstract class DesignComboBase : FilterComboCache<Tuple<IDesignStandIn, string>>, IDisposable
{
    private readonly   EphemeralConfig _config;
    private readonly   DesignChanged   _designChanged;
    private readonly   DesignColors    _designColors;
    protected readonly TabSelected     TabSelected;
    protected          float           InnerWidth;
    private            IDesignStandIn? _currentDesign;

    protected DesignComboBase(Func<IReadOnlyList<Tuple<IDesignStandIn, string>>> generator, Logger log, DesignChanged designChanged,
        TabSelected tabSelected, EphemeralConfig config, DesignColors designColors)
        : base(generator, MouseWheelType.Control, log)
    {
        _designChanged = designChanged;
        TabSelected    = tabSelected;
        _config        = config;
        _designColors  = designColors;
        _designChanged.Subscribe(OnDesignChange, DesignChanged.Priority.DesignCombo);
    }

    public bool Incognito
        => _config.IncognitoMode;

    void IDisposable.Dispose()
    {
        _designChanged.Unsubscribe(OnDesignChange);
        GC.SuppressFinalize(this);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (design, path) = Items[globalIdx];
        bool ret;
        if (design is Design realDesign)
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, _designColors.GetColor(realDesign));
            ret = base.DrawSelectable(globalIdx, selected);

            if (path.Length > 0 && realDesign.Name != path)
            {
                var start          = ImGui.GetItemRectMin();
                var pos            = start.X + ImGui.CalcTextSize(realDesign.Name).X;
                var maxSize        = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                var remainingSpace = maxSize - pos;
                var requiredSize   = ImGui.CalcTextSize(path).X + ImGui.GetStyle().ItemInnerSpacing.X;
                var offset         = remainingSpace - requiredSize;
                if (ImGui.GetScrollMaxY() == 0)
                    offset -= ImGui.GetStyle().ItemInnerSpacing.X;

                if (offset < ImGui.GetStyle().ItemSpacing.X)
                    ImGuiUtil.HoverTooltip(path);
                else
                    ImGui.GetWindowDrawList().AddText(start with { X = pos + offset },
                        ImGui.GetColorU32(ImGuiCol.TextDisabled), path);
            }
        }
        else
        {
            ret = base.DrawSelectable(globalIdx, selected);
        }


        return ret;
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        CurrentSelectionIdx = Items.IndexOf(p => _currentDesign == p.Item1);
        CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        return CurrentSelectionIdx;
    }

    protected bool Draw(IDesignStandIn? currentDesign, string? label, float width)
    {
        _currentDesign = currentDesign;
        InnerWidth     = 400 * ImGuiHelpers.GlobalScale;
        var  name = label ?? "Select Design Here...";
        bool ret;
        using (_ = currentDesign != null ? ImRaii.PushColor(ImGuiCol.Text, _designColors.GetColor(currentDesign as Design)) : null)
        {
            ret = Draw("##design", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing())
             && CurrentSelection != null;
        }

        if (currentDesign is Design design)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                TabSelected.Invoke(MainWindow.TabType.Designs, design);
            ImGuiUtil.HoverTooltip("Control + Right-Click to move to design.");
        }

        _currentDesign = null;
        return ret;
    }

    protected override string ToString(Tuple<IDesignStandIn, string> obj)
        => obj.Item1.ResolveName(Incognito);

    protected override float GetFilterWidth()
        => InnerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        var (design, path) = Items[globalIndex];
        return filter.IsContained(path) || filter.IsContained(design.ResolveName(false));
    }

    private void OnDesignChange(DesignChanged.Type type, Design design, object? data = null)
    {
        switch (type)
        {
            case DesignChanged.Type.Created:
            case DesignChanged.Type.Renamed:
            case DesignChanged.Type.ChangedColor:
            case DesignChanged.Type.Deleted:
            case DesignChanged.Type.QuickDesignBar:
                var priorState = IsInitialized;
                if (priorState)
                    Cleanup();
                CurrentSelectionIdx = Items.IndexOf(s => ReferenceEquals(s.Item1, CurrentSelection?.Item1));
                if (CurrentSelectionIdx >= 0)
                {
                    CurrentSelection = Items[CurrentSelectionIdx];
                }
                else if (Items.Count > 0)
                {
                    CurrentSelectionIdx = 0;
                    CurrentSelection    = Items[0];
                }
                else
                {
                    CurrentSelection = null;
                }

                if (!priorState)
                    Cleanup();
                break;
        }
    }
}

public abstract class DesignCombo : DesignComboBase
{
    protected DesignCombo(Logger log, DesignChanged designChanged, TabSelected tabSelected,
        EphemeralConfig config, DesignColors designColors, Func<IReadOnlyList<Tuple<IDesignStandIn, string>>> generator)
        : base(generator, log, designChanged, tabSelected, config, designColors)
    {
        if (Items.Count == 0)
            return;

        CurrentSelection    = Items[0];
        CurrentSelectionIdx = 0;
        base.Cleanup();
    }

    public IDesignStandIn? Design
        => CurrentSelection?.Item1;

    public void Draw(float width)
        => Draw(Design, Design?.ResolveName(Incognito) ?? string.Empty, width);
}

public sealed class QuickDesignCombo : DesignCombo
{
    public QuickDesignCombo(DesignManager designs,
        DesignFileSystem fileSystem,
        Logger log,
        DesignChanged designChanged,
        TabSelected tabSelected,
        EphemeralConfig config,
        DesignColors designColors)
        : base(log, designChanged, tabSelected, config, designColors, () =>
        [
            .. designs.Designs
                .Where(d => d.QuickDesign)
                .Select(d => new Tuple<IDesignStandIn, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
                .OrderBy(d => d.Item2),
        ])
        => AllowMouseWheel = MouseWheelType.Unmodified;
}

public sealed class LinkDesignCombo(
    DesignManager designs,
    DesignFileSystem fileSystem,
    Logger log,
    DesignChanged designChanged,
    TabSelected tabSelected,
    EphemeralConfig config,
    DesignColors designColors)
    : DesignCombo(log, designChanged, tabSelected, config, designColors, () =>
    [
        .. designs.Designs
            .Select(d => new Tuple<IDesignStandIn, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
            .OrderBy(d => d.Item2),
    ]);

public sealed class RandomDesignCombo(
    DesignManager designs,
    DesignFileSystem fileSystem,
    Logger log,
    DesignChanged designChanged,
    TabSelected tabSelected,
    EphemeralConfig config,
    DesignColors designColors)
    : DesignCombo(log, designChanged, tabSelected, config, designColors, () =>
    [
        .. designs.Designs
            .Select(d => new Tuple<IDesignStandIn, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
            .OrderBy(d => d.Item2),
    ])
{
    private Design? GetDesign(RandomPredicate.Exact exact)
    {
        return exact.Which switch
        {
            RandomPredicate.Exact.Type.Name => designs.Designs.FirstOrDefault(d => d.Name == exact.Value),
            RandomPredicate.Exact.Type.Path => fileSystem.Find(exact.Value.Text, out var c) && c is DesignFileSystem.Leaf l ? l.Value : null,
            RandomPredicate.Exact.Type.Identifier => designs.Designs.ByIdentifier(Guid.TryParse(exact.Value.Text, out var g) ? g : Guid.Empty),
            _ => null,
        };
    }

    public bool Draw(RandomPredicate.Exact exact, float width)
    {
        var design = GetDesign(exact);
        return Draw(design, design?.ResolveName(Incognito) ?? $"Not Found [{exact.Value.Text}]", width);
    }

    public bool Draw(IDesignStandIn? design, float width)
        => Draw(design, design?.ResolveName(Incognito) ?? string.Empty, width);
}

public sealed class SpecialDesignCombo(
    DesignManager designs,
    DesignFileSystem fileSystem,
    TabSelected tabSelected,
    DesignColors designColors,
    Logger log,
    DesignChanged designChanged,
    AutoDesignManager autoDesignManager,
    EphemeralConfig config,
    RandomDesignGenerator rng)
    : DesignComboBase(() => designs.Designs
        .Select(d => new Tuple<IDesignStandIn, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
        .OrderBy(d => d.Item2)
        .Prepend(new Tuple<IDesignStandIn, string>(new RandomDesign(rng), string.Empty))
        .Prepend(new Tuple<IDesignStandIn, string>(new RevertDesign(),    string.Empty))
        .ToList(), log, designChanged, tabSelected, config, designColors)
{
    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex)
    {
        if (!Draw(design?.Design, design?.Design.ResolveName(Incognito), ImGui.GetContentRegionAvail().X))
            return;

        if (autoDesignIndex >= 0)
            autoDesignManager.ChangeDesign(set, autoDesignIndex, CurrentSelection!.Item1);
        else
            autoDesignManager.AddDesign(set, CurrentSelection!.Item1);
    }
}
