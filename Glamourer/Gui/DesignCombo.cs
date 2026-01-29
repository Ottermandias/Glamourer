using Dalamud.Interface.Utility.Raii;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Designs.Special;
using Glamourer.Events;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Extensions;
using OtterGui.Log;
using OtterGui.Widgets;
using MouseWheelType = OtterGui.Widgets.MouseWheelType;

namespace Glamourer.Gui;

public abstract class DesignComboBase : FilterComboCache<Tuple<IDesignStandIn, string>>, IDisposable
{
    protected readonly EphemeralConfig Config;
    protected readonly DesignChanged   DesignChanged;
    protected readonly DesignColors    DesignColors;
    protected readonly TabSelected     TabSelected;
    protected          float           InnerWidth;
    private            IDesignStandIn? _currentDesign;
    private            bool            _isCurrentSelectionDirty;

    protected DesignComboBase(Func<IReadOnlyList<Tuple<IDesignStandIn, string>>> generator, Logger log, DesignChanged designChanged,
        TabSelected tabSelected, EphemeralConfig config, DesignColors designColors)
        : base(generator, MouseWheelType.Control, log)
    {
        DesignChanged = designChanged;
        TabSelected   = tabSelected;
        Config        = config;
        DesignColors  = designColors;
        DesignChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignCombo);
    }

    public bool Incognito
        => Config.IncognitoMode;

    void IDisposable.Dispose()
    {
        DesignChanged.Unsubscribe(OnDesignChanged);
        GC.SuppressFinalize(this);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (design, path) = Items[globalIdx];
        bool ret;
        switch (design)
        {
            case Design realDesign:
            {
                using var color = ImGuiColor.Text.Push(DesignColors.GetColor(realDesign));
                ret = base.DrawSelectable(globalIdx, selected);
                DrawPath(path, realDesign);
                return ret;
            }
            case QuickSelectedDesign quickDesign:
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.NormalDesign.Value());
                ret = base.DrawSelectable(globalIdx, selected);
                DrawResolvedDesign(quickDesign);
                return ret;
            }
            default: return base.DrawSelectable(globalIdx, selected);
        }
    }

    private static void DrawPath(string path, Design realDesign)
    {
        if (path.Length <= 0 || realDesign.Name == path)
            return;

        DrawRightAligned(realDesign.Name, path, ImGui.GetColorU32(ImGuiCol.TextDisabled));
    }

    private void DrawResolvedDesign(QuickSelectedDesign quickDesign)
    {
        var linkedDesign = quickDesign.CurrentDesign;
        if (linkedDesign != null)
            DrawRightAligned(quickDesign.ResolveName(false), linkedDesign.Name.Text, DesignColors.GetColor(linkedDesign));
        else
            DrawRightAligned(quickDesign.ResolveName(false), "[Nothing]", DesignColors.MissingColor);
    }

    protected bool Draw(IDesignStandIn? currentDesign, string? label, float width)
    {
        _currentDesign = currentDesign;
        UpdateCurrentSelection();
        InnerWidth = 400 * Im.Style.GlobalScale;
        var  name = label ?? "Select Design Here...";
        bool ret;
        using (currentDesign is not null ? ImGuiColor.Text.Push(DesignColors.GetColor(currentDesign as Design)) : null)
        {
            ret = Draw("##design", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing()) && CurrentSelection is not null;
        }

        if (currentDesign is Design design)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                TabSelected.Invoke(MainTabType.Designs, design);
            ImGuiUtil.HoverTooltip("Control + Right-Click to move to design.");
        }

        QuickSelectedDesignTooltip(currentDesign);

        _currentDesign = null;
        return ret;
    }

    protected override string ToString(Tuple<IDesignStandIn, string> obj)
        => obj.Item1.ResolveName(Incognito);

    protected override float GetFilterWidth()
        => InnerWidth - 2 * Im.Style.FramePadding.X;

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        var (design, path) = Items[globalIndex];
        return filter.IsContained(path) || filter.IsContained(design.ResolveName(false));
    }

    protected override void OnMouseWheel(string preview, ref int _2, int steps)
    {
        if (!ReferenceEquals(_currentDesign, CurrentSelection?.Item1))
            CurrentSelectionIdx = -1;

        base.OnMouseWheel(preview, ref _2, steps);
    }

    private void UpdateCurrentSelection()
    {
        if (!_isCurrentSelectionDirty)
            return;

        var priorState = IsInitialized;
        if (priorState)
            Cleanup();
        CurrentSelectionIdx = Items.IndexOf(s => ReferenceEquals(s.Item1, CurrentSelection?.Item1));
        if (CurrentSelectionIdx >= 0)
        {
            UpdateSelection(Items[CurrentSelectionIdx]);
        }
        else if (Items.Count > 0)
        {
            CurrentSelectionIdx = 0;
            UpdateSelection(Items[0]);
        }
        else
        {
            UpdateSelection(null);
        }

        if (!priorState)
            Cleanup();
        _isCurrentSelectionDirty = false;
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        CurrentSelectionIdx = Items.IndexOf(p => _currentDesign == p.Item1);
        UpdateSelection(CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null);
        return CurrentSelectionIdx;
    }

    private void OnDesignChanged(DesignChanged.Type type, Design? _1, ITransaction? _2 = null)
    {
        _isCurrentSelectionDirty = type switch
        {
            DesignChanged.Type.Created        => true,
            DesignChanged.Type.Renamed        => true,
            DesignChanged.Type.ChangedColor   => true,
            DesignChanged.Type.Deleted        => true,
            DesignChanged.Type.QuickDesignBar => true,
            _                                 => _isCurrentSelectionDirty,
        };
    }

    private void QuickSelectedDesignTooltip(IDesignStandIn? design)
    {
        if (!ImGui.IsItemHovered())
            return;

        if (design is not QuickSelectedDesign q)
            return;

        using var tt           = ImRaii.Tooltip();
        var       linkedDesign = q.CurrentDesign;
        if (linkedDesign != null)
        {
            ImGui.TextUnformatted("Currently resolving to ");
            using var color = ImGuiColor.Text.Push(DesignColors.GetColor(linkedDesign));
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(linkedDesign.Name.Text);
        }
        else
        {
            ImGui.TextUnformatted("No design selected in the Quick Design Bar.");
        }
    }

    private static void DrawRightAligned(string leftText, string text, Rgba32 color)
    {
        var start          = ImGui.GetItemRectMin();
        var pos            = start.X + ImGui.CalcTextSize(leftText).X;
        var maxSize        = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        var remainingSpace = maxSize - pos;
        var requiredSize   = ImGui.CalcTextSize(text).X + Im.Style.ItemInnerSpacing.X;
        var offset         = remainingSpace - requiredSize;
        if (ImGui.GetScrollMaxY() == 0)
            offset -= Im.Style.ItemInnerSpacing.X;

        if (offset < Im.Style.ItemSpacing.X)
            ImGuiUtil.HoverTooltip(text);
        else
            Im.Window.DrawList.Text(start with { X = pos + offset }, color, text);
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
    public QuickDesignCombo(DesignFileSystem fileSystem,
        Logger log,
        DesignChanged designChanged,
        TabSelected tabSelected,
        EphemeralConfig config,
        DesignColors designColors)
        : base(log, designChanged, tabSelected, config, designColors, () =>
        [
            .. fileSystem
                .Where(kvp => kvp.Key.QuickDesign)
                .Select(kvp => new Tuple<IDesignStandIn, string>(kvp.Key, kvp.Value.FullName()))
                .OrderBy(d => d.Item2),
        ])
    {
        if (config.SelectedQuickDesign != Guid.Empty)
        {
            CurrentSelectionIdx = Items.IndexOf(t => t.Item1 is Design d && d.Identifier == config.SelectedQuickDesign);
            if (CurrentSelectionIdx >= 0)
                CurrentSelection = Items[CurrentSelectionIdx];
            else if (Items.Count > 0)
                CurrentSelectionIdx = 0;
        }

        AllowMouseWheel  =  MouseWheelType.Unmodified;
        SelectionChanged += OnSelectionChange;
    }

    private void OnSelectionChange(Tuple<IDesignStandIn, string>? old, Tuple<IDesignStandIn, string>? @new)
    {
        if (old == null)
        {
            if (@new?.Item1 is not Design d)
                return;

            Config.SelectedQuickDesign = d.Identifier;
            Config.Save();
        }
        else if (@new?.Item1 is not Design d)
        {
            Config.SelectedQuickDesign = Guid.Empty;
            Config.Save();
        }
        else if (!old.Item1.Equals(@new.Item1))
        {
            Config.SelectedQuickDesign = d.Identifier;
            Config.Save();
        }
    }
}

public sealed class LinkDesignCombo(
    DesignFileSystem fileSystem,
    Logger log,
    DesignChanged designChanged,
    TabSelected tabSelected,
    EphemeralConfig config,
    DesignColors designColors)
    : DesignCombo(log, designChanged, tabSelected, config, designColors, () =>
    [
        .. fileSystem
            .Select(kvp => new Tuple<IDesignStandIn, string>(kvp.Key, kvp.Value.FullName()))
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
        .. fileSystem
            .Select(kvp => new Tuple<IDesignStandIn, string>(kvp.Key, kvp.Value.FullName()))
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
    DesignFileSystem fileSystem,
    TabSelected tabSelected,
    DesignColors designColors,
    Logger log,
    DesignChanged designChanged,
    AutoDesignManager autoDesignManager,
    EphemeralConfig config,
    RandomDesignGenerator rng,
    QuickSelectedDesign quickSelectedDesign)
    : DesignComboBase(() => fileSystem
        .Select(kvp => new Tuple<IDesignStandIn, string>(kvp.Key, kvp.Value.FullName()))
        .OrderBy(d => d.Item2)
        .Prepend(new Tuple<IDesignStandIn, string>(new RandomDesign(rng), string.Empty))
        .Prepend(new Tuple<IDesignStandIn, string>(quickSelectedDesign,   string.Empty))
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
