using Glamourer.Interop;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;

namespace Glamourer.State;

public unsafe class CurrentDesign : IDesign
{
    public ref CharacterData Data
        => ref _drawData;

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

    public void Reset()
        => _drawData = _initialData;

    public void ApplyToActor(Actor actor)
    {
        if (!actor)
            return;

        void Redraw()
            => Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw);

        if (_drawData.ModelId != actor.ModelId)
        {
            Redraw();
            return;
        }

        var customize1 = _drawData.Customize;
        var customize2 = actor.Customize;
        if (RedrawManager.NeedsRedraw(customize1, customize2))
        {
            Redraw();
            return;
        }

        Glamourer.RedrawManager.UpdateCustomize(actor, customize2);
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            Glamourer.RedrawManager.ChangeEquip(actor, slot, actor.Equip[slot]);
        Glamourer.RedrawManager.LoadWeapon(actor, actor.MainHand, actor.OffHand);
        if (actor.IsHuman && actor.DrawObject)
            RedrawManager.SetVisor(actor.DrawObject.Pointer, actor.VisorEnabled);
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
