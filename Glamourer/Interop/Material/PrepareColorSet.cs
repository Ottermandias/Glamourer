using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using OtterGui.Classes;
using OtterGui.Services;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
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
        => _task = hooks.CreateHook<Delegate>(Name, Sigs.PrepareColorSet, Detour, true);

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

    public static bool TryGetColorTable(CharacterBase* characterBase, MaterialResourceHandle* material, StainIds stainIds,
        out LegacyColorTable table)
    {
        if (material->ColorTable == null)
        {
            table = default;
            return false;
        }

        var newTable = *(LegacyColorTable*)material->ColorTable;
        // TODO
        //if (stainIds.Stain1.Id != 0 || stainIds.Stain2.Id != 0)
        //    characterBase->ReadStainingTemplate(material, stainId.Id, (Half*)(&newTable));
        table = newTable;
        return true;
    }

    /// <summary> Assumes the actor is valid. </summary>
    public static bool TryGetColorTable(Actor actor, MaterialValueIndex index, out LegacyColorTable table)
    {
        var idx = index.SlotIndex * MaterialService.MaterialsPerModel + index.MaterialIndex;
        if (!index.TryGetModel(actor, out var model))
            return false;

        var handle = (MaterialResourceHandle*)model.AsCharacterBase->Materials[idx];
        if (handle == null)
        {
            table = default;
            return false;
        }

        return TryGetColorTable(model.AsCharacterBase, handle, GetStains(), out table);

        StainIds GetStains()
        {
            switch (index.DrawObject)
            {
                case MaterialValueIndex.DrawObjectType.Human:
                    return index.SlotIndex < 10 ? actor.Model.GetArmor(((uint)index.SlotIndex).ToEquipSlot()).Stains : StainIds.None;
                case MaterialValueIndex.DrawObjectType.Mainhand:
                    var mainhand = (Model)actor.AsCharacter->DrawData.WeaponData[1].DrawObject;
                    return mainhand.IsWeapon ? StainIds.FromWeapon(*mainhand.AsWeapon) : StainIds.None;
                case MaterialValueIndex.DrawObjectType.Offhand:
                    var offhand = (Model)actor.AsCharacter->DrawData.WeaponData[1].DrawObject;
                    return offhand.IsWeapon ? StainIds.FromWeapon(*offhand.AsWeapon) : StainIds.None;
                default: return StainIds.None;
            }
        }
    }
}
