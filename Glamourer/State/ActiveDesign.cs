using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;

namespace Glamourer.State;

public sealed partial class ActiveDesign : DesignData
{
    public readonly ActorIdentifier Identifier;

    private ModelData _initialData = new();

    public CustomizeFlag ChangedCustomize { get; private set; } = 0;
    public CustomizeFlag FixedCustomize   { get; private set; } = 0;

    public EquipFlag ChangedEquip { get; private set; } = 0;
    public EquipFlag FixedEquip   { get; private set; } = 0;


    public bool IsHatVisible    { get; private set; } = false;
    public bool IsWeaponVisible { get; private set; } = false;
    public bool IsVisorToggled  { get; private set; } = false;
    public bool IsWet           { get; private set; } = false;

    private ActiveDesign(ItemManager items, ActorIdentifier identifier)
    : base(items)
        => Identifier = identifier;

    public ActiveDesign(ItemManager items, ActorIdentifier identifier, Actor actor)
    : base(items)
    {
        Identifier = identifier;
        Initialize(items, actor);
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
    public void Initialize(ItemManager items, Actor actor)
    {
        if (!actor)
            return;

        if (!_initialData.Customize.Equals(actor.Customize))
        {
            _initialData.Customize.Load(actor.Customize);
            Customize.Load(actor.Customize);
        }

        var initialEquip = _initialData.Equipment;
        var currentEquip = actor.Equip;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var current = currentEquip[slot];
            if (initialEquip[slot] != current)
            {
                initialEquip[slot] = current;
                UpdateArmor(items, slot, current, true);
                SetStain(slot, current.Stain);
            }
        }

        if (_initialData.MainHand != actor.MainHand)
        {
            _initialData.MainHand = actor.MainHand;
            UpdateMainhand(items, actor.MainHand);
            SetStain(EquipSlot.MainHand, actor.MainHand.Stain);
        }

        if (_initialData.OffHand != actor.OffHand)
        {
            _initialData.OffHand = actor.OffHand;
            UpdateOffhand(items, actor.OffHand);
            SetStain(EquipSlot.OffHand, actor.OffHand.Stain);
        }

        var visor = VisorService.GetVisorState(actor.DrawObject);
        if (IsVisorToggled != visor)
            IsVisorToggled = visor;
    }

    public string CreateOldBase64()
        => DesignBase64Migration.CreateOldBase64(in ModelData, EquipFlagExtensions.All, CustomizeFlagExtensions.All, IsWet, IsHatVisible, true,
            IsVisorToggled,
            true, IsWeaponVisible, true, false, 1f);
}
