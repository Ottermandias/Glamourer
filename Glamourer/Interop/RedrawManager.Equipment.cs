using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Glamourer.Designs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe partial class RedrawManager
{
    private delegate ulong  FlagSlotForUpdateDelegate(nint drawObject, uint slot, CharacterArmor* data);

    // This gets called when one of the ten equip items of an existing draw object gets changed.
    [Signature(Sigs.FlagSlotForUpdate, DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegate> _flagSlotForUpdateHook = null!;

    public void UpdateSlot(DrawObject drawObject, EquipSlot slot, CharacterArmor data)
        => FlagSlotForUpdateDetourBase(drawObject.Address, slot.ToIndex(), &data, true);

    public void UpdateStain(DrawObject drawObject, EquipSlot slot, StainId stain)
    {
        var armor = drawObject.Equip[slot] with { Stain = stain};
        UpdateSlot(drawObject, slot, armor);
    }

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
        => FlagSlotForUpdateDetourBase(drawObject, slotIdx, data, false);

    private ulong FlagSlotForUpdateDetourBase(nint drawObject, uint slotIdx, CharacterArmor* data, bool manual)
    {
        try
        {
            var slot = slotIdx.ToEquipSlot();
            Glamourer.Log.Verbose(
                $"Flagged slot {slot} of 0x{(ulong)drawObject:X} for update with {data->Set.Value}-{data->Variant} (Stain {data->Stain.Value}).");
            HandleEquipUpdate(drawObject, slot, ref *data, manual);
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error invoking SlotUpdate:\n{ex}");
        }

        return _flagSlotForUpdateHook.Original(drawObject, slotIdx, data);

        //try
        //{
        //    var actor      = Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
        //    var identifier = actor.GetIdentifier();
        //
        //    if (_fixedDesigns.TryGetDesign(identifier, out var design))
        //    {
        //        PluginLog.Information($"Loaded {slot} from fixed design for {identifier}.");
        //        (var replaced, *data) =
        //            Glamourer.Items.RestrictedGear.ResolveRestricted(design.Armor(slot).Model, slot, (Race)drawObject->Race, (Gender)drawObject->Sex);
        //    }
        //    else if (_currentManipulations.TryGetDesign(identifier, out var save2))
        //    {
        //        PluginLog.Information($"Updated {slot} from current designs for {identifier}.");
        //        (var replaced, *data) =
        //            Glamourer.Items.RestrictedGear.ResolveRestricted(*data, slot, (Race)drawObject->Race, (Gender)(drawObject->Sex + 1));
        //        save2.Data.Equipment[slot] = *data;
        //    }
        //}
        //catch (Exception e)
        //{
        //    PluginLog.Error($"Error on loading new gear:\n{e}");
        //}
        //
        //return _flagSlotForUpdateHook.Original(drawObject, slotIdx, data);
    }

    private void HandleEquipUpdate(nint drawObject, EquipSlot slot, ref CharacterArmor data, bool manual)
    {
        var actor      = Glamourer.Penumbra.GameObjectFromDrawObject(drawObject);
        var identifier = actor.GetIdentifier();

        if (!_currentManipulations.TryGetDesign(identifier, out var design))
            return;

        var flag      = slot.ToFlag();
        var stainFlag = slot.ToStainFlag();
    }
}
