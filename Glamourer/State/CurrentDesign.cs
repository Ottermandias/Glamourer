using System.Collections.Generic;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Interop;
using Penumbra.GameData.Enums;

namespace Glamourer.State;

public unsafe class CurrentDesign : ICharacterData
{
    public CharacterData Data
        => _drawData;

    private CharacterData _drawData;
    private CharacterData _initialData;

    public CurrentDesign(Actor actor)
    {
        _initialData = new CharacterData();
        if (!actor)
            return;

        _initialData.Load(actor);
        var drawObject = actor.DrawObject;
        if (drawObject.Valid)
            _drawData.Load(drawObject);
        else
            _drawData = _initialData.Clone();
    }

    public void SaveCustomization(Customize customize, IReadOnlyCollection<Actor> actors)
    {
        _drawData.Customize.Load(customize);
        foreach (var actor in actors.Where(a => a && a.DrawObject))
            Glamourer.RedrawManager.UpdateCustomize(actor.DrawObject, _drawData.Customize);
    }

    public void Update(Actor actor)
    {
        if (!actor)
            return;

        if (!_initialData.Customize.Equals(actor.Customize))
        {
            _initialData.Customize.Load(actor.Customize);
            _drawData.Customize.Load(actor.Customize);
        }

        var initialEquip = _initialData.Equipment;
        var currentEquip = actor.Equip;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var current = currentEquip[slot];
            if (initialEquip[slot] != current)
            {
                initialEquip[slot]        = current;
                _drawData.Equipment[slot] = current;
            }
        }

        if (_initialData.MainHand != actor.MainHand)
        {
            _initialData.MainHand = actor.MainHand;
            _drawData.MainHand    = actor.MainHand;
        }

        if (_initialData.OffHand != actor.OffHand)
        {
            _initialData.OffHand = actor.OffHand;
            _drawData.OffHand    = actor.OffHand;
        }
    }
}
