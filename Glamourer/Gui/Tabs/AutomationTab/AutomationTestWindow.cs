using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Glamourer.Automation;
using Glamourer.Config;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using ImSharp;
using Luna;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationTestWindow : Window, IDisposable
{
    private readonly JobCombo            _jobCombo;
    private          int                 _gearSet = -1;
    private          Job?                _job;
    private readonly AutomationChanged   _changed;
    private readonly AutomationSelection _selection;
    private readonly Configuration       _config;
    private readonly ItemManager         _items;
    private readonly JobService          _jobs;
    private readonly TabSelected         _designSelection;

    public AutomationTestWindow(AutomationChanged changed,
        AutomationSelection selection,
        Configuration config,
        ItemManager items,
        JobService jobs,
        TabSelected designSelection)
        : base("Glamourer Automation Set Tester###Glamourer Automation Set Tester")
    {
        _changed         = changed;
        _selection       = selection;
        _config          = config;
        _items           = items;
        _jobs            = jobs;
        _designSelection = designSelection;
        _jobCombo        = new JobCombo(jobs);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600,  400),
            MaximumSize = new Vector2(4096, 4096),
        };
        _selection.SelectionChanged += OnSelectionChanged;
        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        WindowName = _selection.Set is null
            ? "Glamourer Automation Set Tester###Glamourer Automation Set Tester"
            : $"Glamourer Automation Set Tester ({_selection.Set!.Name})###Glamourer Automation Set Tester";
    }

    public override unsafe void Draw()
    {
        if (_job is null)
            if (!_jobs.Jobs.TryGetValue(PlayerState.Instance()->CurrentClassJobId, out _job))
                _job = _jobs.Jobs.Ordered[1];

        var cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current,
            () => new AutomationTestCache(_changed, _selection, _config, _items, _gearSet, _job.Id));


        if (_jobCombo.Draw("Current Job"u8, _job, "Select the job to check this automation set against."u8, 200 * Im.Style.GlobalScale,
                out var newJob))
        {
            _job      = newJob;
            cache.Job = newJob.Id;
        }

        Im.Line.Same();
        Im.Item.SetNextWidthScaled(50);
        Im.Drag("Gearset"u8, ref _gearSet, _gearSet < 0 ? "None##%i"u8 : "%i"u8, -1, 100, 0.02f,
            SliderFlags.AlwaysClamp);
        if (Im.Item.Deactivated)
            cache.GearSet = _gearSet;

        if (Im.Item.RightClicked())
        {
            _gearSet      = -1;
            cache.GearSet = -1;
        }

        Im.Tooltip.OnHover("Right-Click to remove gearset restriction."u8);

        LunaStyle.DrawSeparator();

        using var table = Im.Table.Begin("##table"u8, 3,
            TableFlags.RowBackground | TableFlags.ScrollX | TableFlags.ScrollY | TableFlags.BordersOuter, Im.ContentRegion.Available);
        if (!table)
            return;

        table.SetupColumn("Change"u8,        TableColumnFlags.WidthStretch, 1f / 3f);
        table.SetupColumn("Source Design"u8, TableColumnFlags.WidthStretch, 1f / 4f);
        table.SetupColumn("Applied Value"u8, TableColumnFlags.WidthStretch, 5f / 12f);
        table.SetupScrollFreeze(0, 1);

        table.HeaderRow();


        using var clip = new Im.ListClipper(cache.Count, Im.Style.TextHeight + Im.Style.CellPadding.Y * 2);
        foreach (var (idx, change) in clip.Iterate(cache).Index())
        {
            using var id = Im.Id.Push(idx);
            table.DrawColumn(change.Slot);
            table.NextColumn();
            var targetAvailable = change.Design.TryGetTarget(out var design);
            if (Im.Selectable(change.Source) && targetAvailable)
                _designSelection.Invoke(new TabSelected.Arguments(MainTabType.Designs, design));
            if (targetAvailable)
                Im.Tooltip.OnHover("Click to move to design."u8);
            table.DrawColumn(change.Target);
        }
    }

    private sealed class JobCombo(JobService jobs) : SimpleFilterCombo<Job>(SimpleFilterType.Text)
    {
        public override StringU8 DisplayString(in Job value)
            => value.Name;

        public override string FilterString(in Job value)
            => value.Name.ToString();

        public override IEnumerable<Job> GetBaseItems()
            => jobs.Jobs.Ordered;
    }

    public void Dispose()
        => _selection.SelectionChanged -= OnSelectionChanged;
}
