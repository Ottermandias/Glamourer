using Glamourer.Config;
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

public sealed class ActorSelector(
    ActorSelection selection,
    ActorObjectManager objects,
    ActorFilter filter,
    PenumbraService penumbra,
    Configuration config) : IPanel
{
    public ReadOnlySpan<byte> Id
        => "ActorSelector"u8;

    public unsafe void Draw()
    {
        Im.Cursor.Y += Im.Style.FramePadding.Y;
        var cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new ActorSelectorCache(objects, filter, penumbra));
        HandleRememberedSelection();
        using var clip = new Im.ListClipper(cache.Count, Im.Style.TextHeightWithSpacing);
        foreach (var actor in clip.Iterate(cache))
        {
            Im.Cursor.X += Im.Style.FramePadding.X;
            var selected = actor.Identifier.Equals(selection.Identifier);
            if (Im.Selectable(config.Ephemeral.IncognitoMode ? actor.IncognitoText : actor.DisplayText.Utf8, selected) && !selected)
                selection.Select(actor.Identifier, actor.Data);
        }
    }

    private unsafe void HandleRememberedSelection()
    {
        // We already have a valid selection.
        if (selection.Identifier.IsValid)
            return;

        // We do not have a remembered selection.
        if (!config.Ui.SelectedActor.IsValid)
            return;

        // We have no actor corresponding to the selection available to create a new state.
        if (!objects.TryGetValue(config.Ui.SelectedActor, out var data))
            return;

        // The actor has no model yet.
        if (data.Objects.First().Model is not { IsCharacterBase: true } model)
            return;

        // The model still has a staging area, so is not fully loaded.
        if (model.AsCharacterBase->PerSlotStagingArea is not null || model.AsCharacterBase->TempData is not null)
            return;

        // The model should be fully loaded, so the selection can probably create the expected state.
        selection.Select(config.Ui.SelectedActor, data);
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
