using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Table;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlockTable : Table<EquipItem>, IDisposable
{
    private readonly ObjectUnlocked _event;

    public UnlockTable(ItemManager items, TextureService textures, ItemUnlockManager itemUnlocks,
        PenumbraChangedItemTooltip tooltip, ObjectUnlocked @event, JobService jobs, FavoriteManager favorites)
        : base("ItemUnlockTable", new ItemList(items),
            new FavoriteColumn(favorites, @event) { Label = "F" },
            new NameColumn(textures, tooltip) { Label     = "Item Name..." },
            new SlotColumn() { Label                      = "Equip Slot" },
            new TypeColumn() { Label                      = "Item Type..." },
            new UnlockDateColumn(itemUnlocks) { Label     = "Unlocked" },
            new ItemIdColumn() { Label                    = "Item Id..." },
            new ModelDataColumn(items) { Label            = "Model Data..." },
            new JobColumn(jobs) { Label                   = "Jobs" },
            new RequiredLevelColumn() { Label             = "Level..." },
            new DyableColumn() { Label                    = "Dye" },
            new CrestColumn() { Label                     = "Crest" },
            new TradableColumn() { Label                  = "Trade" }
        )
    {
        _event   =  @event;
        Sortable =  true;
        Flags    |= ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Resizable;
        _event.Subscribe(OnObjectUnlock, ObjectUnlocked.Priority.UnlockTable);
    }

    public void Dispose()
        => _event.Unsubscribe(OnObjectUnlock);

    private sealed class FavoriteColumn : YesNoColumn<EquipItem>
    {
        public override float Width
            => ImGui.GetFrameHeightWithSpacing();

        private readonly FavoriteManager _favorites;
        private readonly ObjectUnlocked  _hackEvent; // used to trigger the table dirty.

        public FavoriteColumn(FavoriteManager favorites, ObjectUnlocked hackEvent)
        {
            _favorites =  favorites;
            _hackEvent =  hackEvent;
            Flags      |= ImGuiTableColumnFlags.NoResize;
        }

        protected override bool GetValue(EquipItem item)
            => _favorites.Contains(item);

        public override void DrawColumn(EquipItem item, int idx)
        {
            ImGui.AlignTextToFramePadding();
            if (UiHelpers.DrawFavoriteStar(_favorites, item))
                _hackEvent.Invoke(ObjectUnlocked.Type.Customization, 0, DateTimeOffset.Now);
        }

        public override bool FilterFunc(EquipItem item)
            => FilterValue.HasFlag(_favorites.Contains(item) ? YesNoFlag.Yes : YesNoFlag.No);

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => _favorites.Contains(rhs).CompareTo(_favorites.Contains(lhs));
    }

    private sealed class NameColumn : ColumnString<EquipItem>
    {
        private readonly TextureService             _textures;
        private readonly PenumbraChangedItemTooltip _tooltip;

        public override float Width
            => 400 * ImGuiHelpers.GlobalScale;

        public NameColumn(TextureService textures, PenumbraChangedItemTooltip tooltip)
        {
            _textures =  textures;
            _tooltip  =  tooltip;
            Flags     |= ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder;
        }

        public override string ToName(EquipItem item)
            => item.Name;

        public override void DrawColumn(EquipItem item, int _)
        {
            if (_textures.TryLoadIcon(item.IconId.Id, out var iconHandle))
                ImGuiUtil.HoverIcon(iconHandle, new Vector2(ImGui.GetFrameHeight()));
            else
                ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            if (ImGui.Selectable(item.Name))
                Glamourer.Messager.Chat.Print(new SeStringBuilder().AddItemLink(item.ItemId.Id, false).BuiltString);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _tooltip.Player(out var state))
                _tooltip.ApplyItem(state, item);

            if (ImGui.IsItemHovered() && _tooltip.Player())
                _tooltip.CreateTooltip(item, string.Empty, true);
        }
    }

    private sealed class TypeColumn : ColumnString<EquipItem>
    {
        public override float Width
            => ImGui.CalcTextSize(FullEquipType.CrossPeinHammer.ToName()).X;

        public override string ToName(EquipItem item)
            => item.Type.ToName();

        public override void DrawColumn(EquipItem item, int _)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(item.Type.ToName());
        }

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Type.CompareTo(rhs.Type);
    }

    private sealed class SlotColumn : ColumnFlags<EquipFlag, EquipItem>
    {
        public override float Width
            => ImGui.CalcTextSize("Equip Slotmm").X;

        private EquipFlag _filterValue;

        public SlotColumn()
        {
            Flags        &= ~ImGuiTableColumnFlags.NoResize;
            AllFlags     =  Values.Aggregate((a, b) => a | b);
            _filterValue =  AllFlags;
        }

        public override void DrawColumn(EquipItem item, int idx)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(ToString(item.Type.ToSlot()));
        }

        public override EquipFlag FilterValue
            => _filterValue;

        protected override IReadOnlyList<EquipFlag> Values
            => new[]
            {
                EquipFlag.Mainhand,
                EquipFlag.Offhand,
                EquipFlag.Head,
                EquipFlag.Body,
                EquipFlag.Hands,
                EquipFlag.Legs,
                EquipFlag.Feet,
                EquipFlag.Ears,
                EquipFlag.Neck,
                EquipFlag.Wrist,
                EquipFlag.RFinger,
            };

        protected override string[] Names
            => new[]
            {
                ToString(EquipSlot.MainHand),
                ToString(EquipSlot.OffHand),
                ToString(EquipSlot.Head),
                ToString(EquipSlot.Body),
                ToString(EquipSlot.Hands),
                ToString(EquipSlot.Legs),
                ToString(EquipSlot.Feet),
                ToString(EquipSlot.Ears),
                ToString(EquipSlot.Neck),
                ToString(EquipSlot.Wrists),
                ToString(EquipSlot.RFinger),
            };

        protected override void SetValue(EquipFlag value, bool enable)
            => _filterValue = enable ? _filterValue | value : _filterValue & ~value;

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Type.ToSlot().CompareTo(rhs.Type.ToSlot());

        public override bool FilterFunc(EquipItem item)
            => _filterValue.HasFlag(item.Type.ToSlot().ToFlag());

        private static string ToString(EquipSlot slot)
            => slot switch
            {
                EquipSlot.MainHand => "Mainhand",
                EquipSlot.OffHand  => "Offhand",
                EquipSlot.Head     => "Head",
                EquipSlot.Body     => "Body",
                EquipSlot.Hands    => "Hands",
                EquipSlot.Legs     => "Legs",
                EquipSlot.Feet     => "Feet",
                EquipSlot.Ears     => "Ears",
                EquipSlot.Neck     => "Neck",
                EquipSlot.Wrists   => "Wrists",
                EquipSlot.RFinger  => "Finger",
                _                  => string.Empty,
            };
    }

    private sealed class UnlockDateColumn : Column<EquipItem>
    {
        private readonly ItemUnlockManager _unlocks;

        public override float Width
            => 110 * ImGuiHelpers.GlobalScale;

        public UnlockDateColumn(ItemUnlockManager unlocks)
        {
            _unlocks =  unlocks;
            Flags    &= ~ImGuiTableColumnFlags.NoResize;
        }

        public override void DrawColumn(EquipItem item, int idx)
        {
            if (!_unlocks.IsUnlocked(item.ItemId, out var time))
                return;

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(time == DateTimeOffset.MinValue ? "Always" : time.LocalDateTime.ToString("g"));
        }

        public override int Compare(EquipItem lhs, EquipItem rhs)
        {
            var unlockedLhs = _unlocks.IsUnlocked(lhs.ItemId, out var timeLhs);
            var unlockedRhs = _unlocks.IsUnlocked(rhs.ItemId, out var timeRhs);
            var c1          = unlockedLhs.CompareTo(unlockedRhs);
            return c1 != 0 ? c1 : timeLhs.CompareTo(timeRhs);
        }
    }

    private sealed class ItemIdColumn : ColumnNumber<EquipItem>
    {
        public override float Width
            => 70 * ImGuiHelpers.GlobalScale;

        public override int ToValue(EquipItem item)
            => (int) item.Id.Id;

        public ItemIdColumn()
            : base(ComparisonMethod.Equal)
        { }
    }

    private sealed class ModelDataColumn : ColumnString<EquipItem>
    {
        private readonly ItemManager _items;

        public override float Width
            => 100 * ImGuiHelpers.GlobalScale;

        public ModelDataColumn(ItemManager items)
            => _items = items;

        public override void DrawColumn(EquipItem item, int _)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiUtil.RightAlign(item.ModelString);
            if (ImGui.IsItemHovered()
             && item.Type.ValidOffhand().IsOffhandType()
             && _items.ItemData.TryGetValue(item.ItemId, EquipSlot.OffHand, out var offhand))
            {
                using var tt = ImRaii.Tooltip();
                ImGui.TextUnformatted("Offhand: " + offhand.ModelString);
            }
        }

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.Weapon().CompareTo(rhs.Weapon());

        public override bool FilterFunc(EquipItem item)
        {
            if (FilterValue.Length == 0)
                return true;

            if (FilterRegex?.IsMatch(item.ModelString) ?? item.ModelString.Contains(FilterValue, StringComparison.OrdinalIgnoreCase))
                return true;

            if (item.Type.ValidOffhand().IsOffhandType()
             && _items.ItemData.TryGetValue(item.ItemId, EquipSlot.OffHand, out var offhand))
                return FilterRegex?.IsMatch(offhand.ModelString)
                 ?? offhand.ModelString.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }

    private sealed class RequiredLevelColumn : ColumnNumber<EquipItem>
    {
        public override float Width
            => 70 * ImGuiHelpers.GlobalScale;

        public override string ToName(EquipItem item)
            => item.Level.ToString();

        public override int ToValue(EquipItem item)
            => item.Level.Value;

        public RequiredLevelColumn()
            : base(ComparisonMethod.LessEqual)
        { }
    }


    private sealed class JobColumn : ColumnFlags<JobFlag, EquipItem>
    {
        public override float Width
            => 200 * ImGuiHelpers.GlobalScale;

        private readonly JobService _jobs;

        private readonly JobFlag[] _values;
        private readonly string[]  _names;
        private          JobFlag   _filterValue;

        public override JobFlag FilterValue
            => _filterValue;

        public JobColumn(JobService jobs)
        {
            _jobs        =  jobs;
            _values      =  _jobs.Jobs.Values.Skip(1).Select(j => j.Flag).ToArray();
            _names       =  _jobs.Jobs.Values.Skip(1).Select(j => j.Abbreviation).ToArray();
            AllFlags     =  _values.Aggregate((l, r) => l | r);
            _filterValue =  AllFlags;
            Flags        &= ~ImGuiTableColumnFlags.NoResize;
        }

        protected override void SetValue(JobFlag value, bool enable)
            => _filterValue = enable ? _filterValue | value : _filterValue & ~value;

        protected override IReadOnlyList<JobFlag> Values
            => _values;

        protected override string[] Names
            => _names;

        public override int Compare(EquipItem lhs, EquipItem rhs)
            => lhs.JobRestrictions.Id.CompareTo(rhs.JobRestrictions.Id);

        public override bool FilterFunc(EquipItem item)
        {
            if (item.JobRestrictions.Id < 2)
                return true;

            if (item.JobRestrictions.Id >= _jobs.AllJobGroups.Count)
                return false;

            var group = _jobs.AllJobGroups[item.JobRestrictions.Id];
            return group.Fits(FilterValue);
        }

        public override void DrawColumn(EquipItem item, int idx)
        {
            var text = $"Unknown {item.JobRestrictions.Id}";
            if (item.JobRestrictions.Id < _jobs.AllJobGroups.Count)
            {
                var group = _jobs.AllJobGroups[Math.Max((int)item.JobRestrictions.Id, 1)];
                if (group.Name.Length > 0)
                    text = group.Name;
            }

            ImGui.TextUnformatted(text);
        }
    }

    private sealed class DyableColumn : YesNoColumn<EquipItem>
    {
        public DyableColumn()
            => Tooltip = "Whether the item is dyable.";

        protected override bool GetValue(EquipItem item)
            => item.Flags.HasFlag(ItemFlags.IsDyable);
    }

    private sealed class TradableColumn : YesNoColumn<EquipItem>
    {
        public TradableColumn()
            => Tooltip = "Whether the item is tradable.";

        protected override bool GetValue(EquipItem item)
            => item.Flags.HasFlag(ItemFlags.IsTradable);
    }

    private sealed class CrestColumn : YesNoColumn<EquipItem>
    {
        public CrestColumn()
            => Tooltip = "Whether a crest can be applied to the item..";

        protected override bool GetValue(EquipItem item)
            => item.Flags.HasFlag(ItemFlags.IsCrestWorthy);
    }

    private sealed class ItemList(ItemManager items) : IReadOnlyCollection<EquipItem>
    {
        public IEnumerator<EquipItem> GetEnumerator()
            => items.ItemData.AllItems(true).Select(i => i.Item2).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => items.ItemData.Primary.Count;
    }

    private void OnObjectUnlock(ObjectUnlocked.Type _1, uint _2, DateTimeOffset _3)
    {
        FilterDirty = true;
        SortDirty   = true;
    }
}
