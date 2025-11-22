using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Material;
using Glamourer.State;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Automation;

public sealed class AutoDesignApplier : IDisposable
{
    private readonly Configuration      _config;
    private readonly AutoDesignManager  _manager;
    private readonly StateManager       _state;
    private readonly JobService         _jobs;
    private readonly EquippedGearset    _equippedGearset;
    private readonly ActorManager       _actors;
    private readonly AutomationChanged  _event;
    private readonly ActorObjectManager _objects;
    private readonly WeaponLoading      _weapons;
    private readonly HumanModelList     _humans;
    private readonly DesignMerger       _designMerger;
    private readonly IClientState       _clientState;

    private readonly JobChangeState _jobChangeState;

    public AutoDesignApplier(Configuration config, AutoDesignManager manager, StateManager state, JobService jobs, ActorManager actors,
        AutomationChanged @event, ActorObjectManager objects, WeaponLoading weapons, HumanModelList humans, IClientState clientState,
        EquippedGearset equippedGearset, DesignMerger designMerger, JobChangeState jobChangeState)
    {
        _config          =  config;
        _manager         =  manager;
        _state           =  state;
        _jobs            =  jobs;
        _actors          =  actors;
        _event           =  @event;
        _objects         =  objects;
        _weapons         =  weapons;
        _humans          =  humans;
        _clientState     =  clientState;
        _equippedGearset =  equippedGearset;
        _designMerger    =  designMerger;
        _jobChangeState  =  jobChangeState;
        _jobs.JobChanged += OnJobChange;
        _event.Subscribe(OnAutomationChange, AutomationChanged.Priority.AutoDesignApplier);
        _weapons.Subscribe(OnWeaponLoading, WeaponLoading.Priority.AutoDesignApplier);
        _equippedGearset.Subscribe(OnEquippedGearset, EquippedGearset.Priority.AutoDesignApplier);
    }

    public void OnEnableAutoDesignsChanged(bool value)
    {
        if (value)
            return;

        foreach (var state in _state.Values)
            state.Sources.RemoveFixedDesignSources();
    }

    public void Dispose()
    {
        _weapons.Unsubscribe(OnWeaponLoading);
        _event.Unsubscribe(OnAutomationChange);
        _equippedGearset.Unsubscribe(OnEquippedGearset);
        _jobs.JobChanged -= OnJobChange;
    }

    private void OnWeaponLoading(Actor actor, EquipSlot slot, ref CharacterWeapon weapon)
    {
        if (!_jobChangeState.HasState || !_config.EnableAutoDesigns)
            return;

        var id = actor.GetIdentifier(_actors);
        if (id == _jobChangeState.Identifier)
        {
            var state   = _jobChangeState.State!;
            var current = state.BaseData.Item(slot);
            switch (slot)
            {
                case EquipSlot.MainHand:
                {
                    if (_jobChangeState.TryGetValue(current.Type, actor.Job, false, out var data))
                    {
                        Glamourer.Log.Verbose(
                            $"Changing Mainhand from {state.ModelData.Weapon(EquipSlot.MainHand)} | {state.BaseData.Weapon(EquipSlot.MainHand)} to {data.Item1} for 0x{actor.Address:X}.");
                        _state.ChangeItem(state, EquipSlot.MainHand, data.Item1, new ApplySettings(Source: data.Item2));
                        weapon = state.ModelData.Weapon(EquipSlot.MainHand);
                    }

                    break;
                }
                case EquipSlot.OffHand when current.Type == state.BaseData.MainhandType.Offhand():
                {
                    if (_jobChangeState.TryGetValue(current.Type, actor.Job, false, out var data))
                    {
                        Glamourer.Log.Verbose(
                            $"Changing Offhand from {state.ModelData.Weapon(EquipSlot.OffHand)} | {state.BaseData.Weapon(EquipSlot.OffHand)} to {data.Item1} for 0x{actor.Address:X}.");
                        _state.ChangeItem(state, EquipSlot.OffHand, data.Item1, new ApplySettings(Source: data.Item2));
                        weapon = state.ModelData.Weapon(EquipSlot.OffHand);
                    }

                    _jobChangeState.Reset();
                    break;
                }
            }
        }
        else
        {
            _jobChangeState.Reset();
        }
    }

    private void OnAutomationChange(AutomationChanged.Type type, AutoDesignSet? set, object? bonusData)
    {
        if (!_config.EnableAutoDesigns || set == null)
            return;

        switch (type)
        {
            case AutomationChanged.Type.ToggleSet when !set.Enabled:
            case AutomationChanged.Type.DeletedDesign when set.Enabled:
                // The automation set was disabled or deleted, no other for those identifiers can be enabled, remove existing Fixed Locks.
                RemoveOld(set.Identifiers);
                break;
            case AutomationChanged.Type.ChangeIdentifier when set.Enabled:
                // Remove fixed state from the old identifiers assigned and the old enabled set, if any.
                var (oldIds, _, _) = ((ActorIdentifier[], ActorIdentifier, AutoDesignSet?))bonusData!;
                RemoveOld(oldIds);
                ApplyNew(set); // Does not need to disable oldSet because same identifiers.
                break;
            case AutomationChanged.Type.ToggleSet: // Does not need to disable old states because same identifiers.
            case AutomationChanged.Type.ChangedBase:
            case AutomationChanged.Type.AddedDesign:
            case AutomationChanged.Type.MovedDesign:
            case AutomationChanged.Type.ChangedDesign:
            case AutomationChanged.Type.ChangedConditions:
            case AutomationChanged.Type.ChangedType:
            case AutomationChanged.Type.ChangedData:
                ApplyNew(set);
                break;
        }

        return;

        void ApplyNew(AutoDesignSet? newSet)
        {
            if (newSet is not { Enabled: true })
                return;

            foreach (var id in newSet.Identifiers)
            {
                // If the stored identifier uses a wildcard in the player name, it will not directly
                // be present in the ActorObjectManager dictionaries. Scan the live objects and
                // apply to any matching actors instead.
                if (!id.PlayerName.IsEmpty && id.PlayerName.ToString().Contains('*'))
                {
                    var pattern = id.PlayerName.ToString();
                    var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*");
                    foreach (var (key, data2) in _objects)
                    {
                        if (key.Type != id.Type)
                            continue;

                        var worldMatches = key.Type switch
                        {
                            IdentifierType.Player => key.HomeWorld == id.HomeWorld || key.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld || id.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld,
                            IdentifierType.Owned  => key.HomeWorld == id.HomeWorld || key.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld || id.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld,
                            _ => true,
                        };

                        if (!worldMatches)
                            continue;

                        if (!System.Text.RegularExpressions.Regex.IsMatch(key.PlayerName.ToString(), $"^{regexPattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            continue;

                        // Skip this actor if there's an exact-match automation set already enabled for it.
                        if (_manager.EnabledSets.ContainsKey(key))
                            continue;

                        // Apply to all actors represented by this key.
                        foreach (var actor in data2.Objects)
                        {
                            var specificId = actor.GetIdentifier(_actors);
                            if (_state.GetOrCreate(specificId, actor, out var state))
                            {
                                Reduce(actor, state, newSet, _config.RespectManualOnAutomationUpdate, false, true, out var forcedRedraw);
                                _state.ReapplyAutomationState(actor, forcedRedraw, false, StateSource.Fixed);
                            }
                        }
                    }

                    continue;
                }
                if (_objects.TryGetValue(id, out var data))
                {
                    if (_state.GetOrCreate(id, data.Objects[0], out var state))
                    {
                        Reduce(data.Objects[0], state, newSet, _config.RespectManualOnAutomationUpdate, false, true, out var forcedRedraw);
                        foreach (var actor in data.Objects)
                            _state.ReapplyAutomationState(actor, forcedRedraw, false, StateSource.Fixed);
                    }
                }
                else if (_objects.TryGetValueAllWorld(id, out data) || _objects.TryGetValueNonOwned(id, out data))
                {
                    foreach (var actor in data.Objects)
                    {
                        var specificId = actor.GetIdentifier(_actors);
                        if (_state.GetOrCreate(specificId, actor, out var state))
                        {
                            Reduce(actor, state, newSet, _config.RespectManualOnAutomationUpdate, false, true, out var forcedRedraw);
                            _state.ReapplyAutomationState(actor, forcedRedraw, false, StateSource.Fixed);
                        }
                    }
                }
                else if (_state.TryGetValue(id, out var state))
                {
                    state.Sources.RemoveFixedDesignSources();
                }
            }
        }

        void RemoveOld(ActorIdentifier[]? identifiers)
        {
            if (identifiers == null)
                return;

            foreach (var id in identifiers)
            {
                if (id.Type is IdentifierType.Player && id.HomeWorld == WorldId.AnyWorld)
                    foreach (var state in _state.Where(kvp => kvp.Key.PlayerName == id.PlayerName).Select(kvp => kvp.Value))
                        state.Sources.RemoveFixedDesignSources();
                else if (_state.TryGetValue(id, out var state))
                    state.Sources.RemoveFixedDesignSources();
            }
        }
    }

    private void OnJobChange(Actor actor, Job oldJob, Job newJob)
    {
        if (!_config.EnableAutoDesigns || !actor.Identifier(_actors, out var id))
            return;

        if (!GetPlayerSet(id, out var set))
        {
            if (_state.TryGetValue(id, out var s))
                s.LastJob = newJob.Id;
            return;
        }

        if (!_state.GetOrCreate(actor, out var state))
            return;

        if (oldJob.Id == newJob.Id && state.LastJob == newJob.Id)
            return;

        var respectManual = state.LastJob == newJob.Id;
        state.LastJob = actor.Job;
        Reduce(actor, state, set, respectManual, true, true, out var forcedRedraw);
        _state.ReapplyState(actor, forcedRedraw, StateSource.Fixed);
    }

    public void ReapplyAutomation(Actor actor, ActorIdentifier identifier, ActorState state, bool reset, bool forcedNew, out bool forcedRedraw)
    {
        forcedRedraw = false;
        if (!_config.EnableAutoDesigns)
            return;

        if (reset)
            _state.ResetState(state, StateSource.Game);

        if (GetPlayerSet(identifier, out var set))
            Reduce(actor, state, set, false, false, forcedNew, out forcedRedraw);
    }

    public bool Reduce(Actor actor, ActorIdentifier identifier, [NotNullWhen(true)] out ActorState? state)
    {
        AutoDesignSet set;
        if (!_state.TryGetValue(identifier, out state))
        {
            if (!GetPlayerSet(identifier, out set!))
                return false;

            if (!_state.GetOrCreate(identifier, actor, out state))
                return false;
        }
        else if (!GetPlayerSet(identifier, out set!))
        {
            if (state.UpdateTerritory(_clientState.TerritoryType) && _config.RevertManualChangesOnZoneChange)
                _state.ResetState(state, StateSource.Game);
            return true;
        }

        var respectManual = !state.UpdateTerritory(_clientState.TerritoryType) || !_config.RevertManualChangesOnZoneChange;
        if (!respectManual)
            _state.ResetState(state, StateSource.Game);
        Reduce(actor, state, set, respectManual, false, false, out _);
        return true;
    }

    private unsafe void Reduce(Actor actor, ActorState state, AutoDesignSet set, bool respectManual, bool fromJobChange, bool newApplication,
        out bool forcedRedraw)
    {
        if (set.BaseState is AutoDesignSet.Base.Game)
        {
            _state.ResetStateFixed(state, respectManual);
        }
        else if (!respectManual)
        {
            state.Sources.RemoveFixedDesignSources();
            for (var i = 0; i < state.Materials.Values.Count; ++i)
            {
                var (key, value) = state.Materials.Values[i];
                if (value.Source is StateSource.Fixed)
                    state.Materials.UpdateValue(key, new MaterialValueState(value.Game, value.Model, value.DrawData, StateSource.Manual),
                        out _);
            }
        }

        forcedRedraw = false;
        if (!_humans.IsHuman((uint)actor.AsCharacter->ModelContainer.ModelCharaId))
            return;

        if (actor.IsTransformed)
            return;

        var mergedDesign = _designMerger.Merge(
            set.Designs.Where(d => d.IsActive(actor))
                .SelectMany(d => d.Design.AllLinks(newApplication).Select(l => (l.Design, l.Flags & d.Type, d.Jobs.Flags))),
            state.ModelData.Customize, state.BaseData, true, _config.AlwaysApplyAssociatedMods);

        if (_objects.IsInGPose && actor.IsGPoseOrCutscene)
        {
            mergedDesign.ResetTemporarySettings = false;
            mergedDesign.AssociatedMods.Clear();
        }
        else if (set.ResetTemporarySettings)
        {
            mergedDesign.ResetTemporarySettings = true;
        }

        _state.ApplyDesign(state, mergedDesign, new ApplySettings(0, StateSource.Fixed, respectManual, fromJobChange, false, false, false));
        forcedRedraw = mergedDesign.ForcedRedraw;
    }

    /// <summary> Get world-specific first and all-world afterward. </summary>
    private bool GetPlayerSet(ActorIdentifier identifier, [NotNullWhen(true)] out AutoDesignSet? set)
    {
        if (!_config.EnableAutoDesigns)
        {
            set = null;
            return false;
        }

        switch (identifier.Type)
        {
            case IdentifierType.Player:
                if (TryGettingSetExactOrWildcard(identifier, out set))
                    return true;

                identifier = _actors.CreatePlayer(identifier.PlayerName, WorldId.AnyWorld);
                if (TryGettingSetExactOrWildcard(identifier, out set))
                    return true;

                set = null;
                return false;
            case IdentifierType.Retainer:
            case IdentifierType.Npc:
                return TryGettingSetExactOrWildcard(identifier, out set);
            case IdentifierType.Owned:
                if (TryGettingSetExactOrWildcard(identifier, out set))
                    return true;

                identifier = _actors.CreateOwned(identifier.PlayerName, WorldId.AnyWorld, identifier.Kind, identifier.DataId);
                if (TryGettingSetExactOrWildcard(identifier, out set))
                    return true;

                identifier = _actors.CreateNpc(identifier.Kind, identifier.DataId);
                return TryGettingSetExactOrWildcard(identifier, out set);
            default:
                set = null;
                return false;
        }
    }

    /// <summary> Try to get a set matching exactly or via wildcard pattern. </summary>
    private bool TryGettingSetExactOrWildcard(ActorIdentifier identifier, [NotNullWhen(true)] out AutoDesignSet? set)
    {
        // First try exact match
        if (_manager.EnabledSets.TryGetValue(identifier, out set))
            return true;

        // Then try wildcard matches
        foreach (var (key, value) in _manager.EnabledSets)
        {
            // Use wildcard-aware matching when the stored identifier contains a wildcard pattern in the name.
            if (!key.PlayerName.IsEmpty && key.PlayerName.ToString().Contains('*'))
            {
                var sameType = identifier.Type == key.Type;
                if (!sameType)
                    continue;

                var worldMatches = identifier.Type switch
                {
                    Penumbra.GameData.Enums.IdentifierType.Player => identifier.HomeWorld == key.HomeWorld || identifier.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld || key.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld,
                    Penumbra.GameData.Enums.IdentifierType.Owned  => identifier.HomeWorld == key.HomeWorld || identifier.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld || key.HomeWorld == Penumbra.GameData.Structs.WorldId.AnyWorld,
                    _ => true,
                };

                if (!worldMatches)
                    continue;

                var name = identifier.PlayerName.ToString();
                var pattern = key.PlayerName.ToString();
                var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*");
                if (System.Text.RegularExpressions.Regex.IsMatch(name, $"^{regexPattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    set = value;
                    return true;
                }
            }
            else if (identifier.Matches(key))
            {
                set = value;
                return true;
            }
        }

        set = null;
        return false;
    }

    internal static int NewGearsetId = -1;

    private void OnEquippedGearset(string name, int id, int prior, byte _, byte job)
    {
        if (!_config.EnableAutoDesigns)
            return;

        var (player, data) = _objects.PlayerData;
        if (!player.IsValid)
            return;

        if (!GetPlayerSet(player, out var set) || !_state.TryGetValue(player, out var state))
            return;

        var respectManual = prior == id;
        NewGearsetId = id;
        Reduce(data.Objects[0], state, set, respectManual, job != state.LastJob, prior == id, out var forcedRedraw);
        NewGearsetId = -1;
        foreach (var actor in data.Objects)
            _state.ReapplyState(actor, forcedRedraw, StateSource.Fixed);
    }

    public static unsafe bool CheckGearset(short check)
    {
        if (NewGearsetId != -1)
            return check == NewGearsetId;

        var module = RaptureGearsetModule.Instance();
        if (module == null)
            return false;

        return check == module->CurrentGearsetIndex;
    }
}
