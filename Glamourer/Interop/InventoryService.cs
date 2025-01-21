using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Glamourer.Events;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Interop;

public sealed unsafe class InventoryService : IDisposable, IRequiredService
{
    private readonly MovedEquipment                    _movedItemsEvent;
    private readonly EquippedGearset                   _gearsetEvent;
    private readonly List<(EquipSlot, uint, StainIds)> _itemList = new(12);

    public InventoryService(MovedEquipment movedItemsEvent, IGameInteropProvider interop, EquippedGearset gearsetEvent)
    {
        _movedItemsEvent = movedItemsEvent;
        _gearsetEvent = gearsetEvent;

        _moveItemHook = interop.HookFromAddress<MoveItemDelegate>((nint)InventoryManager.MemberFunctionPointers.MoveItemSlot, MoveItemDetour);
        _equipGearsetHook = interop.HookFromAddress<EquipGearsetInternalDelegate>((nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearsetInternal, EquipGearSetDetour);

        _moveItemHook.Enable();
        _equipGearsetHook.Enable();
    }

    public void Dispose()
    {
        _moveItemHook.Dispose();
        _equipGearsetHook.Dispose();
    }

    private delegate nint EquipGearsetInternalDelegate(RaptureGearsetModule* module, uint gearsetId, byte glamourPlateId);

    private readonly Hook<EquipGearsetInternalDelegate> _equipGearsetHook = null!;

    private nint EquipGearSetDetour(RaptureGearsetModule* module, uint gearsetId, byte glamourPlateId)
    {
        var prior = module->CurrentGearsetIndex;
        var ret = _equipGearsetHook.Original(module, gearsetId, glamourPlateId);
        var set = module->GetGearset((int)gearsetId);
        _gearsetEvent.Invoke(new ByteString(set->Name).ToString(), (int)gearsetId, prior, glamourPlateId, set->ClassJob);
        Glamourer.Log.Verbose($"[InventoryService] Applied gear set {gearsetId} with glamour plate {glamourPlateId} (Returned {ret})");
        if (ret == 0)
        {
            var entry = module->GetGearset((int)gearsetId);
            if (entry == null)
                return ret;

            if (glamourPlateId == 0)
                glamourPlateId = entry->GlamourSetLink;

            _itemList.Clear();


            if (glamourPlateId != 0)
            {
                void Add(EquipSlot slot, uint glamourId, StainIds glamourStain, ref RaptureGearsetModule.GearsetItem item)
                {
                    if (item.ItemId == 0)
                        _itemList.Add((slot, 0, StainIds.None));
                    else if (glamourId != 0)
                        _itemList.Add((slot, glamourId, glamourStain));
                    else if (item.GlamourId != 0)
                        _itemList.Add((slot, item.GlamourId, StainIds.FromGearsetItem(item)));
                    else
                        _itemList.Add((slot, FixId(item.ItemId), StainIds.FromGearsetItem(item)));
                }

                var plate = MirageManager.Instance()->GlamourPlates[glamourPlateId - 1];
                Add(EquipSlot.MainHand, plate.ItemIds[0],  StainIds.FromGlamourPlate(plate, 0),  ref entry->Items[0]);
                Add(EquipSlot.OffHand,  plate.ItemIds[1],  StainIds.FromGlamourPlate(plate, 1),  ref entry->Items[1]);
                Add(EquipSlot.Head,     plate.ItemIds[2],  StainIds.FromGlamourPlate(plate, 2),  ref entry->Items[2]);
                Add(EquipSlot.Body,     plate.ItemIds[3],  StainIds.FromGlamourPlate(plate, 3),  ref entry->Items[3]);
                Add(EquipSlot.Hands,    plate.ItemIds[4],  StainIds.FromGlamourPlate(plate, 4),  ref entry->Items[5]);
                Add(EquipSlot.Legs,     plate.ItemIds[5],  StainIds.FromGlamourPlate(plate, 5),  ref entry->Items[6]);
                Add(EquipSlot.Feet,     plate.ItemIds[6],  StainIds.FromGlamourPlate(plate, 6),  ref entry->Items[7]);
                Add(EquipSlot.Ears,     plate.ItemIds[7],  StainIds.FromGlamourPlate(plate, 7),  ref entry->Items[8]);
                Add(EquipSlot.Neck,     plate.ItemIds[8],  StainIds.FromGlamourPlate(plate, 8),  ref entry->Items[9]);
                Add(EquipSlot.Wrists,   plate.ItemIds[9],  StainIds.FromGlamourPlate(plate, 9),  ref entry->Items[10]);
                Add(EquipSlot.RFinger,  plate.ItemIds[10], StainIds.FromGlamourPlate(plate, 10), ref entry->Items[11]);
                Add(EquipSlot.LFinger,  plate.ItemIds[11], StainIds.FromGlamourPlate(plate, 11), ref entry->Items[12]);
            }
            else
            {
                void Add(EquipSlot slot, ref RaptureGearsetModule.GearsetItem item)
                {
                    if (item.ItemId == 0)
                        _itemList.Add((slot, 0, StainIds.None));
                    else if (item.GlamourId != 0)
                        _itemList.Add((slot, item.GlamourId, StainIds.FromGearsetItem(item)));
                    else
                        _itemList.Add((slot, FixId(item.ItemId), StainIds.FromGearsetItem(item)));
                }

                Add(EquipSlot.MainHand, ref entry->Items[0]);
                Add(EquipSlot.OffHand,  ref entry->Items[1]);
                Add(EquipSlot.Head,     ref entry->Items[2]);
                Add(EquipSlot.Body,     ref entry->Items[3]);
                Add(EquipSlot.Hands,    ref entry->Items[5]);
                Add(EquipSlot.Legs,     ref entry->Items[6]);
                Add(EquipSlot.Feet,     ref entry->Items[7]);
                Add(EquipSlot.Ears,     ref entry->Items[8]);
                Add(EquipSlot.Neck,     ref entry->Items[9]);
                Add(EquipSlot.Wrists,   ref entry->Items[10]);
                Add(EquipSlot.RFinger,  ref entry->Items[11]);
                Add(EquipSlot.LFinger,  ref entry->Items[12]);
            }

            _movedItemsEvent.Invoke(_itemList.ToArray());
        }

        return ret;
    }

    private static uint FixId(uint itemId)
        => itemId % 50000;

    private delegate int MoveItemDelegate(InventoryManager* manager, InventoryType sourceContainer, ushort sourceSlot,
        InventoryType targetContainer, ushort targetSlot, byte unk);

    private readonly Hook<MoveItemDelegate> _moveItemHook;

    private int MoveItemDetour(InventoryManager* manager, InventoryType sourceContainer, ushort sourceSlot,
        InventoryType targetContainer, ushort targetSlot, byte unk)
    {
        var ret = _moveItemHook.Original(manager, sourceContainer, sourceSlot, targetContainer, targetSlot, unk);
        Glamourer.Log.Excessive($"[InventoryService] Moved {sourceContainer} {sourceSlot} {targetContainer} {targetSlot} (Returned {ret})");
        if (ret == 0)
        {
            if (InvokeSource(sourceContainer, sourceSlot, out var source))
                if (InvokeTarget(manager, targetContainer, targetSlot, out var target))
                    _movedItemsEvent.Invoke(new[]
                    {
                        source,
                        target,
                    });
                else
                    _movedItemsEvent.Invoke(new[]
                    {
                        source,
                    });
            else if (InvokeTarget(manager, targetContainer, targetSlot, out var target))
                _movedItemsEvent.Invoke(new[]
                {
                    target,
                });
        }

        return ret;
    }

    private static bool InvokeSource(InventoryType sourceContainer, uint sourceSlot, out (EquipSlot, uint, StainIds) tuple)
    {
        tuple = default;
        if (sourceContainer is not InventoryType.EquippedItems)
            return false;

        var slot = GetSlot(sourceSlot);
        if (slot is EquipSlot.Unknown)
            return false;

        tuple = (slot, 0u, StainIds.None);
        return true;
    }

    private static bool InvokeTarget(InventoryManager* manager, InventoryType targetContainer, uint targetSlot,
        out (EquipSlot, uint, StainIds) tuple)
    {
        tuple = default;
        if (targetContainer is not InventoryType.EquippedItems)
            return false;

        var slot = GetSlot(targetSlot);
        if (slot is EquipSlot.Unknown)
            return false;

        // Invoked after calling Original, so the item is already moved.
        var inventory = manager->GetInventoryContainer(targetContainer);
        if (inventory == null || inventory->Loaded == 0 || inventory->Size <= targetSlot)
            return false;

        var item = inventory->GetInventorySlot((int)targetSlot);
        if (item == null)
            return false;

        tuple = (slot, item->GlamourId != 0 ? item->GlamourId : item->ItemId, new StainIds(item->Stains));
        return true;
    }

    private static EquipSlot GetSlot(uint slot)
        => slot switch
        {
            0  => EquipSlot.MainHand,
            1  => EquipSlot.OffHand,
            2  => EquipSlot.Head,
            3  => EquipSlot.Body,
            4  => EquipSlot.Hands,
            6  => EquipSlot.Legs,
            7  => EquipSlot.Feet,
            8  => EquipSlot.Ears,
            9  => EquipSlot.Neck,
            10 => EquipSlot.Wrists,
            11 => EquipSlot.RFinger,
            12 => EquipSlot.LFinger,
            _  => EquipSlot.Unknown,
        };
}
