using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
using Glamourer.Unlocks;
using OtterGui.Classes;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Automation;

public class AutoDesignApplier : IDisposable
{
    private readonly Configuration          _config;
    private readonly AutoDesignManager      _manager;
    private readonly StateManager           _state;
    private readonly JobService             _jobs;
    private readonly EquippedGearset        _equippedGearset;
    private readonly ActorService           _actors;
    private readonly CustomizationService   _customizations;
    private readonly CustomizeUnlockManager _customizeUnlocks;
    private readonly ItemUnlockManager      _itemUnlocks;
    private readonly AutomationChanged      _event;
    private readonly ObjectManager          _objects;
    private readonly WeaponLoading          _weapons;
    private readonly HumanModelList         _humans;
    private readonly IClientState           _clientState;

    private          ActorState?                                                 _jobChangeState;
    private readonly Dictionary<FullEquipType, (EquipItem, StateChanged.Source)> _jobChangeMainhand = [];
    private readonly Dictionary<FullEquipType, (EquipItem, StateChanged.Source)> _jobChangeOffhand  = [];

    private void ResetJobChange()
    {
        _jobChangeState = null;
        _jobChangeMainhand.Clear();
        _jobChangeOffhand.Clear();
    }

    public AutoDesignApplier(Configuration config, AutoDesignManager manager, StateManager state, JobService jobs,
        CustomizationService customizations, ActorService actors, ItemUnlockManager itemUnlocks, CustomizeUnlockManager customizeUnlocks,
        AutomationChanged @event, ObjectManager objects, WeaponLoading weapons, HumanModelList humans, IClientState clientState,
        EquippedGearset equippedGearset)
    {
        _config           =  config;
        _manager          =  manager;
        _state            =  state;
        _jobs             =  jobs;
        _customizations   =  customizations;
        _actors           =  actors;
        _itemUnlocks      =  itemUnlocks;
        _customizeUnlocks =  customizeUnlocks;
        _event            =  @event;
        _objects          =  objects;
        _weapons          =  weapons;
        _humans           =  humans;
        _clientState      =  clientState;
        _equippedGearset  =  equippedGearset;
        _jobs.JobChanged  += OnJobChange;
        _event.Subscribe(OnAutomationChange, AutomationChanged.Priority.AutoDesignApplier);
        _weapons.Subscribe(OnWeaponLoading, WeaponLoading.Priority.AutoDesignApplier);
        _equippedGearset.Subscribe(OnEquippedGearset, EquippedGearset.Priority.AutoDesignApplier);
    }

    public void Dispose()
    {
        _weapons.Unsubscribe(OnWeaponLoading);
        _event.Unsubscribe(OnAutomationChange);
        _equippedGearset.Unsubscribe(OnEquippedGearset);
        _jobs.JobChanged -= OnJobChange;
    }

    private void OnWeaponLoading(Actor actor, EquipSlot slot, Ref<CharacterWeapon> weapon)
    {
        if (_jobChangeState == null || !_config.EnableAutoDesigns)
            return;

        var id = actor.GetIdentifier(_actors.AwaitedService);
        if (id == _jobChangeState.Identifier)
        {
            var current = _jobChangeState.BaseData.Item(slot);
            if (slot is EquipSlot.MainHand)
            {
                if (_jobChangeMainhand.TryGetValue(current.Type, out var data))
                {
                    Glamourer.Log.Verbose(
                        $"Changing Mainhand from {_jobChangeState.ModelData.Weapon(EquipSlot.MainHand)} | {_jobChangeState.BaseData.Weapon(EquipSlot.MainHand)} to {data.Item1} for 0x{actor.Address:X}.");
                    _state.ChangeItem(_jobChangeState, EquipSlot.MainHand, data.Item1, data.Item2);
                    weapon.Value = _jobChangeState.ModelData.Weapon(EquipSlot.MainHand);
                }
            }
            else if (slot is EquipSlot.OffHand && current.Type == _jobChangeState.BaseData.MainhandType.Offhand())
            {
                if (_jobChangeOffhand.TryGetValue(current.Type, out var data))
                {
                    Glamourer.Log.Verbose(
                        $"Changing Offhand from {_jobChangeState.ModelData.Weapon(EquipSlot.OffHand)} | {_jobChangeState.BaseData.Weapon(EquipSlot.OffHand)} to {data.Item1} for 0x{actor.Address:X}.");
                    _state.ChangeItem(_jobChangeState, EquipSlot.OffHand, data.Item1, data.Item2);
                    weapon.Value = _jobChangeState.ModelData.Weapon(EquipSlot.OffHand);
                }

                ResetJobChange();
            }
        }
        else
        {
            ResetJobChange();
        }
    }

    private void OnAutomationChange(AutomationChanged.Type type, AutoDesignSet? set, object? bonusData)
    {
        if (!_config.EnableAutoDesigns || set == null)
            return;

        void RemoveOld(ActorIdentifier[]? identifiers)
        {
            if (identifiers == null)
                return;

            foreach (var id in identifiers)
            {
                if (id.Type is IdentifierType.Player && id.HomeWorld == WorldId.AnyWorld)
                    foreach (var state in _state.Where(kvp => kvp.Key.PlayerName == id.PlayerName).Select(kvp => kvp.Value))
                        state.RemoveFixedDesignSources();
                else if (_state.TryGetValue(id, out var state))
                    state.RemoveFixedDesignSources();
            }
        }

        void ApplyNew(AutoDesignSet? newSet)
        {
            if (newSet is not { Enabled: true })
                return;

            _objects.Update();
            foreach (var id in newSet.Identifiers)
            {
                if (_objects.TryGetValue(id, out var data))
                {
                    if (_state.GetOrCreate(id, data.Objects[0], out var state))
                    {
                        Reduce(data.Objects[0], state, newSet, false, false);
                        foreach (var actor in data.Objects)
                            _state.ReapplyState(actor);
                    }
                }
                else if (_objects.TryGetValueAllWorld(id, out data) || _objects.TryGetValueNonOwned(id, out data))
                {
                    foreach (var actor in data.Objects)
                    {
                        var specificId = actor.GetIdentifier(_actors.AwaitedService);
                        if (_state.GetOrCreate(specificId, actor, out var state))
                        {
                            Reduce(actor, state, newSet, false, false);
                            _state.ReapplyState(actor);
                        }
                    }
                }
                else if (_state.TryGetValue(id, out var state))
                {
                    state.RemoveFixedDesignSources();
                }
            }
        }

        switch (type)
        {
            case AutomationChanged.Type.ToggleSet when !set.Enabled:
            case AutomationChanged.Type.DeletedDesign when set.Enabled:
                // The automation set was disabled or deleted, no other for those identifiers can be enabled, remove existing Fixed Locks.
                RemoveOld(set.Identifiers);
                break;
            case AutomationChanged.Type.ChangeIdentifier when set.Enabled:
                // Remove fixed state from the old identifiers assigned and the old enabled set, if any.
                var (oldIds, _, oldSet) = ((ActorIdentifier[], ActorIdentifier, AutoDesignSet?))bonusData!;
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
                ApplyNew(set);
                break;
        }
    }

    private void OnJobChange(Actor actor, Job oldJob, Job newJob)
    {
        if (!_config.EnableAutoDesigns || !actor.Identifier(_actors.AwaitedService, out var id))
            return;

        if (!GetPlayerSet(id, out var set))
        {
            if (_state.TryGetValue(id, out var s))
                s.LastJob = (byte)newJob.Id;
            return;
        }

        if (!_state.TryGetValue(id, out var state))
            return;

        if (oldJob.Id == newJob.Id && state.LastJob == newJob.Id)
            return;

        var respectManual = state.LastJob == newJob.Id;
        state.LastJob = actor.Job;
        Reduce(actor, state, set, respectManual, true);
        _state.ReapplyState(actor);
    }

    public void ReapplyAutomation(Actor actor, ActorIdentifier identifier, ActorState state)
    {
        if (!_config.EnableAutoDesigns)
            return;

        if (!GetPlayerSet(identifier, out var set))
            return;

        Reduce(actor, state, set, false, false);
    }

    public bool Reduce(Actor actor, ActorIdentifier identifier, [NotNullWhen(true)] out ActorState? state)
    {
        AutoDesignSet set;
        if (!_state.TryGetValue(identifier, out state))
        {
            if (!_config.EnableAutoDesigns)
                return false;

            if (!GetPlayerSet(identifier, out set!))
                return false;

            if (!_state.GetOrCreate(identifier, actor, out state))
                return false;
        }
        else if (!GetPlayerSet(identifier, out set!))
        {
            if (state.UpdateTerritory(_clientState.TerritoryType) && _config.RevertManualChangesOnZoneChange)
                _state.ResetState(state, StateChanged.Source.Game);
            return true;
        }

        var respectManual = !state.UpdateTerritory(_clientState.TerritoryType) || !_config.RevertManualChangesOnZoneChange;
        if (!respectManual)
            _state.ResetState(state, StateChanged.Source.Game);
        Reduce(actor, state, set, respectManual, false);
        return true;
    }

    private unsafe void Reduce(Actor actor, ActorState state, AutoDesignSet set, bool respectManual, bool fromJobChange)
    {
        EquipFlag     totalEquipFlags     = 0;
        CustomizeFlag totalCustomizeFlags = 0;
        CrestFlag     totalCrestFlags     = 0;
        byte          totalMetaFlags      = 0;
        if (set.BaseState == AutoDesignSet.Base.Game)
            _state.ResetStateFixed(state);
        else if (!respectManual)
            state.RemoveFixedDesignSources();

        if (!_humans.IsHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId))
            return;

        foreach (var design in set.Designs)
        {
            if (!design.IsActive(actor))
                continue;

            if (design.ApplicationType is 0)
                continue;

            ref readonly var data   = ref design.GetDesignData(state);
            var              source = design.Revert ? StateChanged.Source.Game : StateChanged.Source.Fixed;

            if (!data.IsHuman)
                continue;

            var (equipFlags, customizeFlags, crestFlags, applyHat, applyVisor, applyWeapon, applyWet) = design.ApplyWhat();
            ReduceMeta(state, data, applyHat, applyVisor, applyWeapon, applyWet, ref totalMetaFlags, respectManual, source);
            ReduceCustomize(state, data, customizeFlags, ref totalCustomizeFlags, respectManual, source);
            ReduceEquip(state, data, equipFlags, ref totalEquipFlags, respectManual, source, fromJobChange);
            ReduceCrests(state, data, crestFlags, ref totalCrestFlags, respectManual, source);
        }

        if (totalCustomizeFlags != 0)
            state.ModelData.ModelId = 0;
    }

    /// <summary> Get world-specific first and all-world afterwards. </summary>
    private bool GetPlayerSet(ActorIdentifier identifier, [NotNullWhen(true)] out AutoDesignSet? set)
    {
        switch (identifier.Type)
        {
            case IdentifierType.Player:
                if (_manager.EnabledSets.TryGetValue(identifier, out set))
                    return true;

                identifier = _actors.AwaitedService.CreatePlayer(identifier.PlayerName, ushort.MaxValue);
                return _manager.EnabledSets.TryGetValue(identifier, out set);
            case IdentifierType.Retainer:
            case IdentifierType.Npc:
                return _manager.EnabledSets.TryGetValue(identifier, out set);
            case IdentifierType.Owned:
                identifier = _actors.AwaitedService.CreateNpc(identifier.Kind, identifier.DataId);
                return _manager.EnabledSets.TryGetValue(identifier, out set);
            default:
                set = null;
                return false;
        }
    }

    private void ReduceCrests(ActorState state, in DesignData design, CrestFlag crestFlags, ref CrestFlag totalCrestFlags, bool respectManual,
        StateChanged.Source source)
    {
        crestFlags &= ~totalCrestFlags;
        if (crestFlags == 0)
            return;

        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            if (!crestFlags.HasFlag(slot))
                continue;

            if (!respectManual || state[slot] is not StateChanged.Source.Manual)
                _state.ChangeCrest(state, slot, design.Crest(slot), source);
            totalCrestFlags |= slot;
        }
    }

    private void ReduceEquip(ActorState state, in DesignData design, EquipFlag equipFlags, ref EquipFlag totalEquipFlags, bool respectManual,
        StateChanged.Source source, bool fromJobChange)
    {
        equipFlags &= ~totalEquipFlags;
        if (equipFlags == 0)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var flag = slot.ToFlag();
            if (equipFlags.HasFlag(flag))
            {
                var item = design.Item(slot);
                if (!_config.UnlockedItemMode || _itemUnlocks.IsUnlocked(item.Id, out _))
                {
                    if (!respectManual || state[slot, false] is not StateChanged.Source.Manual)
                        _state.ChangeItem(state, slot, item, source);
                    totalEquipFlags |= flag;
                }
            }

            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                if (!respectManual || state[slot, true] is not StateChanged.Source.Manual)
                    _state.ChangeStain(state, slot, design.Stain(slot), source);
                totalEquipFlags |= stainFlag;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.Mainhand))
        {
            var item        = design.Item(EquipSlot.MainHand);
            var checkUnlock = !_config.UnlockedItemMode || _itemUnlocks.IsUnlocked(item.Id, out _);
            var checkState  = !respectManual || state[EquipSlot.MainHand, false] is not StateChanged.Source.Manual;
            if (checkUnlock && checkState)
            {
                if (fromJobChange)
                {
                    _jobChangeMainhand.TryAdd(item.Type, (item, source));
                    _jobChangeState = state;
                }
                else if (state.ModelData.Item(EquipSlot.MainHand).Type == item.Type)
                {
                    _state.ChangeItem(state, EquipSlot.MainHand, item, source);
                    totalEquipFlags |= EquipFlag.Mainhand;
                }
            }
        }

        if (equipFlags.HasFlag(EquipFlag.Offhand))
        {
            var item        = design.Item(EquipSlot.OffHand);
            var checkUnlock = !_config.UnlockedItemMode || _itemUnlocks.IsUnlocked(item.Id, out _);
            var checkState  = !respectManual || state[EquipSlot.OffHand, false] is not StateChanged.Source.Manual;
            if (checkUnlock && checkState)
            {
                if (fromJobChange)
                {
                    _jobChangeOffhand.TryAdd(item.Type, (item, source));
                    _jobChangeState = state;
                }
                else if (state.ModelData.Item(EquipSlot.OffHand).Type == item.Type)
                {
                    _state.ChangeItem(state, EquipSlot.OffHand, item, source);
                    totalEquipFlags |= EquipFlag.Offhand;
                }
            }
        }

        if (equipFlags.HasFlag(EquipFlag.MainhandStain))
        {
            if (!respectManual || state[EquipSlot.MainHand, true] is not StateChanged.Source.Manual)
                _state.ChangeStain(state, EquipSlot.MainHand, design.Stain(EquipSlot.MainHand), source);
            totalEquipFlags |= EquipFlag.MainhandStain;
        }

        if (equipFlags.HasFlag(EquipFlag.OffhandStain))
        {
            if (!respectManual || state[EquipSlot.OffHand, true] is not StateChanged.Source.Manual)
                _state.ChangeStain(state, EquipSlot.OffHand, design.Stain(EquipSlot.OffHand), source);
            totalEquipFlags |= EquipFlag.OffhandStain;
        }
    }

    private void ReduceCustomize(ActorState state, in DesignData design, CustomizeFlag customizeFlags, ref CustomizeFlag totalCustomizeFlags,
        bool respectManual, StateChanged.Source source)
    {
        customizeFlags &= ~totalCustomizeFlags;
        if (customizeFlags == 0)
            return;

        var           customize = state.ModelData.Customize;
        CustomizeFlag fixFlags  = 0;

        // Skip anything not human.
        if (!state.ModelData.IsHuman || !design.IsHuman)
            return;

        if (customizeFlags.HasFlag(CustomizeFlag.Clan))
        {
            if (!respectManual || state[CustomizeIndex.Clan] is not StateChanged.Source.Manual)
                fixFlags |= _customizations.ChangeClan(ref customize, design.Customize.Clan);
            customizeFlags      &= ~(CustomizeFlag.Clan | CustomizeFlag.Race);
            totalCustomizeFlags |= CustomizeFlag.Clan | CustomizeFlag.Race;
        }

        if (customizeFlags.HasFlag(CustomizeFlag.Gender))
        {
            if (!respectManual || state[CustomizeIndex.Gender] is not StateChanged.Source.Manual)
                fixFlags |= _customizations.ChangeGender(ref customize, design.Customize.Gender);
            customizeFlags      &= ~CustomizeFlag.Gender;
            totalCustomizeFlags |= CustomizeFlag.Gender;
        }

        if (fixFlags != 0)
            _state.ChangeCustomize(state, customize, fixFlags, source);

        if (customizeFlags.HasFlag(CustomizeFlag.Face))
        {
            if (!respectManual || state[CustomizeIndex.Face] is not StateChanged.Source.Manual)
                _state.ChangeCustomize(state, CustomizeIndex.Face, design.Customize.Face, source);
            customizeFlags      &= ~CustomizeFlag.Face;
            totalCustomizeFlags |= CustomizeFlag.Face;
        }

        var set  = _customizations.AwaitedService.GetList(state.ModelData.Customize.Clan, state.ModelData.Customize.Gender);
        var face = state.ModelData.Customize.Face;
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var flag = index.ToFlag();
            if (!customizeFlags.HasFlag(flag))
                continue;

            var value = design.Customize[index];
            if (CustomizationService.IsCustomizationValid(set, face, index, value, out var data))
            {
                if (data.HasValue && _config.UnlockedItemMode && !_customizeUnlocks.IsUnlocked(data.Value, out _))
                    continue;

                if (!respectManual || state[index] is not StateChanged.Source.Manual)
                    _state.ChangeCustomize(state, index, value, source);
                totalCustomizeFlags |= flag;
            }
        }
    }

    private void ReduceMeta(ActorState state, in DesignData design, bool applyHat, bool applyVisor, bool applyWeapon, bool applyWet,
        ref byte totalMetaFlags, bool respectManual, StateChanged.Source source)
    {
        if (applyHat && (totalMetaFlags & 0x01) == 0)
        {
            if (!respectManual || state[ActorState.MetaIndex.HatState] is not StateChanged.Source.Manual)
                _state.ChangeHatState(state, design.IsHatVisible(), source);
            totalMetaFlags |= 0x01;
        }

        if (applyVisor && (totalMetaFlags & 0x02) == 0)
        {
            if (!respectManual || state[ActorState.MetaIndex.VisorState] is not StateChanged.Source.Manual)
                _state.ChangeVisorState(state, design.IsVisorToggled(), source);
            totalMetaFlags |= 0x02;
        }

        if (applyWeapon && (totalMetaFlags & 0x04) == 0)
        {
            if (!respectManual || state[ActorState.MetaIndex.WeaponState] is not StateChanged.Source.Manual)
                _state.ChangeWeaponState(state, design.IsWeaponVisible(), source);
            totalMetaFlags |= 0x04;
        }

        if (applyWet && (totalMetaFlags & 0x08) == 0)
        {
            if (!respectManual || state[ActorState.MetaIndex.Wetness] is not StateChanged.Source.Manual)
                _state.ChangeWetness(state, design.IsWet(), source);
            totalMetaFlags |= 0x08;
        }
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
        Reduce(data.Objects[0], state, set, respectManual, job != state.LastJob);
        NewGearsetId = -1;
        foreach (var actor in data.Objects)
            _state.ReapplyState(actor);
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
