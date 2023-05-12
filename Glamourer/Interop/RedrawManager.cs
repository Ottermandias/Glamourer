using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using Glamourer.Api;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

public unsafe partial class RedrawManager : IDisposable
{
    private readonly ItemManager          _items;
    private readonly ActorService         _actors;
    private readonly FixedDesignManager   _fixedDesignManager;
    private readonly ActiveDesign.Manager _stateManager;
    private readonly PenumbraAttach       _penumbra;
    private readonly WeaponService        _weapons;

    public RedrawManager(FixedDesignManager fixedDesignManager, ActiveDesign.Manager stateManager, ItemManager items, ActorService actors,
        PenumbraAttach penumbra, WeaponService weapons)
    {
        SignatureHelper.Initialise(this);
        _fixedDesignManager = fixedDesignManager;
        _stateManager       = stateManager;
        _items              = items;
        _actors             = actors;
        _penumbra           = penumbra;
        _weapons            = weapons;

        _penumbra.CreatingCharacterBase += OnCharacterRedraw;
        _penumbra.CreatedCharacterBase += OnCharacterRedrawFinished;
    }

    public void Dispose()
    {
    }

    private void OnCharacterRedraw(Actor actor, uint* modelId, Customize customize, CharacterEquip equip)
    {
        // Do not apply anything if the game object model id does not correspond to the draw object model id.
        // This is the case if the actor is transformed to a different creature.
        if (actor.ModelId != *modelId)
            return;

        // Check if we have a current design in use, or if not if the actor has a fixed design.
        var identifier = actor.GetIdentifier(_actors.AwaitedService);
        if (!_stateManager.TryGetValue(identifier, out var save))
            return;

        // Compare game object customize data against draw object customize data for transformations.
        // Apply customization if they correspond and there is customization to apply.
        var gameObjectCustomize = new Customize((CustomizeData*)actor.Pointer->CustomizeData);
        if (gameObjectCustomize.Equals(customize))
            customize.Load(save!.Customize);

        // Compare game object equip data against draw object equip data for transformations.
        // Apply each piece of equip that should be applied if they correspond.
        var gameObjectEquip = new CharacterEquip((CharacterArmor*)actor.Pointer->EquipSlotData);
        if (gameObjectEquip.Equals(equip))
        {
            var saveEquip = save!.Equipment;
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
