using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class UpdateSlotService : IDisposable
{
    public UpdateSlotService()
    {
        SignatureHelper.Initialise(this);
        _flagSlotForUpdateHook.Enable();
    }

    public void Dispose()
        => _flagSlotForUpdateHook.Dispose();

    private delegate ulong FlagSlotForUpdateDelegateIntern(nint drawObject, uint slot, CharacterArmor* data);
    public delegate  void  FlagSlotForUpdateDelegate(DrawObject drawObject, EquipSlot slot, ref CharacterArmor item);

    // This gets called when one of the ten equip items of an existing draw object gets changed.
    [Signature(Sigs.FlagSlotForUpdate, DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegateIntern> _flagSlotForUpdateHook = null!;

    public event FlagSlotForUpdateDelegate? EquipUpdate;

    public ulong FlagSlotForUpdateInterop(DrawObject drawObject, EquipSlot slot, CharacterArmor armor)
        => _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);

    public void UpdateSlot(DrawObject drawObject, EquipSlot slot, CharacterArmor data)
    {
        InvokeFlagSlotEvent(drawObject, slot, ref data);
        FlagSlotForUpdateInterop(drawObject, slot, data);
    }

    public void UpdateStain(DrawObject drawObject, EquipSlot slot, StainId stain)
    {
        var armor = drawObject.Equip[slot] with { Stain = stain };
        UpdateSlot(drawObject, slot, armor);
    }

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot = slotIdx.ToEquipSlot();
        InvokeFlagSlotEvent(drawObject, slot, ref *data);
        return _flagSlotForUpdateHook.Original(drawObject, slotIdx, data);
    }

    private void InvokeFlagSlotEvent(DrawObject drawObject, EquipSlot slot, ref CharacterArmor armor)
    {
        if (EquipUpdate == null)
        {
            Glamourer.Log.Excessive(
                $"{slot} updated on 0x{drawObject.Address:X} to {armor.Set.Value}-{armor.Variant} with stain {armor.Stain.Value}.");
            return;
        }

        var iv = armor;
        foreach (var del in EquipUpdate.GetInvocationList().OfType<FlagSlotForUpdateDelegate>())
        {
            try
            {
                del(drawObject, slot, ref armor);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not invoke {nameof(EquipUpdate)} Subscriber:\n{ex}");
            }
        }

        Glamourer.Log.Excessive(
            $"{slot} updated on 0x{drawObject.Address:X} to {armor.Set.Value}-{armor.Variant} with stain {armor.Stain.Value}, initial armor was {iv.Set.Value}-{iv.Variant} with stain {iv.Stain.Value}.");
    }
}
