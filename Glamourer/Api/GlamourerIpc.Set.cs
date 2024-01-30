using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public enum GlamourerErrorCode
    {
        Success,
        ActorNotFound,
        ActorNotHuman,
        ItemInvalid,
    }

    public const string LabelSetItem                = "Glamourer.SetItem";
    public const string LabelSetItemOnce            = "Glamourer.SetItemOnce";
    public const string LabelSetItemByActorName     = "Glamourer.SetItemByActorName";
    public const string LabelSetItemOnceByActorName = "Glamourer.SetItemOnceByActorName";


    private readonly FuncProvider<Character?, byte, ulong, byte, uint, int> _setItemProvider;
    private readonly FuncProvider<Character?, byte, ulong, byte, uint, int> _setItemOnceProvider;
    private readonly FuncProvider<string, byte, ulong, byte, uint, int>     _setItemByActorNameProvider;
    private readonly FuncProvider<string, byte, ulong, byte, uint, int>     _setItemOnceByActorNameProvider;

    public static FuncSubscriber<Character?, byte, ulong, byte, uint, int> SetItemSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelSetItem);

    public static FuncSubscriber<Character?, byte, ulong, byte, uint, int> SetItemOnceSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelSetItemOnce);

    public static FuncSubscriber<string, byte, ulong, byte, uint, int> SetItemByActorNameSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelSetItemByActorName);

    public static FuncSubscriber<string, byte, ulong, byte, uint, int> SetItemOnceByActorNameSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelSetItemOnceByActorName);

    private GlamourerErrorCode SetItem(Character? character, EquipSlot slot, CustomItemId itemId, StainId stainId, uint key, bool once)
    {
        if (itemId.Id == 0)
            itemId = ItemManager.NothingId(slot);

        var item = _items.Resolve(slot, itemId);
        if (!item.Valid)
            return GlamourerErrorCode.ItemInvalid;

        var identifier = _actors.FromObject(character, false, false, false);
        if (!identifier.IsValid)
            return GlamourerErrorCode.ActorNotFound;

        if (!_stateManager.TryGetValue(identifier, out var state))
        {
            _objects.Update();
            var data = _objects[identifier];
            if (!data.Valid || !_stateManager.GetOrCreate(identifier, data.Objects[0], out state))
                return GlamourerErrorCode.ActorNotFound;
        }

        if (!state.ModelData.IsHuman)
            return GlamourerErrorCode.ActorNotHuman;

        _stateManager.ChangeEquip(state, slot, item, stainId,
            new ApplySettings(Source: once ? StateSource.IpcManual : StateSource.IpcFixed, Key: key));
        return GlamourerErrorCode.Success;
    }

    private GlamourerErrorCode SetItemByActorName(string name, EquipSlot slot, CustomItemId itemId, StainId stainId, uint key, bool once)
    {
        if (itemId.Id == 0)
            itemId = ItemManager.NothingId(slot);

        var item = _items.Resolve(slot, itemId);
        if (!item.Valid)
            return GlamourerErrorCode.ItemInvalid;

        var found = false;
        _objects.Update();
        foreach (var identifier in FindActorsRevert(name).Distinct())
        {
            if (!_stateManager.TryGetValue(identifier, out var state))
            {
                var data = _objects[identifier];
                if (!data.Valid || !_stateManager.GetOrCreate(identifier, data.Objects[0], out state))
                    continue;
            }

            if (!state.ModelData.IsHuman)
                return GlamourerErrorCode.ActorNotHuman;

            _stateManager.ChangeEquip(state, slot, item, stainId,
                new ApplySettings(Source: once ? StateSource.IpcManual : StateSource.IpcFixed, Key: key));
            found = true;
        }

        return found ? GlamourerErrorCode.Success : GlamourerErrorCode.ActorNotFound;
    }
}
