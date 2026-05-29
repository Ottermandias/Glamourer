using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.State;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Api;

public class PluginApi(ActorObjectManager objects, StateManager states, DesignManager designs, NavigationService navigation)
    : IGlamourerApiPlugin, IApiService
{
    public void OpenMainWindow(bool? open, MainTabType tab = MainTabType.None)
    {
        navigation.SetMainWindow(open);
        navigation.MoveTo(tab);
    }

    public void OpenDesign(Guid designId)
    {
        if (designs.Designs.ByIdentifier(designId) is { } design)
            navigation.OpenTo(design);
        else
            navigation.OpenTo(MainTabType.Designs);
    }

    public void OpenActorIndex(int objectIndex)
    {
        if (GetState(objectIndex) is { } state)
            navigation.OpenTo(state);
        else
            navigation.OpenTo(MainTabType.Actors);
    }

    public void OpenActorName(string name, ushort world = ushort.MaxValue)
    {
        if (GetState(name, world) is { } state)
            navigation.OpenTo(state);
        else
            navigation.OpenTo(MainTabType.Actors);
    }

    public void OpenQuickDesignBar(bool? open, Guid designId = default)
    {
        navigation.SetQuickDesignBar(open);
        if(designs.Designs.ByIdentifier(designId) is { } design)
            navigation.SetQuickDesign(design);
    }

    public void OpenEquipmentBarIndex(bool? open, int objectIndex = -1)
    {
        navigation.SetEquipmentBar(open);
        if (GetState(objectIndex) is { } state)
            navigation.MoveTo(state);
    }

    public void OpenEquipmentBarName(bool? open, string name, ushort world = ushort.MaxValue)
    {
        navigation.SetEquipmentBar(open);
        if (GetState(name, world) is { } state)
            navigation.MoveTo(state);
    }


    private ActorState? GetState(int index)
    {
        if (index < 0 || !objects.Actors.VerifyIndex((ObjectIndex)index))
            return null;

        var actor = objects.Objects[index];
        if (actor.Valid && states.GetOrCreate(actor, out var state))
            return state;

        return null;
    }

    private ActorState? GetState(string name, ushort world = ushort.MaxValue)
    {
        if (!ByteString.FromString(name, out var nameU8))
            return null;

        foreach (var (stateId, state) in states)
        {
            if (stateId.Type is IdentifierType.Player
             && stateId.PlayerName.Equals(nameU8)
             && (world is ushort.MaxValue || world == stateId.HomeWorld))
                return state;
        }

        foreach (var (identifier, data) in objects)
        {
            if (identifier.Type is IdentifierType.Player
             && identifier.PlayerName.Equals(nameU8)
             && (world is ushort.MaxValue || world == identifier.HomeWorld)
             && states.GetOrCreate(identifier, data.Objects[0], out var state))
                return state;
        }

        return null;
    }
}
