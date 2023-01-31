using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using Glamourer.Customization;
using Glamourer.State;
using Glamourer.Structs;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

public class DesignBaseValidator
{
    private readonly CustomizationManager _manager;
    private readonly RestrictedGear       _restrictedGear;

    public DesignBaseValidator(CustomizationManager manager, RestrictedGear restrictedGear)
    {
        _manager = manager;
        _restrictedGear = restrictedGear;
    }
}


public unsafe partial class RedrawManager
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
    private readonly CurrentManipulations _currentManipulations;

    public RedrawManager(FixedDesigns fixedDesigns, CurrentManipulations currentManipulations)
    {
        SignatureHelper.Initialise(this);
        Glamourer.Penumbra.CreatingCharacterBase.Event += OnCharacterRedraw;
        Glamourer.Penumbra.CreatedCharacterBase.Event  += OnCharacterRedrawFinished;
        _fixedDesigns                            =  fixedDesigns;
        _currentManipulations                    =  currentManipulations;
        _flagSlotForUpdateHook.Enable();
        _loadWeaponHook.Enable();
        _changeJobHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _loadWeaponHook.Dispose();
        _changeJobHook.Dispose();
        Glamourer.Penumbra.CreatingCharacterBase.Event -= OnCharacterRedraw;
        Glamourer.Penumbra.CreatedCharacterBase.Event  -= OnCharacterRedrawFinished;
    }

    private void OnCharacterRedraw(Actor actor, uint* modelId, Customize customize, CharacterEquip equip)
    {
        // Do not apply anything if the game object model id does not correspond to the draw object model id.
        // This is the case if the actor is transformed to a different creature.
        if (actor.ModelId != *modelId)
            return;

        // Check if we have a current design in use, or if not if the actor has a fixed design.
        var identifier = actor.GetIdentifier();
        if (!(_currentManipulations.TryGetDesign(identifier, out var save) || _fixedDesigns.TryGetDesign(identifier, out var save2)))
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