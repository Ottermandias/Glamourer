﻿using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.State;
using OtterGui.Extensions;
using OtterGui.Log;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Api;

public class ApiHelpers(ActorObjectManager objects, StateManager stateManager, ActorManager actors) : IApiService
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal IEnumerable<ActorState> FindExistingStates(string actorName, ushort worldId = ushort.MaxValue)
    {
        if (actorName.Length == 0 || !ByteString.FromString(actorName, out var byteString))
            yield break;

        if (worldId == WorldId.AnyWorld.Id)
        {
            foreach (var state in stateManager.Values.Where(state
                         => state.Identifier.Type is IdentifierType.Player && state.Identifier.PlayerName == byteString))
                yield return state;
        }
        else
        {
            var identifier = actors.CreatePlayer(byteString, worldId);
            if (stateManager.TryGetValue(identifier, out var state))
                yield return state;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal GlamourerApiEc FindExistingState(int objectIndex, out ActorState? state)
    {
        var actor      = objects.Objects[objectIndex];
        var identifier = actor.GetIdentifier(actors);
        if (!identifier.IsValid)
        {
            state = null;
            return GlamourerApiEc.ActorNotFound;
        }

        stateManager.TryGetValue(identifier, out state);
        return GlamourerApiEc.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal ActorState? FindState(int objectIndex)
    {
        var actor      = objects.Objects[objectIndex];
        var identifier = actor.GetIdentifier(actors);
        if (identifier.IsValid && stateManager.GetOrCreate(identifier, actor, out var state))
            return state;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static DesignBase.FlagRestrictionResetter Restrict(DesignBase design, ApplyFlag flags)
        => (flags & (ApplyFlag.Equipment | ApplyFlag.Customization)) switch
        {
            ApplyFlag.Equipment                           => design.TemporarilyRestrictApplication(ApplicationCollection.Equipment),
            ApplyFlag.Customization                       => design.TemporarilyRestrictApplication(ApplicationCollection.Customizations),
            ApplyFlag.Equipment | ApplyFlag.Customization => design.TemporarilyRestrictApplication(ApplicationCollection.All),
            _                                             => design.TemporarilyRestrictApplication(ApplicationCollection.None),
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Lock(ActorState state, uint key, ApplyFlag flags)
    {
        if ((flags & ApplyFlag.Lock) != 0 && key != 0)
            state.Lock(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal IEnumerable<ActorState> FindStates(string objectName)
    {
        if (objectName.Length == 0 || !ByteString.FromString(objectName, out var byteString))
            return [];

        return stateManager.Values.Where(state => state.Identifier.Type is IdentifierType.Player && state.Identifier.PlayerName == byteString)
            .Concat(objects
                .Where(kvp => kvp.Key is { IsValid: true, Type: IdentifierType.Player } && kvp.Key.PlayerName == byteString)
                .SelectWhere(kvp =>
                {
                    if (stateManager.ContainsKey(kvp.Key))
                        return (false, null);

                    var ret = stateManager.GetOrCreate(kvp.Key, kvp.Value.Objects[0], out var state);
                    return (ret, state);
                }));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static GlamourerApiEc Return(GlamourerApiEc ec, LazyString args, [CallerMemberName] string name = "Unknown")
    {
        if (ec is GlamourerApiEc.Success or GlamourerApiEc.NothingDone)
            Glamourer.Log.Verbose($"[{name}] Called with {args}, returned {ec}.");
        else
            Glamourer.Log.Debug($"[{name}] Called with {args}, returned {ec}.");
        return ec;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static LazyString Args(params object[] arguments)
    {
        if (arguments.Length == 0)
            return new LazyString(() => "no arguments");

        return new LazyString(() =>
        {
            var sb = new StringBuilder();
            for (var i = 0; i < arguments.Length / 2; ++i)
            {
                sb.Append(arguments[2 * i]);
                sb.Append(" = ");
                if (arguments[2 * i + 1] is IEnumerable e)
                    sb.Append($"[{string.Join(',', e)}]");
                else
                    sb.Append(arguments[2 * i + 1]);
                sb.Append(", ");
            }

            return sb.ToString(0, sb.Length - 2);
        });
    }
}
