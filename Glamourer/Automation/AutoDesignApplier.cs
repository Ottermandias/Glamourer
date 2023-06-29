using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;

namespace Glamourer.Automation;

public class AutoDesignApplier : IDisposable
{
    private readonly Configuration        _config;
    private readonly AutoDesignManager    _manager;
    private readonly PhrasingService      _phrasing;
    private readonly StateManager         _state;
    private readonly JobService           _jobs;
    private readonly ActorService         _actors;
    private readonly CustomizationService _customizations;

    public AutoDesignApplier(Configuration config, AutoDesignManager manager, PhrasingService phrasing, StateManager state, JobService jobs,
        CustomizationService customizations, ActorService actors)
    {
        _config          =  config;
        _manager         =  manager;
        _phrasing        =  phrasing;
        _state           =  state;
        _jobs            =  jobs;
        _customizations  =  customizations;
        _actors          =  actors;
        _jobs.JobChanged += OnJobChange;
    }

    public void Dispose()
    {
        _jobs.JobChanged -= OnJobChange;
    }

    private void OnJobChange(Actor actor, Job _)
    {
        if (!_config.EnableAutoDesigns || !actor.Identifier(_actors.AwaitedService, out var id))
            return;

        if (!GetPlayerSet(id, out var set))
        {
            if (_state.TryGetValue(id, out var s))
                s.LastJob = actor.Job;
            return;
        }

        if (!_state.GetOrCreate(id, actor, out var state))
            return;

        var sameJob = state.LastJob == actor.Job;
        state.LastJob = actor.Job;
        Reduce(actor, state, set, sameJob);
        _state.ReapplyState(actor);
    }

    public void Reduce(Actor actor, ActorIdentifier identifier, ActorState state)
    {
        if (!_config.EnableAutoDesigns)
            return;

        if (!GetPlayerSet(identifier, out var set))
            return;

        Reduce(actor, state, set, true);
    }

    private unsafe void Reduce(Actor actor, ActorState state, AutoDesignSet set, bool respectManual)
    {
        EquipFlag totalEquipFlags     = 0;
        var       totalCustomizeFlags = _phrasing.Phrasing2 ? 0 : CustomizeFlagExtensions.RedrawRequired;
        byte      totalMetaFlags      = 0;
        foreach (var design in set.Designs)
        {
            if (!design.IsActive(actor))
                continue;

            if (design.ApplicationType is 0)
                continue;

            if (actor.AsCharacter->CharacterData.ModelCharaId != design.Design.DesignData.ModelId)
                continue;

            var (equipFlags, customizeFlags, applyHat, applyVisor, applyWeapon, applyWet) = design.ApplyWhat();
            Reduce(state, in design.Design.DesignData, equipFlags,     ref totalEquipFlags, respectManual);
            Reduce(state, in design.Design.DesignData, customizeFlags, ref totalCustomizeFlags, respectManual);
            Reduce(state, in design.Design.DesignData, applyHat,       applyVisor, applyWeapon, applyWet, ref totalMetaFlags, respectManual);
        }
    }

    /// <summary> Get world-specific first and all-world afterwards. </summary>
    private bool GetPlayerSet(ActorIdentifier identifier, [NotNullWhen(true)] out AutoDesignSet? set)
    {
        if (identifier.Type is not IdentifierType.Player)
            return _manager.EnabledSets.TryGetValue(identifier, out set);

        if (_manager.EnabledSets.TryGetValue(identifier, out set))
            return true;

        identifier = _actors.AwaitedService.CreatePlayer(identifier.PlayerName, ushort.MaxValue);
        return _manager.EnabledSets.TryGetValue(identifier, out set);
    }

    private void Reduce(ActorState state, in DesignData design, EquipFlag equipFlags, ref EquipFlag totalEquipFlags, bool respectManual)
    {
        equipFlags &= ~totalEquipFlags;
        if (equipFlags == 0)
            return;

        // TODO add item conditions
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var flag = slot.ToFlag();
            if (equipFlags.HasFlag(flag))
            {
                if (!respectManual || state[slot, false] is not StateChanged.Source.Manual)
                    _state.ChangeItem(state, slot, design.Item(slot), StateChanged.Source.Fixed);
                totalEquipFlags |= flag;
            }

            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                if (!respectManual || state[slot, true] is not StateChanged.Source.Manual)
                    _state.ChangeStain(state, slot, design.Stain(slot), StateChanged.Source.Fixed);
                totalEquipFlags |= stainFlag;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.Mainhand))
        {
            var item = design.Item(EquipSlot.MainHand);
            if (state.ModelData.Item(EquipSlot.MainHand).Type == item.Type)
            {
                if (!respectManual || state[EquipSlot.MainHand, false] is not StateChanged.Source.Manual)
                    _state.ChangeItem(state, EquipSlot.MainHand, item, StateChanged.Source.Fixed);
                totalEquipFlags |= EquipFlag.Mainhand;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.Offhand))
        {
            var item = design.Item(EquipSlot.OffHand);
            if (state.ModelData.Item(EquipSlot.OffHand).Type == item.Type)
            {
                if (!respectManual || state[EquipSlot.OffHand, false] is not StateChanged.Source.Manual)
                    _state.ChangeItem(state, EquipSlot.OffHand, item, StateChanged.Source.Fixed);
                totalEquipFlags |= EquipFlag.Offhand;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.MainhandStain))
        {
            if (!respectManual || state[EquipSlot.MainHand, true] is not StateChanged.Source.Manual)
                _state.ChangeStain(state, EquipSlot.MainHand, design.Stain(EquipSlot.MainHand), StateChanged.Source.Fixed);
            totalEquipFlags |= EquipFlag.MainhandStain;
        }

        if (equipFlags.HasFlag(EquipFlag.OffhandStain))
        {
            if (!respectManual || state[EquipSlot.OffHand, true] is not StateChanged.Source.Manual)
                _state.ChangeStain(state, EquipSlot.OffHand, design.Stain(EquipSlot.OffHand), StateChanged.Source.Fixed);
            totalEquipFlags |= EquipFlag.OffhandStain;
        }
    }

    private void Reduce(ActorState state, in DesignData design, CustomizeFlag customizeFlags, ref CustomizeFlag totalCustomizeFlags,
        bool respectManual)
    {
        customizeFlags &= ~totalCustomizeFlags;
        if (customizeFlags == 0)
            return;

        var           customize = state.ModelData.Customize;
        CustomizeFlag fixFlags  = 0;
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
            _state.ChangeCustomize(state, customize, fixFlags, StateChanged.Source.Fixed);

        if (customizeFlags.HasFlag(CustomizeFlag.Face))
        {
            if (!respectManual || state[CustomizeIndex.Face] is not StateChanged.Source.Manual)
                _state.ChangeCustomize(state, CustomizeIndex.Face, design.Customize.Face, StateChanged.Source.Fixed);
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
            if (CustomizationService.IsCustomizationValid(set, face, index, value))
            {
                if (!respectManual || state[index] is not StateChanged.Source.Manual)
                    _state.ChangeCustomize(state, index, value, StateChanged.Source.Fixed);
                totalCustomizeFlags |= flag;
            }
        }
    }

    private void Reduce(ActorState state, in DesignData design, bool applyHat, bool applyVisor, bool applyWeapon, bool applyWet,
        ref byte totalMetaFlags, bool respectManual)
    {
        if (applyHat && (totalMetaFlags & 0x01) == 0)
        {
            if (!respectManual || state[ActorState.MetaFlag.HatState] is not StateChanged.Source.Manual)
                _state.ChangeHatState(state, design.IsHatVisible(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x01;
        }

        if (applyVisor && (totalMetaFlags & 0x02) == 0)
        {
            if (!respectManual || state[ActorState.MetaFlag.VisorState] is not StateChanged.Source.Manual)
                _state.ChangeVisorState(state, design.IsVisorToggled(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x02;
        }

        if (applyWeapon && (totalMetaFlags & 0x04) == 0)
        {
            if (!respectManual || state[ActorState.MetaFlag.WeaponState] is not StateChanged.Source.Manual)
                _state.ChangeWeaponState(state, design.IsWeaponVisible(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x04;
        }

        if (applyWet && (totalMetaFlags & 0x08) == 0)
        {
            if (!respectManual || state[ActorState.MetaFlag.Wetness] is not StateChanged.Source.Manual)
                _state.ChangeWetness(state, design.IsWet(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x08;
        }
    }
}
