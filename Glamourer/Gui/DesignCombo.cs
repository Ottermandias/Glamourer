using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Automation;
using Glamourer.GameData;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

public abstract class DesignComboBase : FilterComboCache<Tuple<Design, string>>, IDisposable
{
    private readonly   EphemeralConfig _config;
    private readonly   DesignChanged   _designChanged;
    private readonly   DesignColors    _designColors;
    protected readonly TabSelected     TabSelected;
    protected          float           InnerWidth;
    private            Design?         _currentDesign;

    protected DesignComboBase(Func<IReadOnlyList<Tuple<Design, string>>> generator, Logger log, DesignChanged designChanged,
        TabSelected tabSelected, EphemeralConfig config, DesignColors designColors)
        : base(generator, log)
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
        => _designChanged.Unsubscribe(OnDesignChange);

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (design, path) = Items[globalIdx];
        bool ret;
        using (var color = ImRaii.PushColor(ImGuiCol.Text, _designColors.GetColor(design)))
        {
            ret = base.DrawSelectable(globalIdx, selected);
        }

        if (path.Length > 0 && design.Name != path)
        {
            var start          = ImGui.GetItemRectMin();
            var pos            = start.X + ImGui.CalcTextSize(design.Name).X;
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

        return ret;
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        CurrentSelectionIdx = Items.IndexOf(p => _currentDesign == p.Item1);
        CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        return CurrentSelectionIdx;
    }

    protected bool Draw(Design? currentDesign, string? label, float width)
    {
        _currentDesign = currentDesign;
        InnerWidth     = 400 * ImGuiHelpers.GlobalScale;
        var name = label ?? "Select Design Here...";
        var ret = Draw("##design", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing())
         && CurrentSelection != null;

        if (currentDesign != null)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                TabSelected.Invoke(MainWindow.TabType.Designs, currentDesign);
            ImGuiUtil.HoverTooltip("Control + Right-Click to move to design.");
        }

        _currentDesign = null;
        return ret;
    }

    protected override string ToString(Tuple<Design, string> obj)
        => obj.Item1.Name.Text;

    protected override float GetFilterWidth()
        => InnerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        var (design, path) = Items[globalIndex];
        return filter.IsContained(path) || design.Name.Lower.Contains(filter.Lower);
    }

    private void OnDesignChange(DesignChanged.Type type, Design design, object? data = null)
    {
        switch (type)
        {
            case DesignChanged.Type.Created:
            case DesignChanged.Type.Renamed:
                Cleanup();
                break;
            case DesignChanged.Type.Deleted:
                Cleanup();
                if (CurrentSelection?.Item1 == design)
                {
                    CurrentSelectionIdx = Items.Count > 0 ? 0 : -1;
                    CurrentSelection    = Items[CurrentSelectionIdx];
                }

                break;
        }
    }
}

public sealed class DesignCombo : DesignComboBase
{
    private readonly DesignManager _manager;

    public DesignCombo(DesignManager designs, DesignFileSystem fileSystem, Logger log, DesignChanged designChanged, TabSelected tabSelected,
        EphemeralConfig config, DesignColors designColors)
        : base(() => designs.Designs
            .Select(d => new Tuple<Design, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
            .OrderBy(d => d.Item2)
            .ToList(), log, designChanged, tabSelected, config, designColors)
    {
        _manager = designs;
        if (designs.Designs.Count == 0)
            return;

        CurrentSelection    = Items[0];
        CurrentSelectionIdx = 0;
    }

    public Design? Design
        => CurrentSelection?.Item1;

    public void Draw(float width)
    {
        Draw(Design, (Incognito ? Design?.Incognito : Design?.Name.Text) ?? string.Empty, width);
        if (ImGui.IsItemHovered() && _manager.Designs.Count > 1)
        {
            var mouseWheel = -(int)ImGui.GetIO().MouseWheel % _manager.Designs.Count;
            CurrentSelectionIdx = mouseWheel switch
            {
                < 0 when CurrentSelectionIdx < 0 => _manager.Designs.Count - 1 + mouseWheel,
                < 0                              => (CurrentSelectionIdx + _manager.Designs.Count + mouseWheel) % _manager.Designs.Count,
                > 0 when CurrentSelectionIdx < 0 => mouseWheel,
                > 0                              => (CurrentSelectionIdx + mouseWheel) % _manager.Designs.Count,
                _                                => CurrentSelectionIdx,
            };
            CurrentSelection = Items[CurrentSelectionIdx];
        }
    }
}

public sealed class RevertDesignCombo : DesignComboBase, IDisposable
{
    public const     int               RevertDesignIndex = -1228;
    public readonly  Design            RevertDesign;
    private readonly AutoDesignManager _autoDesignManager;

    public RevertDesignCombo(DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected, DesignColors designColors,
        ItemManager items, CustomizeService customize, Logger log, DesignChanged designChanged, AutoDesignManager autoDesignManager,
        EphemeralConfig config)
        : this(designs, fileSystem, tabSelected, designColors, CreateRevertDesign(customize, items), log, designChanged, autoDesignManager,
            config)
    { }

    private RevertDesignCombo(DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected, DesignColors designColors,
        Design revertDesign, Logger log, DesignChanged designChanged, AutoDesignManager autoDesignManager, EphemeralConfig config)
        : base(() => designs.Designs
            .Select(d => new Tuple<Design, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
            .OrderBy(d => d.Item2)
            .Prepend(new Tuple<Design, string>(revertDesign, string.Empty))
            .ToList(), log, designChanged, tabSelected, config, designColors)
    {
        RevertDesign       = revertDesign;
        _autoDesignManager = autoDesignManager;
    }


    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex)
    {
        if (!Draw(design?.Design, design?.Name(Incognito), ImGui.GetContentRegionAvail().X))
            return;

        if (autoDesignIndex >= 0)
            _autoDesignManager.ChangeDesign(set, autoDesignIndex, CurrentSelection!.Item1 == RevertDesign ? null : CurrentSelection!.Item1);
        else
            _autoDesignManager.AddDesign(set, CurrentSelection!.Item1 == RevertDesign ? null : CurrentSelection!.Item1);
    }

    private static Design CreateRevertDesign(CustomizeService customize, ItemManager items)
        => new(customize, items)
        {
            Index          = RevertDesignIndex,
            Name           = AutoDesign.RevertName,
            ApplyCustomize = CustomizeFlagExtensions.AllRelevant,
        };
}
