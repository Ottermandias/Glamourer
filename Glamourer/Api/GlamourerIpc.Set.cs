using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Events;
using Glamourer.Services;
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

    public const string LabelSetItem            = "Glamourer.SetItem";
    public const string LabelSetItemByActorName = "Glamourer.SetItemByActorName";


    private readonly FuncProvider<Character?, byte, ulong, uint, int> _setItemProvider;
    private readonly FuncProvider<string, byte, ulong, uint, int>     _setItemByActorNameProvider;

    public static FuncSubscriber<Character?, byte, ulong, uint, int> SetItemSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelSetItem);

    public static FuncSubscriber<string, byte, ulong, uint, int> SetItemByActorNameSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelSetItemByActorName);

    private GlamourerErrorCode SetItem(Character? character, EquipSlot slot, CustomItemId itemId, uint key)
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

        _stateManager.ChangeItem(state, slot, item, StateChanged.Source.Ipc, key);
        return GlamourerErrorCode.Success;
    }

    private GlamourerErrorCode SetItemByActorName(string name, EquipSlot slot, CustomItemId itemId, uint key)
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

            _stateManager.ChangeItem(state, slot, item, StateChanged.Source.Ipc, key);
            found = true;
        }

        return found ? GlamourerErrorCode.Success : GlamourerErrorCode.ActorNotFound;
    }
}
