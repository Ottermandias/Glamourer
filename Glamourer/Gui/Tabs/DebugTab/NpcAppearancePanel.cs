using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class NpcAppearancePanel(
    NpcCustomizeSet npcData,
    StateManager stateManager,
    ActorObjectManager objectManager,
    DesignConverter designConverter)
    : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "NPC Appearance"u8;

    public bool Disabled
        => false;

    private readonly NpcDataFilter _filter = new();
    private          bool          _customizeOrGear;

    private sealed class NpcDataFilter : TextFilterBase<CacheItem>
    {
        protected override string ToFilterString(in CacheItem item, int globalIndex)
            => item.Name.Utf16;
    }

    private readonly struct CacheItem(NpcData data)
    {
        public readonly NpcData     Data          = data;
        public readonly StringPair  Name          = new(data.Name);
        public readonly StringU8    DataId        = new($"{data.Id.Id}");
        public readonly StringU8    ModelId       = new($"{data.ModelId}");
        public readonly AwesomeIcon Visor         = data.VisorToggled ? LunaStyle.TrueIcon : LunaStyle.FalseIcon;
        public readonly StringU8    CustomizeData = new($"{data.Customize}");
        public readonly StringU8    GearData      = new(data.WriteGear());
    }

    private sealed class Cache(NpcCustomizeSet npcData, NpcDataFilter filter) : BasicFilterCache<CacheItem>(filter)
    {
        protected override IEnumerable<CacheItem> GetItems()
            => npcData.Select(i => new CacheItem(i));
    }

    public void Draw()
    {
        Im.Checkbox("Compare Customize (or Gear)"u8, ref _customizeOrGear);
        var resetScroll = _filter.DrawFilter("Filter..."u8, Im.ContentRegion.Available);

        using var table = Im.Table.Begin("npcs"u8, 7, TableFlags.RowBackground | TableFlags.ScrollY | TableFlags.SizingFixedFit,
            Im.ContentRegion.Available with { Y = 400 * Im.Style.GlobalScale });
        if (!table)
            return;
        
        if (resetScroll)
            Im.Scroll.Y = 0;
        
        table.SetupColumn("Button"u8,  TableColumnFlags.WidthFixed);
        table.SetupColumn("Name"u8,    TableColumnFlags.WidthFixed, Im.Style.GlobalScale * 300);
        table.SetupColumn("Kind"u8,    TableColumnFlags.WidthFixed);
        table.SetupColumn("Id"u8,      TableColumnFlags.WidthFixed);
        table.SetupColumn("Model"u8,   TableColumnFlags.WidthFixed);
        table.SetupColumn("Visor"u8,   TableColumnFlags.WidthFixed);
        table.SetupColumn("Compare"u8, TableColumnFlags.WidthStretch);
        
        var       cache   = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new Cache(npcData, _filter));
        using var clipper = new Im.ListClipper(cache.Count, Im.Style.FrameHeightWithSpacing);
        foreach (var (idx, data) in clipper.Iterate(cache).Index())
        {
            using var id       = Im.Id.Push(idx);
            var       disabled = !stateManager.GetOrCreate(objectManager.Player, out var state);
            table.NextColumn();
            if (ImEx.Button("Apply"u8, Vector2.Zero, StringU8.Empty, disabled))
            {
                foreach (var (slot, item, stain) in designConverter.FromDrawData(data.Data.Equip(), data.Data.Mainhand, data.Data.Offhand, true))
                    stateManager.ChangeEquip(state!, slot, item, stain, ApplySettings.Manual);
                stateManager.ChangeMetaState(state!, MetaIndex.VisorState, data.Data.VisorToggled, ApplySettings.Manual);
                stateManager.ChangeEntireCustomize(state!, data.Data.Customize, CustomizeFlagExtensions.All, ApplySettings.Manual);
            }

            table.DrawFrameColumn(data.Name.Utf8);
            table.DrawFrameColumn(data.Data.Kind is ObjectKind.BattleNpc ? "B"u8 : "E"u8);
            table.DrawFrameColumn(data.DataId);
            table.DrawFrameColumn(data.ModelId);
            table.NextColumn();
            ImEx.Icon.DrawAligned(data.Visor);
            using var mono = Im.Font.PushMono();
            table.DrawFrameColumn(_customizeOrGear ? data.CustomizeData : data.GearData);
        }
    }
}
