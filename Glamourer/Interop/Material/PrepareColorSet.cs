using Dalamud.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.State;
using OtterGui.Classes;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Files;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Material;

public sealed unsafe class PrepareColorSet
    : EventWrapperPtr12Ref34<CharacterBase, MaterialResourceHandle, StainId, nint, PrepareColorSet.Priority>, IHookService
{
    public enum Priority
    {
        /// <seealso cref="MaterialManager.OnPrepareColorSet"/>
        MaterialManager = 0,
    }

    public PrepareColorSet(HookManager hooks)
        : base("Prepare Color Set ")
        => _task = hooks.CreateHook<Delegate>(Name, "40 55 56 41 56 48 83 EC ?? 80 BA", Detour, true);

    private readonly Task<Hook<Delegate>> _task;

    public nint Address
        => (nint)CharacterBase.MemberFunctionPointers.Destroy;

    public void Enable()
        => _task.Result.Enable();

    public void Disable()
        => _task.Result.Disable();

    public Task Awaiter
        => _task;

    public bool Finished
        => _task.IsCompletedSuccessfully;

    private delegate Texture* Delegate(CharacterBase* characterBase, MaterialResourceHandle* material, StainId stainId);

    private Texture* Detour(CharacterBase* characterBase, MaterialResourceHandle* material, StainId stainId)
    {
        Glamourer.Log.Excessive($"[{Name}] Triggered with 0x{(nint)characterBase:X} 0x{(nint)material:X} {stainId.Id}.");
        var ret = nint.Zero;
        Invoke(characterBase, material, ref stainId, ref ret);
        if (ret != nint.Zero)
            return (Texture*)ret;

        return _task.Result.Original(characterBase, material, stainId);
    }

    public static bool TryGetColorTable(CharacterBase* characterBase, MaterialResourceHandle* material, StainId stainId, out MtrlFile.ColorTable table)
    {
        if (material->ColorTable == null)
        {
            table = default;
            return false;
        }

        var newTable = *(MtrlFile.ColorTable*)material->ColorTable;
        if(stainId.Id != 0)
            characterBase->ReadStainingTemplate(material, stainId.Id, (Half*)(&newTable));
        table = newTable;
        return true;
    }
}

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


        var min     = MaterialValueIndex.Min(type, slotId, materialId);
        var max     = MaterialValueIndex.Max(type, slotId, materialId);
        var values  = state.Materials.GetValues(min, max);
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
                switch (source)
                {
                    case StateSource.Manual: 
                    case StateSource.Pending: 
                        state.Materials.RemoveValue(idx);
                        --i;
                        break;
                    case StateSource.Ipc:
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
