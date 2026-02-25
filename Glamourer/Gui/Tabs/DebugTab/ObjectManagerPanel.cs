using ImSharp;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class ObjectManagerPanel(ActorObjectManager objectManager, ActorManager actors) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Object Manager"u8;

    public bool Disabled
        => false;

    private sealed class Filter : TextFilterBase<CacheItem>
    {
        protected override string ToFilterString(in CacheItem item, int globalIndex)
            => item.Label.Utf16;
    }

    private readonly struct CacheItem(ActorIdentifier identifier, ActorData data)
    {
        public readonly StringPair Label   = new(data.Label);
        public readonly StringU8   Name    = new($"{identifier}");
        public readonly StringU8   Objects = StringU8.Join(", "u8, data.Objects.OrderBy(a => a.Index).Select(a => a.Index));
    }

    private sealed class Cache : BasicFilterCache<CacheItem>
    {
        private readonly ActorObjectManager _objectManager;

        public Cache(ActorObjectManager objectManager, Filter filter)
            : base(filter)
        {
            _objectManager                  =  objectManager;
            _objectManager.Objects.OnUpdate += OnUpdate;
        }

        private void OnUpdate()
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _objectManager.Objects.OnUpdate -= OnUpdate;
        }

        protected override IEnumerable<CacheItem> GetItems()
            => _objectManager.Select(o => new CacheItem(o.Key, o.Value));
    }

    private readonly Filter _filter = new();

    public void Draw()
    {
        objectManager.Objects.DrawDebug();

        using (var table = Im.Table.Begin("##data"u8, 3, TableFlags.RowBackground | TableFlags.SizingFixedFit))
        {
            if (!table)
                return;

            table.DrawColumn("World"u8);
            table.DrawColumn(actors.Finished ? actors.Data.ToWorldName(objectManager.World) : "Service Missing"u8);
            table.DrawColumn($"{objectManager.World}");

            table.DrawColumn("Player Character"u8);
            table.DrawColumn($"{objectManager.Player.Utf8Name} ({objectManager.Player.Index})");
            table.NextColumn();
            Glamourer.Dynamis.DrawPointer(objectManager.Player.Address);

            table.DrawColumn("In GPose"u8);
            table.DrawColumn($"{objectManager.IsInGPose}");
            table.NextColumn();

            table.DrawColumn("In Lobby"u8);
            table.DrawColumn($"{objectManager.IsInLobby}");
            table.NextColumn();

            if (objectManager.IsInGPose)
            {
                table.DrawColumn("GPose Player"u8);
                table.DrawColumn($"{objectManager.GPosePlayer.Utf8Name} ({objectManager.GPosePlayer.Index})");
                table.NextColumn();
                Glamourer.Dynamis.DrawPointer(objectManager.GPosePlayer.Address);
            }

            table.DrawColumn("Number of Players"u8);
            table.DrawColumn($"{objectManager.Count}");
            table.NextColumn();
        }

        var filterChanged = _filter.DrawFilter("Filter..."u8, Im.ContentRegion.Available);
        using var table2 = Im.Table.Begin("##data2"u8, 3, TableFlags.RowBackground | TableFlags.BordersOuter | TableFlags.ScrollY,
            Im.ContentRegion.Available with { Y = 20 * Im.Style.TextHeightWithSpacing });
        if (!table2)
            return;

        if (filterChanged)
            Im.Scroll.Y = 0;

        var       cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new Cache(objectManager, _filter));
        using var clip  = new Im.ListClipper(cache.Count, Im.Style.TextHeightWithSpacing);
        foreach (var item in clip.Iterate(cache))
        {
            table2.DrawColumn(item.Name);
            table2.DrawColumn(item.Label.Utf8);
            table2.DrawColumn(item.Objects);
        }
    }
}
