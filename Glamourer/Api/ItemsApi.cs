using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.Services;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Api;

public class ItemsApi(ApiHelpers helpers, ItemManager itemManager, StateManager stateManager) : IGlamourerApiItems, IApiService
{
    public GlamourerApiEc SetItem(int objectIndex, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "Slot", slot, "ID", itemId, "Stains", stains, "Key", key, "Flags", flags);
        if (!ResolveItem(slot, itemId, out var item))
            return ApiHelpers.Return(GlamourerApiEc.ItemInvalid, args);

        if (helpers.FindState(objectIndex) is not { } state)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!state.ModelData.IsHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        var settings = new ApplySettings(Source: flags.HasFlag(ApplyFlag.Once) ? StateSource.IpcManual : StateSource.IpcFixed, Key: key);
        stateManager.ChangeEquip(state, (EquipSlot)slot, item, new StainIds(stains), settings);
        ApiHelpers.Lock(state, key, flags);
        return GlamourerApiEc.Success;
    }

    public GlamourerApiEc SetItemName(string playerName, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Name", playerName, "Slot", slot, "ID", itemId, "Stains", stains, "Key", key, "Flags", flags);
        if (!ResolveItem(slot, itemId, out var item))
            return ApiHelpers.Return(GlamourerApiEc.ItemInvalid, args);

        var settings    = new ApplySettings(Source: flags.HasFlag(ApplyFlag.Once) ? StateSource.IpcManual : StateSource.IpcFixed, Key: key);
        var anyHuman    = false;
        var anyFound    = false;
        var anyUnlocked = false;
        foreach (var state in helpers.FindStates(playerName))
        {
            anyFound = true;
            if (!state.ModelData.IsHuman)
                continue;

            anyHuman = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked = true;
            stateManager.ChangeEquip(state, (EquipSlot)slot, item, new StainIds(stains), settings);
            ApiHelpers.Lock(state, key, flags);
        }

        if (!anyFound)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!anyHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc SetBonusItem(int objectIndex, ApiBonusSlot slot, ulong bonusItemId, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "Slot", slot, "ID", bonusItemId, "Key", key, "Flags", flags);
        if (!ResolveBonusItem(slot, bonusItemId, out var item))
            return ApiHelpers.Return(GlamourerApiEc.ItemInvalid, args);

        if (helpers.FindState(objectIndex) is not { } state)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!state.ModelData.IsHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        var settings = new ApplySettings(Source: flags.HasFlag(ApplyFlag.Once) ? StateSource.IpcManual : StateSource.IpcFixed, Key: key);
        stateManager.ChangeBonusItem(state, item.Type.ToBonus(), item, settings);
        ApiHelpers.Lock(state, key, flags);
        return GlamourerApiEc.Success;
    }

    public GlamourerApiEc SetBonusItemName(string playerName, ApiBonusSlot slot, ulong bonusItemId, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Name", playerName, "Slot", slot, "ID", bonusItemId, "Key", key, "Flags", flags);
        if (!ResolveBonusItem(slot, bonusItemId, out var item))
            return ApiHelpers.Return(GlamourerApiEc.ItemInvalid, args);

        var settings = new ApplySettings(Source: flags.HasFlag(ApplyFlag.Once) ? StateSource.IpcManual : StateSource.IpcFixed, Key: key);
        var anyHuman = false;
        var anyFound = false;
        var anyUnlocked = false;
        foreach (var state in helpers.FindStates(playerName))
        {
            anyFound = true;
            if (!state.ModelData.IsHuman)
                continue;

            anyHuman = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked = true;
            stateManager.ChangeBonusItem(state, item.Type.ToBonus(), item, settings);
            ApiHelpers.Lock(state, key, flags);
        }

        if (!anyFound)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!anyHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc SetMetaState(int objectIndex, MetaFlag types, bool newValue, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "MetaTypes", types, "NewValue", newValue, "Key", key, "ApplyFlags", flags);
        if (types == 0)
            return ApiHelpers.Return(GlamourerApiEc.InvalidState, args);

        if (helpers.FindState(objectIndex) is not { } state)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!state.ModelData.IsHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        // Grab MetaIndices from attached flags, and update the states.
        var indices = types.ToIndices();
        foreach (var index in indices)
        {
            stateManager.ChangeMetaState(state, index, newValue, ApplySettings.Manual);
            ApiHelpers.Lock(state, key, flags);
        }

        return GlamourerApiEc.Success;
    }

    public GlamourerApiEc SetMetaStateName(string playerName, MetaFlag types, bool newValue, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Name", playerName, "MetaTypes", types, "NewValue", newValue, "Key", key, "ApplyFlags", flags);
        if (types == 0)
            return ApiHelpers.Return(GlamourerApiEc.ItemInvalid, args);

        var anyHuman = false;
        var anyFound = false;
        var anyUnlocked = false;
        foreach (var state in helpers.FindStates(playerName))
        {
            anyFound = true;
            if (!state.ModelData.IsHuman)
                continue;

            anyHuman = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked = true;
            // update all MetaStates for this ActorState
            foreach (var index in types.ToIndices())
            {
                stateManager.ChangeMetaState(state, index, newValue, ApplySettings.Manual);
                ApiHelpers.Lock(state, key, flags);
            }
        }

        if (!anyFound)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!anyHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    private bool ResolveItem(ApiEquipSlot apiSlot, ulong itemId, out EquipItem item)
    {
        var id   = (CustomItemId)itemId;
        var slot = (EquipSlot)apiSlot;
        if (id.Id == 0)
            id = ItemManager.NothingId(slot);

        item = itemManager.Resolve(slot, id);
        return item.Valid;
    }

    private bool ResolveBonusItem(ApiBonusSlot apiSlot, ulong itemId, out EquipItem item)
    {
        var slot = apiSlot switch
        {
            ApiBonusSlot.Glasses => BonusItemFlag.Glasses,
            _                    => BonusItemFlag.Unknown,
        };

        return itemManager.IsBonusItemValid(slot, (BonusItemId)itemId, out item);
    }
}
