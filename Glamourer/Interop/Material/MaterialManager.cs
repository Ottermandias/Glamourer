using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Material;

public sealed unsafe class MaterialManager : IRequiredService, IDisposable
{
    private readonly PrepareColorSet _event;
    private readonly StateManager    _stateManager;
    private readonly PenumbraService _penumbra;
    private readonly ActorManager    _actors;

    private int _lastSlot;

    private readonly ThreadLocal<List<MaterialValueIndex>> _deleteList = new(() => []);

    public MaterialManager(PrepareColorSet prepareColorSet, StateManager stateManager, ActorManager actors, PenumbraService penumbra,
        Configuration config)
    {
        _stateManager = stateManager;
        _actors       = actors;
        _penumbra     = penumbra;
        _event        = prepareColorSet;
        _event.Subscribe(OnPrepareColorSet, PrepareColorSet.Priority.MaterialManager);
    }

    public void Dispose()
        => _event.Unsubscribe(OnPrepareColorSet);

    private void OnPrepareColorSet(in PrepareColorSet.Arguments arguments)
    {
        var actor     = _penumbra.GameObjectFromDrawObject(arguments.Model);
        var validType = FindType(arguments.Model.AsCharacterBase, actor, out var type);
        var (slotId, materialId) = FindMaterial(arguments.Model.AsCharacterBase, arguments.Handle);

        if (!validType
         || type is not MaterialValueIndex.DrawObjectType.Human && slotId > 0
         || !actor.Identifier(_actors, out var identifier)
         || !_stateManager.TryGetValue(identifier, out var state))
            return;

        var min    = MaterialValueIndex.Min(type, slotId, materialId);
        var max    = MaterialValueIndex.Max(type, slotId, materialId);
        var values = state.Materials.GetValues(min, max);
        if (values.Length == 0)
            return;

        if (!PrepareColorSet.TryGetColorTable(arguments.Handle, arguments.Ids, out var baseColorSet))
            return;

        var drawData = type switch
        {
            MaterialValueIndex.DrawObjectType.Human => GetTempSlot(arguments.Model.AsHuman, (HumanSlot)slotId),
            _                                       => GetTempSlot(arguments.Model.AsWeapon),
        };
        var mode = PrepareColorSet.GetMode(arguments.Handle);
        UpdateMaterialValues(state, values, drawData, ref baseColorSet, mode);

        if (MaterialService.GenerateNewColorTable(baseColorSet, out var texture))
            arguments.ReturnValue = (nint)texture;
    }

    /// <summary> Update and apply the glamourer state of an actor according to the application sources when updated by the game. </summary>
    private void UpdateMaterialValues(ActorState state, ReadOnlySpan<(uint Key, MaterialValueState Value)> values, CharacterWeapon drawData,
        ref ColorTable.Table colorTable, ColorRow.Mode mode)
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
                materialValue.Model.Apply(ref row, mode);
            else
                switch (materialValue.Source)
                {
                    case StateSource.Pending:
                        materialValue.Model.Apply(ref row, mode);
                        state.Materials.UpdateValue(idx, new MaterialValueState(newGame, materialValue.Model, drawData, StateSource.Manual),
                            out _);
                        break;
                    case StateSource.IpcPending:
                        materialValue.Model.Apply(ref row, mode);
                        state.Materials.UpdateValue(idx, new MaterialValueState(newGame, materialValue.Model, drawData, StateSource.IpcManual),
                            out _);
                        break;
                    case StateSource.IpcManual:
                    case StateSource.Manual:
                        deleteList.Add(idx);
                        break;
                    case StateSource.Fixed:
                    case StateSource.IpcFixed:
                        materialValue.Model.Apply(ref row, mode);
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
        if (!actor.Valid)
        {
            type = MaterialValueIndex.DrawObjectType.Invalid;
            return false;
        }

        if (actor.Model.AsCharacterBase == characterBase && ((Model)characterBase).IsHuman)
        {
            type = MaterialValueIndex.DrawObjectType.Human;
            return true;
        }

        if (!actor.AsObject->IsCharacter())
        {
            type = MaterialValueIndex.DrawObjectType.Invalid;
            return false;
        }

        if (actor.AsCharacter->DrawData.WeaponData[0].DrawObject == characterBase)
        {
            type = MaterialValueIndex.DrawObjectType.Mainhand;
            return true;
        }

        if (actor.AsCharacter->DrawData.WeaponData[1].DrawObject == characterBase)
        {
            type = MaterialValueIndex.DrawObjectType.Offhand;
            return true;
        }

        type = MaterialValueIndex.DrawObjectType.Invalid;
        return false;
    }

    /// <summary> We need to get the temporary set, variant and stain that is currently being set if it is available. </summary>
    private static CharacterWeapon GetTempSlot(Human* human, HumanSlot slotId)
    {
        if (human->ChangedEquipData is null)
            return slotId.ToSpecificEnum() switch
            {
                EquipSlot slot      => ((Model)human).GetArmor(slot).ToWeapon(0),
                BonusItemFlag bonus => ((Model)human).GetBonus(bonus).ToWeapon(0),
                _                   => default,
            };

        if (!slotId.ToSlotIndex(out var index))
            return default;

        var item = (ChangedEquipData*)human->ChangedEquipData + index;
        if (index < 10)
            return ((CharacterArmor*)item)->ToWeapon(0);

        return new CharacterWeapon(item->BonusModel, 0, item->BonusVariant, StainIds.None);
    }

    /// <summary>
    /// We need to get the temporary set, variant and stain that is currently being set if it is available.
    /// Weapons do not change in skeleton id without being reconstructed, so this is not changeable data.
    /// </summary>
    private static CharacterWeapon GetTempSlot(Weapon* weapon)
    {
        var changedData = weapon->ChangedData;
        if (changedData == null)
            return new CharacterWeapon(weapon->ModelSetId, weapon->SecondaryId, (Variant)weapon->Variant, StainIds.FromWeapon(*weapon));

        return new CharacterWeapon(weapon->ModelSetId, changedData->SecondaryId, changedData->Variant,
            new StainIds(changedData->Stain0, changedData->Stain1));
    }
}
