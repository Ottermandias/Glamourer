using Glamourer.Interop.Penumbra;
using ImSharp;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.ActorTab;

public readonly struct ActorCacheItem(ActorIdentifier identifier, ActorData data)
{
    public readonly ActorIdentifier Identifier    = identifier;
    public readonly ActorData       Data          = data;
    public readonly StringPair      DisplayText   = new(data.Label);
    public readonly StringU8        IncognitoText = new(identifier.Incognito(data.Label));
}

public sealed class ActorSelector(ActorSelection selection, ActorObjectManager objects, ActorFilter filter, PenumbraService penumbra, Config.EphemeralConfig config) : IPanel
{
    public ReadOnlySpan<byte> Id
        => "ActorSelector"u8;

    public void Draw()
    {
        Im.Cursor.Y += Im.Style.FramePadding.Y;
        var       cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new ActorSelectorCache(objects, filter, penumbra));
        using var clip  = new Im.ListClipper(cache.Count, Im.Style.TextHeightWithSpacing);
        foreach (var actor in clip.Iterate(cache))
        {
            Im.Cursor.X += Im.Style.FramePadding.X;
            var selected = actor.Identifier.Equals(selection.Identifier);
            if (Im.Selectable(config.IncognitoMode ? actor.IncognitoText : actor.DisplayText.Utf8, selected) && !selected)
                selection.Select(actor.Identifier, actor.Data);
        }
    }

    private sealed class ActorSelectorCache : BasicFilterCache<ActorCacheItem>
    {
        private readonly ActorObjectManager _objects;
        private readonly PenumbraService    _penumbra;

        public ActorSelectorCache(ActorObjectManager objects, ActorFilter filter, PenumbraService penumbra)
            : base(filter)
        {
            _objects                          =  objects;
            _penumbra                         =  penumbra;
            _objects.Objects.OnUpdateRequired += OnUpdateRequired;
            _penumbra.CreatedCharacterBase    += OnCreatedCharacterBase;
        }

        /// <summary> Update actors when models are created since visible models are required. </summary>
        private void OnCreatedCharacterBase(nint _1, Guid _2, IntPtr _3)
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        /// <summary> Update actors when anything changes in the object table. </summary>
        private void OnUpdateRequired()
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _objects.Objects.OnUpdateRequired -= OnUpdateRequired;
            _penumbra.CreatedCharacterBase    -= OnCreatedCharacterBase;
        }

        protected override IEnumerable<ActorCacheItem> GetItems()
            => _objects.Where(p => p.Value.Objects.Any(a => a.Model)).Select(a => new ActorCacheItem(a.Key, a.Value));
    }
}
