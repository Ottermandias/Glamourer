using Dalamud.Hooking;
using Dalamud.Plugin.Services;
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
    private readonly MovedEquipment                   _movedItemsEvent;
    private readonly EquippedGearset                  _gearsetEvent;
    private readonly List<(EquipSlot, uint, StainIds)> _itemList = new(12);

    public InventoryService(MovedEquipment movedItemsEvent, IGameInteropProvider interop, EquippedGearset gearsetEvent)
    {
        _movedItemsEvent = movedItemsEvent;
        _gearsetEvent    = gearsetEvent;

        _moveItemHook = interop.HookFromAddress<MoveItemDelegate>((nint)InventoryManager.MemberFunctionPointers.MoveItemSlot, MoveItemDetour);
        _equipGearsetHook =
            interop.HookFromAddress<EquipGearsetDelegate>((nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearset, EquipGearSetDetour);

        _moveItemHook.Enable();
        _equipGearsetHook.Enable();
    }

    public void Dispose()
    {
        _moveItemHook.Dispose();
        _equipGearsetHook.Dispose();
    }

    private delegate int EquipGearsetDelegate(RaptureGearsetModule* module, int gearsetId, byte glamourPlateId);

    private readonly Hook<EquipGearsetDelegate> _equipGearsetHook;

    private int EquipGearSetDetour(RaptureGearsetModule* module, int gearsetId, byte glamourPlateId)
    {
        var prior = module->CurrentGearsetIndex;
        var ret   = _equipGearsetHook.Original(module, gearsetId, glamourPlateId);
        var set   = module->GetGearset(gearsetId);
        _gearsetEvent.Invoke(new ByteString(set->Name).ToString(), gearsetId, prior, glamourPlateId, set->ClassJob);
        Glamourer.Log.Excessive($"[InventoryService] Applied gear set {gearsetId} with glamour plate {glamourPlateId} (Returned {ret})");
        if (ret == 0)
        {
            var entry = module->GetGearset(gearsetId);
            if (entry == null)
                return ret;

            if (glamourPlateId == 0)
                glamourPlateId = entry->GlamourSetLink;

            _itemList.Clear();


            if (glamourPlateId != 0)
            {
                void Add(EquipSlot slot, uint glamourId, StainId glamourStain1, StainId glamourStain2, ref RaptureGearsetModule.GearsetItem item)
                {
                    if (item.ItemId == 0)
                        _itemList.Add((slot, 0, new(0, 0)));
                    else if (glamourId != 0)
                        _itemList.Add((slot, glamourId, new(glamourStain1, glamourStain2)));
                    else if (item.GlamourId != 0)
                        _itemList.Add((slot, item.GlamourId, new(item.Stain0Id, item.Stain1Id)));
                    else
                        _itemList.Add((slot, FixId(item.ItemId), new(item.Stain0Id, item.Stain1Id)));
                }

                var plate = MirageManager.Instance()->GlamourPlates[glamourPlateId - 1];
                Add(EquipSlot.MainHand, plate.ItemIds[0],  plate.Stain0Ids[0], plate.Stain1Ids[0],  ref entry->Items[0]);
                Add(EquipSlot.OffHand,  plate.ItemIds[1],  plate.Stain0Ids[1], plate.Stain1Ids[1],  ref entry->Items[1]);
                Add(EquipSlot.Head,     plate.ItemIds[2],  plate.Stain0Ids[2], plate.Stain1Ids[2],  ref entry->Items[2]);
                Add(EquipSlot.Body,     plate.ItemIds[3],  plate.Stain0Ids[3], plate.Stain1Ids[3],  ref entry->Items[3]);
                Add(EquipSlot.Hands,    plate.ItemIds[4],  plate.Stain0Ids[4], plate.Stain1Ids[4],  ref entry->Items[5]);
                Add(EquipSlot.Legs,     plate.ItemIds[5],  plate.Stain0Ids[5], plate.Stain1Ids[5],  ref entry->Items[6]);
                Add(EquipSlot.Feet,     plate.ItemIds[6],  plate.Stain0Ids[6], plate.Stain1Ids[6],  ref entry->Items[7]);
                Add(EquipSlot.Ears,     plate.ItemIds[7],  plate.Stain0Ids[7], plate.Stain1Ids[7],  ref entry->Items[8]);
                Add(EquipSlot.Neck,     plate.ItemIds[8],  plate.Stain0Ids[8], plate.Stain1Ids[8],  ref entry->Items[9]);
                Add(EquipSlot.Wrists,   plate.ItemIds[9],  plate.Stain0Ids[9], plate.Stain1Ids[9],  ref entry->Items[10]);
                Add(EquipSlot.RFinger,  plate.ItemIds[10], plate.Stain0Ids[10], plate.Stain1Ids[10], ref entry->Items[11]);
                Add(EquipSlot.LFinger,  plate.ItemIds[11], plate.Stain0Ids[11], plate.Stain1Ids[11], ref entry->Items[12]);
            }
            else
            {
                void Add(EquipSlot slot, ref RaptureGearsetModule.GearsetItem item)
                {
                    if (item.ItemId == 0)
                        _itemList.Add((slot, 0, new(0, 0)));
                    else if (item.GlamourId != 0)
                        _itemList.Add((slot, item.GlamourId, new(item.Stain0Id, item.Stain1Id)));
                    else
                        _itemList.Add((slot, FixId(item.ItemId), new(item.Stain0Id, item.Stain1Id)));
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

        tuple = (slot, item->GlamourId != 0 ? item->GlamourId : item->ItemId, new(item->Stains[0], item->Stains[1]));
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
