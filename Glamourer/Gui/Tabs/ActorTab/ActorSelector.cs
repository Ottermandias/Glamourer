using Glamourer.Config;
using Glamourer.Interop.Penumbra;
using ImSharp;
using Luna;
using Penumbra.Api.Wrappers;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.ActorTab;

public readonly struct ActorCacheItem(ActorIdentifier identifier, ActorData data)
{
    public readonly ActorIdentifier Identifier    = identifier;
    public readonly ActorData       Data          = data;
    public readonly StringPair      DisplayText   = new(data.Label);
    public readonly StringU8        IncognitoText = new(identifier.Incognito(data.Label));
    private readonly (SubRace Clan, Gender Gender) _appearance = GetAppearance(data);

    public SubRace Clan
        => _appearance.Clan;

    public Gender Gender
        => _appearance.Gender;

    public ObjectIndex Index
        => Data.Objects[0].Index;

    private static (SubRace Clan, Gender Gender) GetAppearance(ActorData data)
    {
        var model = data.Objects.Select(actor => actor.Model).FirstOrDefault(model => model.IsHuman);
        if (!model.IsHuman)
            return (SubRace.Unknown, Gender.Unknown);

        var customize = model.GetCustomize();
        return (customize.Clan, NormalizeGender(customize.Gender));
    }

    private static Gender NormalizeGender(Gender gender)
        => gender switch
        {
            Gender.MaleNpc   => Gender.Male,
            Gender.FemaleNpc => Gender.Female,
            _                => gender,
        };
}

public sealed class ActorSelector(
    ActorSelection selection,
    ActorObjectManager objects,
    ActorFilter filter,
    PenumbraSubscriber penumbra,
    Configuration config) : IPanel
{
    public ReadOnlySpan<byte> Id
        => "ActorSelector"u8;

    public void Draw()
    {
        filter.DrawSelectors(Im.ContentRegion.Available with { Y = Im.Style.FrameHeight });
        Im.Cursor.Y += Im.Style.FramePadding.Y;
        var cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new ActorSelectorCache(objects, filter, penumbra, config));
        HandleRememberedSelection();
        using var child = Im.Child.Begin("ActorList"u8, Im.ContentRegion.Available);
        if (!child)
            return;

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
        private readonly PenumbraSubscriber _penumbra;
        private readonly Configuration      _config;

        public ActorSelectorCache(ActorObjectManager objects, ActorFilter filter, PenumbraSubscriber penumbra, Configuration config)
            : base(filter)
        {
            _objects                                 =  objects;
            _penumbra                                =  penumbra;
            _config                                  =  config;
            _objects.Objects.OnUpdateRequired        += OnUpdateRequired;
            _penumbra.GameState.CreatedCharacterBase += OnCreatedCharacterBase;
            _config.ActorSortModeChanged             += OnActorSortModeChanged;
        }

        /// <summary> Update actors when models are created since visible models are required. </summary>
        private void OnCreatedCharacterBase(in CreatedCharacterBaseArguments _)
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        /// <summary> Update actors when anything changes in the object table. </summary>
        private void OnUpdateRequired()
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        /// <summary> Update actors when the sort mode changes. </summary>
        private void OnActorSortModeChanged(ActorSortMode _1, ActorSortMode _2)
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _objects.Objects.OnUpdateRequired        -= OnUpdateRequired;
            _penumbra.GameState.CreatedCharacterBase -= OnCreatedCharacterBase;
            _config.ActorSortModeChanged             -= OnActorSortModeChanged;
        }

        private static readonly StringU8 PlayerSortOrder = new("0"u8);

        protected override IEnumerable<ActorCacheItem> GetItems()
        {
            var enumerable = _objects.Where(p => p.Value.Objects.Any(a => a.Model)).Select(a => new ActorCacheItem(a.Key, a.Value));
            return _config.ActorSortMode switch
            {
                ActorSortMode.Default                    => enumerable,
                ActorSortMode.Lexicographical            => enumerable.OrderBy(a => a.DisplayText.Utf8),
                ActorSortMode.LexicographicalPlayerFirst => enumerable.OrderBy(a => a.Index == 0 ? PlayerSortOrder : a.DisplayText.Utf8),
                _                                        => enumerable,
            };
        }
    }
}
