using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Material;

public sealed unsafe class MaterialManager : IRequiredService, IDisposable
{
    private readonly PrepareColorSet _event;
    private readonly StateManager    _stateManager;
    private readonly PenumbraService _penumbra;
    private readonly ActorManager    _actors;

    private int _lastSlot;

    public MaterialManager(PrepareColorSet prepareColorSet, StateManager stateManager, ActorManager actors, PenumbraService penumbra)
    {
        _stateManager = stateManager;
        _actors       = actors;
        _penumbra     = penumbra;
        _event        = prepareColorSet;

        _event.Subscribe(OnPrepareColorSet, PrepareColorSet.Priority.MaterialManager);
    }

    public void Dispose()
        => _event.Unsubscribe(OnPrepareColorSet);

    private void OnPrepareColorSet(CharacterBase* characterBase, MaterialResourceHandle* material, ref StainId stain, ref nint ret)
    {
        var actor     = _penumbra.GameObjectFromDrawObject(characterBase);
        var validType = FindType(characterBase, actor, out var type);
        var (slotId, materialId) = FindMaterial(characterBase, material);

        if (!validType
         || slotId == byte.MaxValue
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

        for (var i = 0; i < values.Length; ++i)
        {
            var idx = MaterialValueIndex.FromKey(values[i].key);
            var (oldGame, model, source) = values[i].Value;
            ref var row = ref baseColorSet[idx.RowIndex];
            if (!idx.DataIndex.TryGetValue(row, out var newGame))
                continue;

            if (newGame == oldGame)
            {
                idx.DataIndex.SetValue(ref row, model);
            }
            else
            {
                switch (source.Base())
                {
                    case StateSource.Manual: 
                        _stateManager.ChangeMaterialValue(state, idx, Vector3.Zero, Vector3.Zero, ApplySettings.Game);
                        --i;
                        break;
                    case StateSource.Fixed: 
                        idx.DataIndex.SetValue(ref row, model);
                        state.Materials.UpdateValue(idx, new MaterialValueState(newGame, model, source), out _);
                        break;
                }
            }
        }

        if (MaterialService.GenerateNewColorTable(baseColorSet, out var texture))
            ret = (nint)texture;
    }

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
}
