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
    private readonly UpdateColorSets _updateColorSets;

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

    public PrepareColorSet(HookManager hooks, UpdateColorSets updateColorSets, Logger log)
        : base("Prepare Color Set", log)
    {
        _updateColorSets = updateColorSets;
        _task            = hooks.CreateHook<Delegate>(Name, Sigs.PrepareColorSet, Detour, true);
    }

    private readonly Task<Hook<Delegate>> _task;

    public nint Address
        => (nint)CharacterBase.MemberFunctionPointers.Destroy;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _task.Result.Dispose();
    }

    public void Enable()
        => _task.Result.Enable();

    public void Disable()
        => _task.Result.Disable();

    public Task Awaiter
        => _task;

    public bool Finished
        => _task.IsCompletedSuccessfully;

    private delegate Texture* Delegate(MaterialResourceHandle* material, StainId stainId1, StainId stainId2);

    private Texture* Detour(MaterialResourceHandle* material, StainId stainId1, StainId stainId2)
    {
        Glamourer.Log.Excessive($"[{Name}] Triggered with 0x{(nint)material:X} {stainId1.Id} {stainId2.Id}.");
        var characterBase = _updateColorSets.Get();
        if (!characterBase.IsCharacterBase)
            return _task.Result.Original(material, stainId1, stainId2);

        var ret      = nint.Zero;
        var stainIds = new StainIds(stainId1, stainId2);
        Invoke(new Arguments(characterBase.AsCharacterBase, material, ref stainIds, ref ret));
        if (ret != nint.Zero)
            return (Texture*)ret;

        return _task.Result.Original(material, stainIds.Stain1, stainIds.Stain2);
    }

    public static bool TryGetColorTable(MaterialResourceHandle* material, StainIds stainIds,
        out ColorTable.Table table)
    {
        if (material->DataSet is null || material->DataSetSize < sizeof(ColorTable.Table) || !material->HasColorTable)
        {
            table = default;
            return false;
        }

        var newTable = *(ColorTable.Table*)material->DataSet;
        if (GetDyeTable(material, out var dyeTable))
        {
            if (stainIds.Stain1.Id is not 0)
                material->ReadStainingTemplate(dyeTable, stainIds.Stain1.Id, (Half*)&newTable, 0);

            if (stainIds.Stain2.Id is not 0)
                material->ReadStainingTemplate(dyeTable, stainIds.Stain2.Id, (Half*)&newTable, 1);
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

        var handle = (MaterialResourceHandle*)model.AsCharacterBase->Materials[idx];
        if (handle == null)
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
                    var mainhand = (Model)actor.AsCharacter->DrawData.WeaponData[0].DrawObject;
                    return mainhand.IsWeapon ? StainIds.FromWeapon(*mainhand.AsWeapon) : StainIds.None;
                case MaterialValueIndex.DrawObjectType.Offhand:
                    var offhand = (Model)actor.AsCharacter->DrawData.WeaponData[1].DrawObject;
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

    /// <summary> Get the correct dye table for a material. </summary>
    private static bool GetDyeTable(MaterialResourceHandle* material, out ushort* ptr)
    {
        ptr = null;
        if (material->AdditionalDataSize is 0 || material->AdditionalData is null)
            return false;

        var flags1 = material->AdditionalData[0];
        if ((flags1 & 0xF0) is 0)
        {
            ptr = (ushort*)material + 0x100;
            return true;
        }

        var flags2 = material->AdditionalData[1];
        var offset = 4 * (1 << (flags1 >> 4)) * (1 << (flags2 & 0x0F));
        ptr = (ushort*)material->DataSet + offset;
        return true;
    }
}
