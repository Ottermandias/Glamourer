using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Material;

public sealed unsafe class MaterialManager : IRequiredService, IDisposable
{
    private readonly PrepareColorSet _event;
    private readonly StateManager    _stateManager;
    private readonly PenumbraService _penumbra;
    private readonly ActorManager    _actors;
    private readonly Configuration   _config;

    private int _lastSlot;

    private readonly ThreadLocal<List<MaterialValueIndex>> _deleteList = new(() => []);

    public MaterialManager(PrepareColorSet prepareColorSet, StateManager stateManager, ActorManager actors, PenumbraService penumbra,
        Configuration config)
    {
        _stateManager = stateManager;
        _actors       = actors;
        _penumbra     = penumbra;
        _config       = config;
        _event        = prepareColorSet;
        _event.Subscribe(OnPrepareColorSet, PrepareColorSet.Priority.MaterialManager);
    }

    public void Dispose()
        => _event.Unsubscribe(OnPrepareColorSet);

    private void OnPrepareColorSet(CharacterBase* characterBase, MaterialResourceHandle* material, ref StainId stain, ref nint ret)
    {
        if (!_config.UseAdvancedDyes)
            return;

        var actor     = _penumbra.GameObjectFromDrawObject(characterBase);
        var validType = FindType(characterBase, actor, out var type);
        var (slotId, materialId) = FindMaterial(characterBase, material);

        if (!validType
         || slotId > 9
         || type is not MaterialValueIndex.DrawObjectType.Human && slotId > 0
         || !actor.Identifier(_actors, out var identifier)
         || !_stateManager.TryGetValue(identifier, out var state))
            return;

        var min    = MaterialValueIndex.Min(type, slotId, materialId);
        var max    = MaterialValueIndex.Max(type, slotId, materialId);
        var values = state.Materials.GetValues(min, max);
        if (values.Length == 0)
            return;

        if (!PrepareColorSet.TryGetColorTable(characterBase, material, stain, out var baseColorSet))
            return;

        var drawData = type switch
        {
            MaterialValueIndex.DrawObjectType.Human => GetTempSlot((Human*)characterBase, slotId),
            _                                       => GetTempSlot((Weapon*)characterBase),
        };
        UpdateMaterialValues(state, values, drawData, ref baseColorSet);

        if (MaterialService.GenerateNewColorTable(baseColorSet, out var texture))
            ret = (nint)texture;
    }

    /// <summary> Update and apply the glamourer state of an actor according to the application sources when updated by the game. </summary>
    private void UpdateMaterialValues(ActorState state, ReadOnlySpan<(uint Key, MaterialValueState Value)> values, CharacterWeapon drawData,
        ref MtrlFile.ColorTable colorTable)
    {
        var deleteList = _deleteList.Value!;
        deleteList.Clear();
        for (var i = 0; i < values.Length; ++i)
        {
            var     idx           = MaterialValueIndex.FromKey(values[i].Key);
            var     materialValue = values[i].Value;
            ref var row           = ref colorTable[idx.RowIndex];
            var     newGame       = new ColorRow(row);
            if (materialValue.EqualGame(newGame, drawData))
                materialValue.Model.Apply(ref row);
            else
                switch (materialValue.Source)
                {
                    case StateSource.Pending:
                        materialValue.Model.Apply(ref row);
                        state.Materials.UpdateValue(idx, new MaterialValueState(newGame, materialValue.Model, drawData, StateSource.Manual),
                            out _);
                        break;
                    case StateSource.IpcManual:
                    case StateSource.Manual:
                        deleteList.Add(idx);
                        break;
                    case StateSource.Fixed:
                    case StateSource.IpcFixed:
                        materialValue.Model.Apply(ref row);
                        state.Materials.UpdateValue(idx, new MaterialValueState(newGame, materialValue.Model, drawData, materialValue.Source),
                            out _);
                        break;
                }
        }

        foreach (var idx in deleteList)
            _stateManager.ResetMaterialValue(state, idx, ApplySettings.Game);
    }

    /// <summary>
    /// Find the index of a material by searching through a draw objects pointers.
    /// Tries to take shortcuts for consecutive searches like when a character is newly created.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private (byte SlotId, byte MaterialId) FindMaterial(CharacterBase* characterBase, MaterialResourceHandle* material)
    {
        for (var i = _lastSlot; i < characterBase->SlotCount; ++i)
        {
            var idx = MaterialService.MaterialsPerModel * i;
            for (var j = 0; j < MaterialService.MaterialsPerModel; ++j)
            {
                var mat = (nint)characterBase->Materials[idx++];
                if (mat != (nint)material)
                    continue;

                _lastSlot = i;
                return ((byte)i, (byte)j);
            }
        }

        for (var i = 0; i < _lastSlot; ++i)
        {
            var idx = MaterialService.MaterialsPerModel * i;
            for (var j = 0; j < MaterialService.MaterialsPerModel; ++j)
            {
                var mat = (nint)characterBase->Materials[idx++];
                if (mat != (nint)material)
                    continue;

                _lastSlot = i;
                return ((byte)i, (byte)j);
            }
        }

        return (byte.MaxValue, byte.MaxValue);
    }

    /// <summary> Find the type of the given draw object by checking the actors pointers. </summary>
    private static bool FindType(CharacterBase* characterBase, Actor actor, out MaterialValueIndex.DrawObjectType type)
    {
        type = MaterialValueIndex.DrawObjectType.Human;
        if (!actor.Valid)
            return false;

        if (actor.Model.AsCharacterBase == characterBase)
            return true;

        if (!actor.AsObject->IsCharacter())
            return false;

        if (actor.AsCharacter->DrawData.WeaponDataSpan[0].DrawObject == characterBase)
        {
            type = MaterialValueIndex.DrawObjectType.Mainhand;
            return true;
        }

        if (actor.AsCharacter->DrawData.WeaponDataSpan[1].DrawObject == characterBase)
        {
            type = MaterialValueIndex.DrawObjectType.Offhand;
            return true;
        }

        return false;
    }

    /// <summary> We need to get the temporary set, variant and stain that is currently being set if it is available. </summary>
    private static CharacterWeapon GetTempSlot(Human* human, byte slotId)
    {
        if (human->ChangedEquipData == null)
            return ((Model)human).GetArmor(((uint)slotId).ToEquipSlot()).ToWeapon(0);

        return ((CharacterArmor*)human->ChangedEquipData + slotId * 3)->ToWeapon(0);
    }

    /// <summary>
    /// We need to get the temporary set, variant and stain that is currently being set if it is available.
    /// Weapons do not change in skeleton id without being reconstructed, so this is not changeable data.
    /// </summary>
    private static CharacterWeapon GetTempSlot(Weapon* weapon)
    {
        var changedData = *(void**)((byte*)weapon + 0x918);
        if (changedData == null)
            return new CharacterWeapon(weapon->ModelSetId, weapon->SecondaryId, (Variant)weapon->Variant, (StainId)weapon->ModelUnknown);

        return new CharacterWeapon(weapon->ModelSetId, *(SecondaryId*)changedData, ((Variant*)changedData)[2], ((StainId*)changedData)[3]);
    }
}
