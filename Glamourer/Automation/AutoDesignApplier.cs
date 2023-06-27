using System;
using System.Diagnostics.CodeAnalysis;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
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

        if (!_manager.EnabledSets.TryGetValue(id, out var set))
            return;

        if (!_state.GetOrCreate(id, actor, out var state))
            return;

        Reduce(actor, state, set);
        _state.ReapplyState(actor);
    }

    public void Reduce(Actor actor, ActorIdentifier identifier, ActorState state)
    {
        if (!_config.EnableAutoDesigns)
            return;

        if (!GetPlayerSet(identifier, out var set))
            return;
        Reduce(actor, state, set);
    }

    private unsafe void Reduce(Actor actor, ActorState state, AutoDesignSet set)
    {
        EquipFlag totalEquipFlags = 0;
        //var       totalCustomizeFlags = _phrasing.Phrasing2 ? 0 : CustomizeFlagExtensions.RedrawRequired;
        var  totalCustomizeFlags = CustomizeFlagExtensions.RedrawRequired;
        byte totalMetaFlags      = 0;
        foreach (var design in set.Designs)
        {
            if (!design.IsActive(actor))
                continue;

            if (design.ApplicationType is 0)
                continue;

            if (actor.AsCharacter->CharacterData.ModelCharaId != design.Design.DesignData.ModelId)
                continue;

            var (equipFlags, customizeFlags, applyHat, applyVisor, applyWeapon, applyWet) = design.ApplyWhat();
            Reduce(state, in design.Design.DesignData, equipFlags,     ref totalEquipFlags);
            Reduce(state, in design.Design.DesignData, customizeFlags, ref totalCustomizeFlags);
            Reduce(state, in design.Design.DesignData, applyHat,       applyVisor, applyWeapon, applyWet, ref totalMetaFlags);
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

    private void Reduce(ActorState state, in DesignData design, EquipFlag equipFlags, ref EquipFlag totalEquipFlags)
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
                _state.ChangeItem(state, slot, design.Item(slot), StateChanged.Source.Fixed);
                totalEquipFlags |= flag;
            }

            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                _state.ChangeStain(state, slot, design.Stain(slot), StateChanged.Source.Fixed);
                totalEquipFlags |= stainFlag;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.Mainhand))
        {
            var item = design.Item(EquipSlot.MainHand);
            if (state.ModelData.Item(EquipSlot.MainHand).Type == item.Type)
            {
                _state.ChangeItem(state, EquipSlot.MainHand, item, StateChanged.Source.Fixed);
                totalEquipFlags |= EquipFlag.Mainhand;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.Offhand))
        {
            var item = design.Item(EquipSlot.OffHand);
            if (state.ModelData.Item(EquipSlot.OffHand).Type == item.Type)
            {
                _state.ChangeItem(state, EquipSlot.OffHand, item, StateChanged.Source.Fixed);
                totalEquipFlags |= EquipFlag.Offhand;
            }
        }

        if (equipFlags.HasFlag(EquipFlag.MainhandStain))
        {
            _state.ChangeStain(state, EquipSlot.MainHand, design.Stain(EquipSlot.MainHand), StateChanged.Source.Fixed);
            totalEquipFlags |= EquipFlag.MainhandStain;
        }

        if (equipFlags.HasFlag(EquipFlag.OffhandStain))
        {
            _state.ChangeStain(state, EquipSlot.OffHand, design.Stain(EquipSlot.OffHand), StateChanged.Source.Fixed);
            totalEquipFlags |= EquipFlag.OffhandStain;
        }
    }

    private void Reduce(ActorState state, in DesignData design, CustomizeFlag customizeFlags, ref CustomizeFlag totalCustomizeFlags)
    {
        customizeFlags &= ~totalCustomizeFlags;
        if (customizeFlags == 0)
            return;

        // TODO add race/gender handling
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
                _state.ChangeCustomize(state, index, value, StateChanged.Source.Fixed);
                totalCustomizeFlags |= flag;
            }
        }
    }

    private void Reduce(ActorState state, in DesignData design, bool applyHat, bool applyVisor, bool applyWeapon, bool applyWet,
        ref byte totalMetaFlags)
    {
        if (applyHat && (totalMetaFlags & 0x01) == 0)
        {
            _state.ChangeHatState(state, design.IsHatVisible(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x01;
        }

        if (applyVisor && (totalMetaFlags & 0x02) == 0)
        {
            _state.ChangeVisorState(state, design.IsVisorToggled(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x02;
        }

        if (applyWeapon && (totalMetaFlags & 0x04) == 0)
        {
            _state.ChangeWeaponState(state, design.IsWeaponVisible(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x04;
        }

        if (applyWet && (totalMetaFlags & 0x08) == 0)
        {
            _state.ChangeWetness(state, design.IsWet(), StateChanged.Source.Fixed);
            totalMetaFlags |= 0x08;
        }
    }
}
