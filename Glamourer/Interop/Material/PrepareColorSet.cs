using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Material;

public sealed unsafe class PrepareColorSet
    : EventBase<PrepareColorSet.Arguments, PrepareColorSet.Priority>, IHookService
{
    private readonly CreateNewModel _createNewModel;

    public enum Priority
    {
        /// <seealso cref="MaterialManager.OnPrepareColorSet"/>
        MaterialManager = 0,
    }

    public ref struct Arguments(Model model, MaterialResourceHandle* handle, ref StainIds ids, ref nint returnValue)
    {
        public readonly Model                   Model       = model;
        public readonly MaterialResourceHandle* Handle      = handle;
        public ref      StainIds                Ids         = ref ids;
        public ref      nint                    ReturnValue = ref returnValue;
    }

    public PrepareColorSet(HookManager hooks, CreateNewModel createNewModel, LunaLogger log)
        : base("Prepare Color Set", log)
    {
        _createNewModel = createNewModel;
        _task           = hooks.CreateHook<Delegate>(Name, Sigs.PrepareColorSet, Detour, true);
    }

    private readonly Task<Hook<Delegate>?> _task;

    public nint Address
        => (nint)CharacterBase.MemberFunctionPointers.Destroy;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _task.Result?.Dispose();
    }

    public void Enable()
        => _task.Result?.Enable();

    public void Disable()
        => _task.Result?.Disable();

    public Task Awaiter
        => _task;

    public bool Finished
        => _task.IsCompletedSuccessfully;

    private delegate Texture* Delegate(MaterialResourceHandle* material, StainId stainId1, StainId stainId2);

    private Texture* Detour(MaterialResourceHandle* material, StainId stainId1, StainId stainId2)
    {
        Glamourer.Log.Excessive($"[{Name}] Triggered with 0x{(nint)material:X} {stainId1.Id} {stainId2.Id}.");
        var characterBase = _createNewModel.Get();
        if (!characterBase.IsCharacterBase)
            return _task.Result!.Original(material, stainId1, stainId2);

        var ret      = nint.Zero;
        var stainIds = new StainIds(stainId1, stainId2);
        Invoke(new Arguments(characterBase.AsCharacterBase, material, ref stainIds, ref ret));
        if (ret != nint.Zero)
            return (Texture*)ret;

        return _task.Result!.Original(material, stainIds.Stain1, stainIds.Stain2);
    }

    public static bool TryGetColorTable(MaterialResourceHandle* material, StainIds stainIds,
        out ColorTable.Table table)
    {
        if (material->DataSet is null || material->DataSetSize < sizeof(ColorTable.Table) || !material->HasColorTable)
        {
            table = default;
            return false;
        }

        var newTable   = *(ColorTable.Table*)material->DataSet;
        var stainTable = (ushort*)material->StainTable;
        if (stainTable is not null)
        {
            if (stainIds.Stain1.Id is not 0)
                material->ReadStainingTemplate(stainTable, stainIds.Stain1.Id, (Half*)&newTable, 0);

            if (stainIds.Stain2.Id is not 0)
                material->ReadStainingTemplate(stainTable, stainIds.Stain2.Id, (Half*)&newTable, 1);
        }

        table = newTable;
        return true;
    }

    /// <summary> Assumes the actor is valid. </summary>
    public static bool TryGetColorTable(Actor actor, MaterialValueIndex index, out ColorTable.Table table, out ColorRow.Mode mode)
    {
        var idx = index.SlotIndex * MaterialService.MaterialsPerModel + index.MaterialIndex;
        if (!index.TryGetModel(actor, out var model))
        {
            mode  = ColorRow.Mode.Dawntrail;
            table = default;
            return false;
        }

        var handle = model.AsCharacterBase->Materials[idx];
        if (handle is null)
        {
            mode  = ColorRow.Mode.Dawntrail;
            table = default;
            return false;
        }

        mode = GetMode(handle);
        return TryGetColorTable(handle, GetStains(), out table);

        StainIds GetStains()
        {
            switch (index.DrawObject)
            {
                case MaterialValueIndex.DrawObjectType.Human:
                    return index.SlotIndex < 10 ? actor.Model.GetArmor(((uint)index.SlotIndex).ToEquipSlot()).Stains : StainIds.None;
                case MaterialValueIndex.DrawObjectType.Mainhand:
                    var mainhand = (Model)actor.AsCharacter->DrawData.WeaponData[0].DrawData.DrawObject;
                    return mainhand.IsWeapon ? StainIds.FromWeapon(*mainhand.AsWeapon) : StainIds.None;
                case MaterialValueIndex.DrawObjectType.Offhand:
                    var offhand = (Model)actor.AsCharacter->DrawData.WeaponData[1].DrawData.DrawObject;
                    return offhand.IsWeapon ? StainIds.FromWeapon(*offhand.AsWeapon) : StainIds.None;
                default: return StainIds.None;
            }
        }
    }

    /// <summary> Get the shader mode of the material. </summary>
    public static ColorRow.Mode GetMode(MaterialResourceHandle* handle)
        => handle == null
            ? ColorRow.Mode.Dawntrail
            : handle->ShpkName.AsSpan().SequenceEqual("characterlegacy.shpk"u8)
                ? ColorRow.Mode.Legacy
                : ColorRow.Mode.Dawntrail;
}
