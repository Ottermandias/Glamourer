using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Table;
using Glamourer.Config;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using ImSharp.Table;
using Luna;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public sealed class UnlockTable : TableBase<UnlockCacheItem, UnlockTable.Cache>, IUiService
{
    private readonly JobService        _jobs;
    private readonly ItemManager       _items;
    private readonly ItemUnlockManager _unlocks;
    private readonly FavoriteManager   _favorites;
    private readonly PenumbraService   _penumbra;
    private readonly ObjectUnlocked    _unlockEvent;
    private readonly IgnoredMods       _ignoredMods;

    public UnlockTable(JobService jobs, ItemManager items, ItemUnlockManager unlocks, PenumbraChangedItemTooltip tooltip,
        ObjectUnlocked unlockEvent, FavoriteManager favorites, PenumbraService penumbra, TextureService textures, IgnoredMods ignoredMods)
        : base(new StringU8("Unlock Table"u8), new FavoriteColumn(favorites), new ModdedColumn(), new NameColumn(textures, tooltip),
            new SlotColumn(), new TypeColumn(), new UnlockDateColumn(), new ItemIdColumn(), new ModelDataColumn(), new JobColumn(jobs),
            new RequiredLevelColumn(), new DyableColumn(), new CrestColumn(), new TradableColumn())
    {
        _jobs        = jobs;
        _items       = items;
        _unlocks     = unlocks;
        _unlockEvent = unlockEvent;
        _favorites   = favorites;
        _penumbra    = penumbra;
        _ignoredMods = ignoredMods;

        Flags |= TableFlags.Hideable | TableFlags.Reorderable | TableFlags.Resizable;
    }

    public override (int Columns, int Rows) GetFrozenScroll()
        => (3, 1);

    public override IEnumerable<UnlockCacheItem> GetItems()
        => _items.ItemData.AllItems(true).Select(p => ToCacheItem(p.Item2));

    private UnlockCacheItem ToCacheItem(EquipItem item)
    {
        var       unlocked = _unlocks.IsUnlocked(item.Id, out var time) ? time : DateTimeOffset.MaxValue;
        EquipItem offhand  = default;
        EquipItem gauntlet = default;
        if (item.Type.ValidOffhand().IsOffhandType())
        {
            _items.ItemData.TryGetValue(item.ItemId, EquipSlot.OffHand, out offhand);
            if (item.Type is FullEquipType.Fists)
            {
                _items.ItemData.TryGetValue(item.ItemId, EquipSlot.Hands, out gauntlet);
                if (gauntlet.Type is not FullEquipType.Hands)
                    gauntlet = default;
            }
        }

        var favorite = _favorites.Contains(item);
        var mods     = _penumbra.CheckCurrentChangedItem(item.Name);
        var jobs = item.JobRestrictions.Id > 0 && item.JobRestrictions.Id < _jobs.AllJobGroups.Count
            ? _jobs.AllJobGroups[item.JobRestrictions]
            : _jobs.AllJobGroups[1];
        return new UnlockCacheItem(item, offhand, gauntlet, jobs)
        {
            UnlockTimestamp = unlocked,
            Mods            = mods,
            Favorite        = favorite,
            RelevantMods    = mods.Count(m => !_ignoredMods.Contains(m.ModName) && !_ignoredMods.Contains(m.ModDirectory)),
        };
    }

    protected override Cache CreateCache()
        => new(this);

    private sealed class FavoriteColumn : YesNoColumn<UnlockCacheItem>
    {
        private readonly FavoriteManager _favorites;

        public FavoriteColumn(FavoriteManager favorites)
        {
            _favorites  =  favorites;
            Flags       |= TableColumnFlags.NoResize;
            Label       =  new StringU8("F"u8);
            FilterLabel =  new StringU8("Favorite"u8);
        }

        public override float ComputeWidth(IEnumerable<UnlockCacheItem> allItems)
            => Im.Style.FrameHeightWithSpacing;

        public override void DrawColumn(in UnlockCacheItem item, int globalIndex)
            => UiHelpers.DrawFavoriteStar(_favorites, item.Item);

        protected override bool GetValue(in UnlockCacheItem item, int globalIndex, int triEnumIndex)
            => item.Favorite;
    }

    private sealed class ModdedColumn : FlagColumn<ModdedColumn.Modded, UnlockCacheItem>
    {
        [Flags]
        public enum Modded
        {
            Relevant = 1,
            Ignored  = 2,
            None     = 4,
        }

        private static readonly AwesomeIcon Dot    = FontAwesomeIcon.Circle;
        private static readonly AwesomeIcon Hollow = FontAwesomeIcon.DotCircle;

        public ModdedColumn()
        {
            Flags |= TableColumnFlags.NoResize;
            Label =  new StringU8("M");
        }

        public override float ComputeWidth(IEnumerable<UnlockCacheItem> allItems)
            => Im.Style.FrameHeightWithSpacing;

        public override void DrawColumn(in UnlockCacheItem item, int globalIndex)
        {
            if (item.Mods.Length is 0)
                return;

            using (AwesomeIcon.Font.Push())
            {
                var (color, text) = item.RelevantMods > 0
                    ? (ColorId.ModdedItemMarker.Value(), Dot)
                    : (ColorId.ModdedItemMarker.Value().HalfTransparent(), Hollow);
                using var c = ImGuiColor.Text.Push(color);
                Im.Text(text.Span);
            }

            if (Im.Item.Hovered())
            {
                using var style = Im.Style.PushDefault();
                using var tt    = Im.Tooltip.Begin();
                foreach (var (_, mod) in item.Mods)
                    Im.BulletText(mod);
            }
        }

        protected override Modded GetValue(in UnlockCacheItem item, int globalIndex)
            => item.RelevantMods > 0 ? Modded.Relevant : item.Mods.Length > 0 ? Modded.Ignored : Modded.None;

        protected override StringU8 DisplayString(in UnlockCacheItem item, int globalIndex)
            => StringU8.Empty;

        protected override IReadOnlyList<(Modded Value, StringU8 Name)> EnumData
            =>
            [
                (Modded.Relevant, new StringU8("Any Relevant Mods"u8)),
                (Modded.Ignored, new StringU8("Only Ignored Mods"u8)),
                (Modded.None, new StringU8("Unmodded"u8)),
            ];


        public override int Compare(in UnlockCacheItem lhs, int lhsGlobalIndex, in UnlockCacheItem rhs, int rhsGlobalIndex)
        {
            var relevant = lhs.RelevantMods.CompareTo(rhs.RelevantMods);
            if (relevant is not 0)
                return relevant;

            return lhs.Mods.Length.CompareTo(rhs.Mods.Length);
        }
    }

    private sealed class NameColumn : TextColumn<UnlockCacheItem>
    {
        private readonly TextureService             _textures;
        private readonly PenumbraChangedItemTooltip _tooltip;

        public NameColumn(TextureService textures, PenumbraChangedItemTooltip tooltip)
        {
            _textures     =  textures;
            _tooltip      =  tooltip;
            Flags         |= TableColumnFlags.NoHide | TableColumnFlags.NoReorder;
            Label         =  new StringU8("Item Name..."u8);
            UnscaledWidth =  400;
        }

        public override void DrawColumn(in UnlockCacheItem item, int _)
        {
            if (_textures.TryLoadIcon(item.Item.IconId.Id, out var iconHandle))
                Im.Image.DrawScaled(iconHandle.Id, new Vector2(Im.Style.FrameHeight), iconHandle.Size);
            else
                Im.Dummy(new Vector2(Im.Style.FrameHeight));
            Im.Line.Same();
            Im.Cursor.FrameAlign();
            if (Im.Selectable(item.Name.Utf8) && item.Item.Id is { IsBonusItem: false, IsCustom: false })
                Glamourer.Messager.Chat.Print(new SeStringBuilder().AddItemLink(item.Item.ItemId.Id, false).BuiltString);

            if (Im.Item.RightClicked() && _tooltip.Player(out var state))
                _tooltip.ApplyItem(state, item.Item);

            if (Im.Item.Hovered() && _tooltip.Player())
                _tooltip.CreateTooltip(item.Item, string.Empty, true);
        }

        protected override string ComparisonText(in UnlockCacheItem item, int globalIndex)
            => item.Name.Utf16;

        protected override StringU8 DisplayText(in UnlockCacheItem item, int globalIndex)
            => item.Name.Utf8;
    }

    private sealed class TypeColumn : TextColumn<UnlockCacheItem>
    {
        public TypeColumn()
            => Label = new StringU8("Item Type..."u8);

        public override float ComputeWidth(IEnumerable<UnlockCacheItem> _)
            => FullEquipType.CrossPeinHammer.ToNameU8().CalculateSize().X;

        protected override string ComparisonText(in UnlockCacheItem item, int globalIndex)
            => string.Empty;

        public override int Compare(in UnlockCacheItem lhs, int lhsGlobalIndex, in UnlockCacheItem rhs, int rhsGlobalIndex)
            => lhs.Item.Type.CompareTo(rhs.Item.Type);

        protected override StringU8 DisplayText(in UnlockCacheItem item, int globalIndex)
            => item.Item.Type.ToNameU8();
    }

    private sealed class SlotColumn : FlagColumn<EquipFlag, UnlockCacheItem>
    {
        public SlotColumn()
        {
            Flags &= ~TableColumnFlags.NoResize;
            Label =  new StringU8("Equip Slot"u8);
        }

        public override float ComputeWidth(IEnumerable<UnlockCacheItem> _)
            => Im.Font.CalculateButtonSize(Label).X + Table.ArrowWidth;

        protected override StringU8 DisplayString(in UnlockCacheItem item, int globalIndex)
        {
            var slot = item.Slot;
            return EnumData.FindFirst(i => i.Value == slot, out var pair) ? pair.Name : StringU8.Empty;
        }

        protected override IReadOnlyList<(EquipFlag Value, StringU8 Name)> EnumData { get; } =
        [
            (EquipFlag.Head, EquipSlot.Head.ToNameU8()),
            (EquipFlag.Body, EquipSlot.Body.ToNameU8()),
            (EquipFlag.Hands, EquipSlot.Hands.ToNameU8()),
            (EquipFlag.Legs, EquipSlot.Legs.ToNameU8()),
            (EquipFlag.Feet, EquipSlot.Feet.ToNameU8()),
            (EquipFlag.Ears, new StringU8("Ears"u8)),
            (EquipFlag.Neck, new StringU8("Neck"u8)),
            (EquipFlag.Wrist, new StringU8("Wrists"u8)),
            (EquipFlag.RFinger, new StringU8("Finger"u8)),
            (EquipFlag.Mainhand, new StringU8("Mainhand"u8)),
            (EquipFlag.Offhand, new StringU8("Offhand"u8)),
        ];

        protected override EquipFlag GetValue(in UnlockCacheItem item, int globalIndex)
            => item.Slot;
    }

    private sealed class UnlockDateColumn : YesNoColumn<UnlockCacheItem>
    {
        public UnlockDateColumn()
        {
            Flags       &= ~TableColumnFlags.NoResize;
            Label       =  new StringU8("Unlocked"u8);
            FilterLabel =  Label;
        }

        public override float ComputeWidth(IEnumerable<UnlockCacheItem> allItems)
            => Im.Font.CalculateButtonSize(Label).X + Table.ArrowWidth;

        public override void DrawColumn(in UnlockCacheItem item, int globalIndex)
        {
            if (item.UnlockTimestamp == DateTimeOffset.MaxValue)
                return;

            Im.Cursor.FrameAlign();
            Im.Text(item.UnlockText);
        }

        protected override bool GetValue(in UnlockCacheItem item, int globalIndex, int triEnumIndex)
            => item.UnlockTimestamp != DateTimeOffset.MaxValue;

        public override int Compare(in UnlockCacheItem lhs, int lhsGlobalIndex, in UnlockCacheItem rhs, int rhsGlobalIndex)
            => lhs.UnlockTimestamp.CompareTo(rhs.UnlockTimestamp);
    }

    private sealed class ItemIdColumn : NumberColumn<uint, UnlockCacheItem>
    {
        public ItemIdColumn()
        {
            Label         = new StringU8("Item Id..."u8);
            UnscaledWidth = 70;
        }

        public override uint ToValue(in UnlockCacheItem item, int globalIndex)
            => item.Item.ItemId;

        protected override StringU8 DisplayNumber(in UnlockCacheItem item, int globalIndex)
            => item.ItemId;

        protected override string ComparisonText(in UnlockCacheItem item, int globalIndex)
            => item.ItemId;
    }

    private sealed class ModelDataColumn : TextColumn<UnlockCacheItem>
    {
        public ModelDataColumn()
        {
            Label         = new StringU8("Model Data..."u8);
            UnscaledWidth = 100;
        }

        public override void DrawColumn(in UnlockCacheItem item, int globalIndex)
        {
            Im.Cursor.FrameAlign();
            ImEx.TextRightAligned(item.ModelString.Utf8);
            if (!item.OffhandModelString.IsEmpty && Im.Item.Hovered())
            {
                using var style = Im.Style.PushDefault();
                using var tt    = Im.Tooltip.Begin();
                using (Im.Group())
                {
                    Im.Text("Offhand: "u8);
                    if (!item.GauntletModelString.IsEmpty)
                        Im.Text("Gauntlets: "u8);
                }

                Im.Line.Same();
                using (Im.Group())
                {
                    Im.Text(item.OffhandModelString.Utf8);
                    if (!item.GauntletModelString.IsEmpty)
                        Im.Text(item.GauntletModelString.Utf8);
                }
            }
        }

        public override int Compare(in UnlockCacheItem lhs, int lhsGlobalIndex, in UnlockCacheItem rhs, int rhsGlobalIndex)
            => lhs.Item.Weapon().CompareTo(rhs.Item.Weapon());

        public override bool WouldBeVisible(in UnlockCacheItem item, int globalIndex)
            => Filter.WouldBeVisible(item.ModelString.Utf16)
             || Filter.WouldBeVisible(item.OffhandModelString.Utf16)
             || Filter.WouldBeVisible(item.GauntletModelString.Utf16);

        protected override string ComparisonText(in UnlockCacheItem item, int globalIndex)
            => string.Empty;

        protected override StringU8 DisplayText(in UnlockCacheItem item, int globalIndex)
            => StringU8.Empty;
    }

    private sealed class RequiredLevelColumn : NumberColumn<byte, UnlockCacheItem>
    {
        public RequiredLevelColumn()
        {
            Label         = new StringU8("Level..."u8);
            UnscaledWidth = 70;
        }

        public override byte ToValue(in UnlockCacheItem item, int globalIndex)
            => item.Item.Level.Value;

        protected override StringU8 DisplayNumber(in UnlockCacheItem item, int globalIndex)
            => item.RequiredLevel;

        protected override string ComparisonText(in UnlockCacheItem item, int globalIndex)
            => item.RequiredLevel.Utf16;
    }

    private sealed class JobColumn : FlagColumn<JobFlag, UnlockCacheItem>
    {
        private readonly JobService _jobs;

        public JobColumn(JobService jobs)
            : base(false)
        {
            _jobs         =  jobs;
            Flags         &= ~TableColumnFlags.NoResize;
            EnumData      =  _jobs.Jobs.Ordered.Select(j => (j.Flag, j.Abbreviation)).ToArray();
            Label         =  new StringU8("Jobs"u8);
            Filter        =  new JobFilter(this);
            UnscaledWidth =  200;
        }

        protected override StringU8 DisplayString(in UnlockCacheItem item, int globalIndex)
            => item.JobText;

        protected override IReadOnlyList<(JobFlag Value, StringU8 Name)> EnumData { get; }

        protected override JobFlag GetValue(in UnlockCacheItem item, int globalIndex)
            => item.Jobs;

        private sealed class JobFilter : FlagFilter
        {
            public JobFilter(JobColumn parent)
                : base(parent)
            {
                ComboFlags |= ComboFlags.HeightLargest;
                ComboFlags &= ~ComboFlags.HeightLarge;
            }

            public override bool WouldBeVisible(in UnlockCacheItem item, int globalIndex)
                => (FilterValue & item.Jobs) is not 0;

            protected override bool DrawCheckbox(int idx)
            {
                var jobs = ((JobColumn)Parent)._jobs.Jobs.Ordered;
                var job  = jobs[(JobId)idx];
                var color = job.Role switch
                {
                    Job.JobRole.Tank           => 0xFFFFD0D0,
                    Job.JobRole.Melee          => 0xFFD0D0FF,
                    Job.JobRole.RangedPhysical => 0xFFD0FFFF,
                    Job.JobRole.RangedMagical  => 0xFFFFD0FF,
                    Job.JobRole.Healer         => 0xFFD0FFD0,
                    Job.JobRole.Crafter        => 0xFF808080,
                    Job.JobRole.Gatherer       => 0xFFD0D0D0,
                    _                          => ImGuiColor.Text.Get(),
                };
                bool r;
                using (ImGuiColor.Text.Push(color))
                {
                    r = base.DrawCheckbox(idx);
                }

                if (idx < jobs.Count - 1 && idx % 2 is 0)
                    Im.Line.Same(Im.Style.FrameHeight * 4);
                return r;
            }
        }
    }

    private sealed class DyableColumn : FlagColumn<UnlockCacheItem.Dyability, UnlockCacheItem>
    {
        public DyableColumn()
        {
            Flags &= ~TableColumnFlags.NoResize;
            Label =  new StringU8("Dye"u8);
        }

        public override float ComputeWidth(IEnumerable<UnlockCacheItem> _)
            => Im.Font.CalculateButtonSize("Dye"u8).X + Table.ArrowWidth;

        public override void DrawColumn(in UnlockCacheItem item, int globalIndex)
        {
            var icon = item.Dyable switch
            {
                UnlockCacheItem.Dyability.Yes => LunaStyle.TrueIcon,
                UnlockCacheItem.Dyability.Two => FontAwesomeIcon.DiceTwo,
                _                             => LunaStyle.FalseIcon,
            };
            using (AwesomeIcon.Font.Push())
            {
                using var color = ImGuiColor.Text.Push(Im.Style[ImGuiColor.CheckMark]);
                ImEx.TextCentered(icon.Span);
            }

            Im.Tooltip.OnHover("Whether the item is dyable, and how many dye slots it has."u8);
        }

        protected override StringU8 DisplayString(in UnlockCacheItem item, int globalIndex)
            => StringU8.Empty;

        protected override IReadOnlyList<(UnlockCacheItem.Dyability Value, StringU8 Name)> EnumData { get; }
            =
            [
                (UnlockCacheItem.Dyability.No, new StringU8("No"u8)),
                (UnlockCacheItem.Dyability.Yes, new StringU8("Yes"u8)),
                (UnlockCacheItem.Dyability.Two, new StringU8("Two"u8)),
            ];

        protected override UnlockCacheItem.Dyability GetValue(in UnlockCacheItem item, int globalIndex)
            => item.Dyable;
    }

    private sealed class TradableColumn : LunaStyle.YesNoColumn<UnlockCacheItem>
    {
        public TradableColumn()
            => Label = new StringU8("Trade"u8);

        protected override bool GetValue(in UnlockCacheItem item, int globalIndex, int triEnumIndex)
            => item.Tradable;
    }

    private sealed class CrestColumn : LunaStyle.YesNoColumn<UnlockCacheItem>
    {
        public CrestColumn()
            => Label = new StringU8("Crest"u8);

        protected override bool GetValue(in UnlockCacheItem item, int globalIndex, int triEnumIndex)
            => item.Crest;
    }

    public sealed class Cache : TableCache<UnlockCacheItem>
    {
        private new UnlockTable Parent
            => (UnlockTable)base.Parent;

        private Guid _lastCollection;

        public Cache(UnlockTable parent)
            : base(parent)
        {
            parent._unlockEvent.Subscribe(OnItemUnlock, ObjectUnlocked.Priority.UnlockTable);
            parent._penumbra.ModSettingChanged += OnModSettingChanged;
            Parent._favorites.FavoriteChanged  += OnFavoriteChanged;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Parent._unlockEvent.Unsubscribe(OnItemUnlock);
            Parent._penumbra.ModSettingChanged -= OnModSettingChanged;
            Parent._favorites.FavoriteChanged  -= OnFavoriteChanged;
        }

        private void OnFavoriteChanged(FavoriteManager.FavoriteType type, uint id, bool favorite)
        {
            if (type is not FavoriteManager.FavoriteType.Item and not FavoriteManager.FavoriteType.BonusItem)
                return;

            FilterDirty = true;
            SortDirty   = true;
            var idx = type is FavoriteManager.FavoriteType.Item
                ? UnfilteredItems.IndexOf(i => i.Item.ItemId == id)
                : UnfilteredItems.IndexOf(i => i.Item.Id.BonusItem == id);
            if (idx >= 0)
                UpdateSingleItem(idx, UnfilteredItems[idx] with { Favorite = favorite }, false);
        }

        private void OnModSettingChanged(ModSettingChange type, Guid collection, string _2, bool inherited)
        {
            if (collection != _lastCollection)
                return;

            FilterDirty = true;
            SortDirty   = true;
            for (var i = 0; i < UnfilteredItems.Count; ++i)
            {
                var item = UnfilteredItems[i];
                UpdateSingleItem(i, item with { Mods = Parent._penumbra.CheckCurrentChangedItem(item.Name.Utf16) }, false);
            }
        }

        private void OnItemUnlock(in ObjectUnlocked.Arguments arguments)
        {
            if (arguments.Type is not ObjectUnlocked.Type.Item)
                return;

            FilterDirty = true;
            SortDirty   = true;
            var id  = arguments.Id;
            var idx = UnfilteredItems.IndexOf(i => i.Item.ItemId == id);
            if (idx >= 0)
                UpdateSingleItem(idx, UnfilteredItems[idx] with { UnlockTimestamp = arguments.Timestamp }, false);
        }

        public override void Update()
        {
            UpdateCollection();
            base.Update();
        }

        private void UpdateCollection()
        {
            var collection = Parent._penumbra.CurrentCollection.Id;
            if (collection == _lastCollection)
                return;

            _lastCollection = collection;
            FilterDirty     = true;
            SortDirty       = true;
            for (var i = 0; i < UnfilteredItems.Count; ++i)
            {
                var item = UnfilteredItems[i];
                UpdateSingleItem(i, item with { Mods = Parent._penumbra.CheckCurrentChangedItem(item.Name.Utf16) }, false);
            }
        }
    }
}
