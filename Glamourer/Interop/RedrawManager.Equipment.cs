using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe partial class RedrawManager
{
    public delegate ulong FlagSlotForUpdateDelegate(Human* drawObject, uint slot, CharacterArmor* data);

    // This gets called when one of the ten equip items of an existing draw object gets changed.
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A", DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegate> _flagSlotForUpdateHook = null!;

    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot = slotIdx.ToEquipSlot();
        try
        {
            var actor      = Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
            var identifier = actor.GetIdentifier();

            if (_fixedDesigns.TryGetDesign(identifier, out var save))
            {
                PluginLog.Information($"Loaded {slot} from fixed design for {identifier}.");
                (var replaced, *data) =
                    Glamourer.RestrictedGear.ResolveRestricted(save.Equipment[slot], slot, (Race)drawObject->Race, (Gender)drawObject->Sex);
            }
            else if (_currentManipulations.TryGetDesign(identifier, out var save2))
            {
                PluginLog.Information($"Updated {slot} from current designs for {identifier}.");
                (var replaced, *data) =
                    Glamourer.RestrictedGear.ResolveRestricted(*data, slot, (Race)drawObject->Race, (Gender)(drawObject->Sex + 1));
                save2.Data.Equipment[slot] = *data;
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on loading new gear:\n{e}");
        }

        return _flagSlotForUpdateHook.Original(drawObject, slotIdx, data);
    }

    public bool ChangeEquip(DrawObject drawObject, uint slotIdx, CharacterArmor data)
    {
        if (!drawObject)
            return false;

        if (slotIdx > 9)
            return false;

        return FlagSlotForUpdateDetour(drawObject.Pointer, slotIdx, &data) != 0;
    }

    public bool ChangeEquip(Actor actor, EquipSlot slot, CharacterArmor data)
        => actor && ChangeEquip(actor.DrawObject, slot.ToIndex(), data);

    public bool ChangeEquip(DrawObject drawObject, EquipSlot slot, CharacterArmor data)
        => ChangeEquip(drawObject, slot.ToIndex(), data);

    public bool ChangeEquip(Actor actor, uint slotIdx, CharacterArmor data)
        => actor && ChangeEquip(actor.DrawObject, slotIdx, data);
}
