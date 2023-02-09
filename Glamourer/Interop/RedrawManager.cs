using System;
using System.Diagnostics;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.State;
using Glamourer.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

public partial class Interop : IDisposable
{
    public Interop()
    {
        SignatureHelper.Initialise(this);
        _changeJobHook.Enable();
    }

    public void Dispose()
    {
        _changeJobHook.Dispose();
    }
}

public unsafe partial class Interop
{
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
            return;

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
    }
}

public unsafe partial class Interop
{
    public delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature(Sigs.ChangeCustomize)]
    private readonly ChangeCustomizeDelegate _changeCustomize = null!;

    public bool UpdateCustomize(Actor actor, CustomizeData customize)
    {
        Debug.Assert(customize.Data != null, "Customize was invalid.");

        if (!actor.Valid || !actor.DrawObject.Valid)
            return false;

        return _changeCustomize(actor.DrawObject.Pointer, customize.Data, 1);
    }
}

public partial class Interop
{
    private delegate void ChangeJobDelegate(IntPtr data, uint job);

    [Signature(Sigs.ChangeJob, DetourName = nameof(ChangeJobDetour))]
    private readonly Hook<ChangeJobDelegate> _changeJobHook = null!;

    private void ChangeJobDetour(IntPtr data, uint job)
    {
        _changeJobHook.Original(data, job);
        JobChanged?.Invoke(data - Offsets.Character.ClassJobContainer, GameData.Jobs(Dalamud.GameData)[(byte)job]);
    }

    public event Action<Actor, Job>? JobChanged;
}

public unsafe partial class RedrawManager : IDisposable
{
    private readonly FixedDesigns         _fixedDesigns;
    private readonly ActiveDesign.Manager _stateManager;

    public RedrawManager(FixedDesigns fixedDesigns, ActiveDesign.Manager stateManager)
    {
        SignatureHelper.Initialise(this);
        _fixedDesigns                                  =  fixedDesigns;
        _stateManager                                  =  stateManager;
        _flagSlotForUpdateHook.Enable();
        _loadWeaponHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _loadWeaponHook.Dispose();
    }

    private void OnCharacterRedraw(Actor actor, uint* modelId, Customize customize, CharacterEquip equip)
    {
        // Do not apply anything if the game object model id does not correspond to the draw object model id.
        // This is the case if the actor is transformed to a different creature.
        if (actor.ModelId != *modelId)
            return;

        // Check if we have a current design in use, or if not if the actor has a fixed design.
        var identifier = actor.GetIdentifier();
        if (!(_stateManager.TryGetValue(identifier, out var save) || _fixedDesigns.TryGetDesign(identifier, out var save2)))
            return;

        // Compare game object customize data against draw object customize data for transformations.
        // Apply customization if they correspond and there is customization to apply.
        var gameObjectCustomize = new Customize((CustomizeData*)actor.Pointer->CustomizeData);
        if (gameObjectCustomize.Equals(customize))
            customize.Load(save!.Customize());

        // Compare game object equip data against draw object equip data for transformations.
        // Apply each piece of equip that should be applied if they correspond.
        var gameObjectEquip = new CharacterEquip((CharacterArmor*)actor.Pointer->EquipSlotData);
        if (gameObjectEquip.Equals(equip))
        {
            var saveEquip = save!.Equipment();
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                (_, equip[slot]) =
                    Glamourer.Items.RestrictedGear.ResolveRestricted(saveEquip[slot], slot, customize.Race, customize.Gender);
            }
        }
    }

    private void OnCharacterRedraw(IntPtr gameObject, string collection, IntPtr modelId, IntPtr customize, IntPtr equipData)
    {
        try
        {
            OnCharacterRedraw(gameObject, (uint*)modelId, new Customize((CustomizeData*)customize),
                new CharacterEquip((CharacterArmor*)equipData));
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on new draw object creation:\n{e}");
        }
    }

    private static void OnCharacterRedrawFinished(IntPtr gameObject, string collection, IntPtr drawObject)
    {
        //SetVisor((Human*)drawObject, true);
    }
}
