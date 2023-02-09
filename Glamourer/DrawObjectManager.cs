using System;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Api;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.State;
using Glamourer.Util;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer;

public class DrawObjectManager : IDisposable
{
    private readonly ItemManager          _items;
    private readonly ActorManager         _actors;
    private readonly ActiveDesign.Manager _manager;
    private readonly Interop.Interop      _interop;
    private readonly PenumbraAttach       _penumbra;

    public DrawObjectManager(ItemManager items, ActorManager actors, ActiveDesign.Manager manager, Interop.Interop interop,
        PenumbraAttach penumbra)
    {
        _items    = items;
        _actors   = actors;
        _manager  = manager;
        _interop  = interop;
        _penumbra = penumbra;

        _interop.EquipUpdate            += FixEquipment;
        _penumbra.CreatingCharacterBase += ApplyActiveDesign;
    }

    private void FixEquipment(DrawObject drawObject, EquipSlot slot, ref CharacterArmor item)
    {
        var customize = drawObject.Customize;
        var (changed, newArmor) = _items.RestrictedGear.ResolveRestricted(item, slot, customize.Race, customize.Gender);
        if (!changed)
            return;

        Glamourer.Log.Verbose(
            $"Invalid armor {item.Set.Value}-{item.Variant} for {customize.Gender} {customize.Race} changed to {newArmor.Set.Value}-{newArmor.Variant}.");
        item = newArmor;
    }

    private unsafe void ApplyActiveDesign(nint gameObjectPtr, string collectionName, nint modelIdPtr, nint customizePtr, nint equipDataPtr)
    {
        var     gameObject = (Actor)gameObjectPtr;
        ref var modelId    = ref *(uint*)modelIdPtr;
        // Do not apply anything if the game object model id does not correspond to the draw object model id.
        // This is the case if the actor is transformed to a different creature.
        if (gameObject.ModelId != modelId)
            return;

        var identifier = _actors.FromObject((GameObject*)gameObjectPtr, out _, true, true);
        if (!identifier.IsValid || !_manager.TryGetValue(identifier, out var design))
            return;

        // Compare game object customize data against draw object customize data for transformations.
        // Apply customization if they correspond and there is customization to apply.
        var gameObjectCustomize = gameObject.Customize;
        var customize           = new Customize((CustomizeData*)customizePtr);
        if (gameObjectCustomize.Equals(customize))
            customize.Load(design.Customize());

        // Compare game object equip data against draw object equip data for transformations.
        // Apply each piece of equip that should be applied if they correspond.
        var gameObjectEquip = gameObject.Equip;
        var equip           = new CharacterEquip((CharacterArmor*)equipDataPtr);
        if (gameObjectEquip.Equals(equip))
        {
            var saveEquip = design.Equipment();
            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
            {
                (_, equip[slot]) =
                    Glamourer.Items.RestrictedGear.ResolveRestricted(saveEquip[slot], slot, customize.Race, customize.Gender);
            }
        }
    }

    public void Dispose()
    {
        _penumbra.CreatingCharacterBase -= ApplyActiveDesign;
        _interop.EquipUpdate            -= FixEquipment;
    }
}
