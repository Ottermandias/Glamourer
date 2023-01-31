using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Interop;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;

namespace Glamourer.State;

public sealed partial class ActiveDesign : DesignBase
{
    private CharacterData _initialData = new();

    private CustomizeFlag _changedCustomize;
    private CustomizeFlag _fixedCustomize;

    private EquipFlag _changedEquip;
    private EquipFlag _fixedEquip;

    public bool IsHatVisible    { get; private set; } = false;
    public bool IsWeaponVisible { get; private set; } = false;
    public bool IsVisorToggled  { get; private set; } = false;
    public bool IsWet           { get; private set; } = false;

    private ActiveDesign()
    { }

    public ActiveDesign(Actor actor)
    {
        Update(actor);
    }

    //public void ApplyToActor(Actor actor)
    //{
    //    if (!actor)
    //        return;
    //
    //    void Redraw()
    //        => Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw);
    //
    //    if (_drawData.ModelId != actor.ModelId)
    //    {
    //        Redraw();
    //        return;
    //    }
    //
    //    var customize1 = _drawData.Customize;
    //    var customize2 = actor.Customize;
    //    if (RedrawManager.NeedsRedraw(customize1, customize2))
    //    {
    //        Redraw();
    //        return;
    //    }
    //
    //    Glamourer.RedrawManager.UpdateCustomize(actor, customize2);
    //    foreach (var slot in EquipSlotExtensions.EqdpSlots)
    //        Glamourer.RedrawManager.ChangeEquip(actor, slot, actor.Equip[slot]);
    //    Glamourer.RedrawManager.LoadWeapon(actor, actor.MainHand, actor.OffHand);
    //    if (actor.IsHuman && actor.DrawObject)
    //        RedrawManager.SetVisor(actor.DrawObject.Pointer, actor.VisorEnabled);
    //}
    //
    public void Update(Actor actor)
    {
        if (!actor)
            return;

        if (!_initialData.Customize.Equals(actor.Customize))
        {
            _initialData.Customize.Load(actor.Customize);
            Customize().Load(actor.Customize);
        }

        var initialEquip = _initialData.Equipment;
        var currentEquip = actor.Equip;
        var equipment    = Equipment();
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var current = currentEquip[slot];
            if (initialEquip[slot] != current)
            {
                initialEquip[slot] = current;
                equipment[slot]    = current;
            }
        }

        if (_initialData.MainHand != actor.MainHand)
        {
            _initialData.MainHand = actor.MainHand;
            UpdateMainhand(actor.MainHand);
        }

        if (_initialData.OffHand != actor.OffHand)
        {
            _initialData.OffHand = actor.OffHand;
            UpdateMainhand(actor.OffHand);
        }
    }
}
