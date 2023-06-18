using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateManager : IReadOnlyDictionary<ActorIdentifier, ActorState>
{
    private readonly ActorService         _actors;
    private readonly ItemManager          _items;
    private readonly CustomizationService _customizations;
    private readonly VisorService         _visor;

    private readonly Dictionary<ActorIdentifier, ActorState> _states = new();

    public StateManager(ActorService actors, ItemManager items, CustomizationService customizations, VisorService visor)
    {
        _actors         = actors;
        _items          = items;
        _customizations = customizations;
        _visor          = visor;
    }

    public bool GetOrCreate(Actor actor, [NotNullWhen(true)] out ActorState? state)
        => GetOrCreate(actor.GetIdentifier(_actors.AwaitedService), actor, out state);

    public bool GetOrCreate(ActorIdentifier identifier, Actor actor, [NotNullWhen(true)] out ActorState? state)
    {
        if (TryGetValue(identifier, out state))
            return true;

        try
        {
            var designData = FromActor(actor);
            _states.Add(identifier, new ActorState(identifier) { Data = designData });
            return true;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not create new actor data for {identifier}:\n{ex}");
            return false;
        }
    }

    public IEnumerator<KeyValuePair<ActorIdentifier, ActorState>> GetEnumerator()
        => _states.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _states.Count;

    public bool ContainsKey(ActorIdentifier key)
        => _states.ContainsKey(key);

    public bool TryGetValue(ActorIdentifier key, out ActorState value)
        => _states.TryGetValue(key, out value!);

    public ActorState this[ActorIdentifier key]
        => _states[key];

    public IEnumerable<ActorIdentifier> Keys
        => _states.Keys;

    public IEnumerable<ActorState> Values
        => _states.Values;

    public unsafe DesignData FromActor(Actor actor)
    {
        var ret = new DesignData();
        if (!actor.IsCharacter)
        {
            ret.SetDefaultEquipment(_items);
            return ret;
        }

        if (actor.AsCharacter->ModelCharaId != 0)
        {
            ret.LoadNonHuman((uint)actor.AsCharacter->ModelCharaId, *(Customize*)&actor.AsCharacter->DrawData.CustomizeData,
                (byte*)&actor.AsCharacter->DrawData.Head);
            return ret;
        }

        var             model = actor.Model;
        CharacterWeapon main;
        CharacterWeapon off;

        ret.SetHatVisible(!actor.AsCharacter->DrawData.IsHatHidden);
        if (model.IsHuman)
        {
            var head     = ret.IsHatVisible() ? model.GetArmor(EquipSlot.Head) : actor.GetArmor(EquipSlot.Head);
            var headItem = _items.Identify(EquipSlot.Head, head.Set, head.Variant);
            ret.SetItem(EquipSlot.Head, headItem);
            ret.SetStain(EquipSlot.Head, head.Stain);

            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
            {
                var armor = model.GetArmor(slot);
                var item  = _items.Identify(slot, armor.Set, armor.Variant);
                ret.SetItem(slot, item);
                ret.SetStain(slot, armor.Stain);
            }

            ret.Customize     = model.GetCustomize();
            (_, _, main, off) = model.GetWeapons(actor);
            ret.SetVisor(_visor.GetVisorState(model));
        }
        else
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var armor = actor.GetArmor(slot);
                var item  = _items.Identify(slot, armor.Set, armor.Variant);
                ret.SetItem(slot, item);
                ret.SetStain(slot, armor.Stain);
            }

            ret.Customize = actor.GetCustomize();
            main          = actor.GetMainhand();
            off           = actor.GetOffhand();
            ret.SetVisor(actor.AsCharacter->DrawData.IsVisorToggled);
        }

        var mainItem = _items.Identify(EquipSlot.MainHand, main.Set, main.Type, (byte)main.Variant);
        var offItem  = _items.Identify(EquipSlot.OffHand,  off.Set,  off.Type,  (byte)off.Variant, mainItem.Type);
        ret.SetItem(EquipSlot.MainHand, mainItem);
        ret.SetStain(EquipSlot.MainHand, main.Stain);
        ret.SetItem(EquipSlot.OffHand, offItem);
        ret.SetStain(EquipSlot.OffHand, off.Stain);

        ret.SetIsWet(actor.AsCharacter->IsGPoseWet);
        ret.SetWeaponVisible(!actor.AsCharacter->DrawData.IsWeaponHidden);
        return ret;
    }
}
