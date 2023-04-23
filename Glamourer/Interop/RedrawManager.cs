using System;
using System.Diagnostics;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

public partial class Interop : IDisposable
{
    private readonly JobService _jobService;

    public Interop(JobService jobService)
    {
        _jobService = jobService;
        SignatureHelper.Initialise(this);
        _changeJobHook.Enable();
        _flagSlotForUpdateHook.Enable();
        _setupVisorHook.Enable();
    }

    public void Dispose()
    {
        _changeJobHook.Dispose();
        _flagSlotForUpdateHook.Dispose();
        _setupVisorHook.Dispose();
    }

    public static unsafe bool GetVisorState(nint humanPtr)
    {
        if (humanPtr == IntPtr.Zero)
            return false;

        var data  = (Human*)humanPtr;
        var flags = &data->CharacterBase.UnkFlags_01;
        return (*flags & Offsets.DrawObjectVisorStateFlag) != 0;
    }

    public static unsafe void SetVisorState(nint humanPtr, bool on)
    {
        if (humanPtr == IntPtr.Zero)
            return;

        var data  = (Human*)humanPtr;
        var flags = &data->CharacterBase.UnkFlags_01;
        var state = (*flags & Offsets.DrawObjectVisorStateFlag) != 0;
        if (state == on)
            return;

        var newFlag = (byte)(on ? *flags | Offsets.DrawObjectVisorStateFlag : *flags & ~Offsets.DrawObjectVisorStateFlag);
        *flags = (byte)(newFlag | Offsets.DrawObjectVisorToggleFlag);
    }
}

public partial class Interop
{
    private delegate void UpdateVisorDelegateInternal(nint humanPtr, ushort modelId, bool on);
    public delegate  void UpdateVisorDelegate(DrawObject human, SetId modelId, ref bool on);

    [Signature(Penumbra.GameData.Sigs.SetupVisor, DetourName = nameof(SetupVisorDetour))]
    private readonly Hook<UpdateVisorDelegateInternal> _setupVisorHook = null!;

    public event UpdateVisorDelegate? VisorUpdate;

    private void SetupVisorDetour(nint humanPtr, ushort modelId, bool on)
    {
        InvokeVisorEvent(humanPtr, modelId, ref on);
        _setupVisorHook.Original(humanPtr, modelId, on);
    }

    private void InvokeVisorEvent(DrawObject drawObject, SetId modelId, ref bool on)
    {
        if (VisorUpdate == null)
        {
            Glamourer.Log.Verbose($"Visor setup on 0x{drawObject.Address:X} with {modelId.Value}, setting to {on}.");
            return;
        }

        var initialValue = on;
        foreach (var del in VisorUpdate.GetInvocationList().OfType<UpdateVisorDelegate>())
        {
            try
            {
                del(drawObject, modelId, ref on);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not invoke {nameof(VisorUpdate)} Subscriber:\n{ex}");
            }
        }

        Glamourer.Log.Verbose(
            $"Visor setup on 0x{drawObject.Address:X} with {modelId.Value}, setting to {on}, initial call was {initialValue}.");
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
        {
            Glamourer.Log.Verbose(
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

        Glamourer.Log.Verbose(
            $"{slot} updated on 0x{drawObject.Address:X} to {armor.Set.Value}-{armor.Variant} with stain {armor.Stain.Value}, initial armor was {iv.Set.Value}-{iv.Variant} with stain {iv.Stain.Value}.");
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
        JobChanged?.Invoke(data - Offsets.Character.ClassJobContainer, _jobService.Jobs[(byte)job]);
    }

    public event Action<Actor, Job>? JobChanged;
}

public unsafe partial class RedrawManager : IDisposable
{
    private readonly ItemManager          _items;
    private readonly ActorService         _actors;
    private readonly FixedDesignManager   _fixedDesignManager;
    private readonly ActiveDesign.Manager _stateManager;

    public RedrawManager(FixedDesignManager fixedDesignManager, ActiveDesign.Manager stateManager, ItemManager items, ActorService actors)
    {
        SignatureHelper.Initialise(this);
        _fixedDesignManager = fixedDesignManager;
        _stateManager       = stateManager;
        _items              = items;
        _actors             = actors;
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
        var identifier = actor.GetIdentifier(_actors.AwaitedService);
        if (!(_stateManager.TryGetValue(identifier, out var save) || _fixedDesignManager.TryGetDesign(identifier, out var save2)))
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
                    _items.ResolveRestrictedGear(saveEquip[slot], slot, customize.Race, customize.Gender);
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
