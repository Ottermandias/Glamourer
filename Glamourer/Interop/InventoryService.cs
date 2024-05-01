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
    private readonly List<(EquipSlot, uint, StainId)> _itemList = new(12);

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
                void Add(EquipSlot slot, uint glamourId, StainId glamourStain, ref RaptureGearsetModule.GearsetItem item)
                {
                    if (item.ItemID == 0)
                        _itemList.Add((slot, 0, 0));
                    else if (glamourId != 0)
                        _itemList.Add((slot, glamourId, glamourStain));
                    else if (item.GlamourId != 0)
                        _itemList.Add((slot, item.GlamourId, item.Stain));
                    else
                        _itemList.Add((slot, FixId(item.ItemID), item.Stain));
                }

                var plate = MirageManager.Instance()->GlamourPlatesSpan[glamourPlateId - 1];
                Add(EquipSlot.MainHand, plate.ItemIds[0],  plate.StainIds[0],  ref entry->ItemsSpan[0]);
                Add(EquipSlot.OffHand,  plate.ItemIds[1],  plate.StainIds[1],  ref entry->ItemsSpan[1]);
                Add(EquipSlot.Head,     plate.ItemIds[2],  plate.StainIds[2],  ref entry->ItemsSpan[2]);
                Add(EquipSlot.Body,     plate.ItemIds[3],  plate.StainIds[3],  ref entry->ItemsSpan[3]);
                Add(EquipSlot.Hands,    plate.ItemIds[4],  plate.StainIds[4],  ref entry->ItemsSpan[5]);
                Add(EquipSlot.Legs,     plate.ItemIds[5],  plate.StainIds[5],  ref entry->ItemsSpan[6]);
                Add(EquipSlot.Feet,     plate.ItemIds[6],  plate.StainIds[6],  ref entry->ItemsSpan[7]);
                Add(EquipSlot.Ears,     plate.ItemIds[7],  plate.StainIds[7],  ref entry->ItemsSpan[8]);
                Add(EquipSlot.Neck,     plate.ItemIds[8],  plate.StainIds[8],  ref entry->ItemsSpan[9]);
                Add(EquipSlot.Wrists,   plate.ItemIds[9],  plate.StainIds[9],  ref entry->ItemsSpan[10]);
                Add(EquipSlot.RFinger,  plate.ItemIds[10], plate.StainIds[10], ref entry->ItemsSpan[11]);
                Add(EquipSlot.LFinger,  plate.ItemIds[11], plate.StainIds[11], ref entry->ItemsSpan[12]);
            }
            else
            {
                void Add(EquipSlot slot, ref RaptureGearsetModule.GearsetItem item)
                {
                    if (item.ItemID == 0)
                        _itemList.Add((slot, 0, 0));
                    else if (item.GlamourId != 0)
                        _itemList.Add((slot, item.GlamourId, item.Stain));
                    else
                        _itemList.Add((slot, FixId(item.ItemID), item.Stain));
                }

                Add(EquipSlot.MainHand, ref entry->ItemsSpan[0]);
                Add(EquipSlot.OffHand,  ref entry->ItemsSpan[1]);
                Add(EquipSlot.Head,     ref entry->ItemsSpan[2]);
                Add(EquipSlot.Body,     ref entry->ItemsSpan[3]);
                Add(EquipSlot.Hands,    ref entry->ItemsSpan[5]);
                Add(EquipSlot.Legs,     ref entry->ItemsSpan[6]);
                Add(EquipSlot.Feet,     ref entry->ItemsSpan[7]);
                Add(EquipSlot.Ears,     ref entry->ItemsSpan[8]);
                Add(EquipSlot.Neck,     ref entry->ItemsSpan[9]);
                Add(EquipSlot.Wrists,   ref entry->ItemsSpan[10]);
                Add(EquipSlot.RFinger,  ref entry->ItemsSpan[11]);
                Add(EquipSlot.LFinger,  ref entry->ItemsSpan[12]);
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

    private static bool InvokeSource(InventoryType sourceContainer, uint sourceSlot, out (EquipSlot, uint, StainId) tuple)
    {
        tuple = default;
        if (sourceContainer is not InventoryType.EquippedItems)
            return false;

        var slot = GetSlot(sourceSlot);
        if (slot is EquipSlot.Unknown)
            return false;

        tuple = (slot, 0u, 0);
        return true;
    }

    private static bool InvokeTarget(InventoryManager* manager, InventoryType targetContainer, uint targetSlot,
        out (EquipSlot, uint, StainId) tuple)
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

        tuple = (slot, item->GlamourID != 0 ? item->GlamourID : item->ItemID, item->Stain);
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
