using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Glamourer.State;
using Newtonsoft.Json.Linq;
using OtterGui.Services;
using Penumbra.GameData.Interop;
using ObjectManager = Glamourer.Interop.ObjectManager;
using StateChanged = Glamourer.Events.StateChanged;

namespace Glamourer.Api;

public sealed class StateApi : IGlamourerApiState, IApiService, IDisposable
{
    private readonly ApiHelpers        _helpers;
    private readonly StateManager      _stateManager;
    private readonly DesignConverter   _converter;
    private readonly Configuration     _config;
    private readonly AutoDesignApplier _autoDesigns;
    private readonly ObjectManager     _objects;
    private readonly StateChanged      _stateChanged;
    private readonly GPoseService      _gPose;

    public StateApi(ApiHelpers helpers,
        StateManager stateManager,
        DesignConverter converter,
        Configuration config,
        AutoDesignApplier autoDesigns,
        ObjectManager objects,
        StateChanged stateChanged,
        GPoseService gPose)
    {
        _helpers      = helpers;
        _stateManager = stateManager;
        _converter    = converter;
        _config       = config;
        _autoDesigns  = autoDesigns;
        _objects      = objects;
        _stateChanged = stateChanged;
        _gPose        = gPose;
        _stateChanged.Subscribe(OnStateChange, Events.StateChanged.Priority.GlamourerIpc);
        _gPose.Subscribe(OnGPoseChange, GPoseService.Priority.GlamourerIpc);
    }

    public void Dispose()
    {
        _stateChanged.Unsubscribe(OnStateChange);
        _gPose.Unsubscribe(OnGPoseChange);
    }

    public (GlamourerApiEc, JObject?) GetState(int objectIndex, uint key)
        => Convert(_helpers.FindState(objectIndex), key);

    public (GlamourerApiEc, JObject?) GetStateName(string playerName, uint key)
        => Convert(_helpers.FindStates(playerName).FirstOrDefault(), key);

    public (GlamourerApiEc, string?) GetStateBase64(int objectIndex, uint key)
        => ConvertBase64(_helpers.FindState(objectIndex), key);

    public (GlamourerApiEc, string?) GetStateBase64Name(string objectName, uint key)
        => ConvertBase64(_helpers.FindStates(objectName).FirstOrDefault(), key);

    public GlamourerApiEc ApplyState(object applyState, int objectIndex, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "Key", key, "Flags", flags);
        if (Convert(applyState, flags, out var version) is not { } design)
            return ApiHelpers.Return(GlamourerApiEc.InvalidState, args);

        if (_helpers.FindState(objectIndex) is not { } state)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        if (version < 3 && state.ModelData.ModelId != 0)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        ApplyDesign(state, design, key, flags);
        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc ApplyStateName(object applyState, string playerName, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Name", playerName, "Key", key, "Flags", flags);
        if (Convert(applyState, flags, out var version) is not { } design)
            return ApiHelpers.Return(GlamourerApiEc.InvalidState, args);

        var states = _helpers.FindExistingStates(playerName);

        var any         = false;
        var anyUnlocked = false;
        var anyHuman    = false;
        foreach (var state in states)
        {
            any = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked = true;
            if (version < 3 && state.ModelData.ModelId != 0)
                continue;

            anyHuman = true;
            ApplyDesign(state, design, key, flags);
        }

        if (any)
            ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        if (!anyHuman)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotHuman, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc RevertState(int objectIndex, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "Key", key, "Flags", flags);
        if (_helpers.FindExistingState(objectIndex, out var state) != GlamourerApiEc.Success)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (state == null)
            return ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        Revert(state, key, flags);
        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc RevertStateName(string playerName, uint key, ApplyFlag flags)
    {
        var args   = ApiHelpers.Args("Name", playerName, "Key", key, "Flags", flags);
        var states = _helpers.FindExistingStates(playerName);

        var any         = false;
        var anyUnlocked = false;
        foreach (var state in states)
        {
            any = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked = true;
            Revert(state, key, flags);
        }

        if (any)
            ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc UnlockState(int objectIndex, uint key)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "Key", key);
        if (_helpers.FindExistingState(objectIndex, out var state) != GlamourerApiEc.Success)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (state == null)
            return ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!state.Unlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc UnlockStateName(string playerName, uint key)
    {
        var args   = ApiHelpers.Args("Name", playerName, "Key", key);
        var states = _helpers.FindExistingStates(playerName);

        var any         = false;
        var anyUnlocked = false;
        foreach (var state in states)
        {
            any         =  true;
            anyUnlocked |= state.Unlock(key);
        }

        if (any)
            ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public int UnlockAll(uint key)
        => _stateManager.Values.Count(state => state.Unlock(key));

    public GlamourerApiEc RevertToAutomation(int objectIndex, uint key, ApplyFlag flags)
    {
        var args = ApiHelpers.Args("Index", objectIndex, "Key", key, "Flags", flags);
        if (_helpers.FindExistingState(objectIndex, out var state) != GlamourerApiEc.Success)
            return ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (state == null)
            return ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!state.CanUnlock(key))
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        RevertToAutomation(_objects[objectIndex], state, key, flags);
        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public GlamourerApiEc RevertToAutomationName(string playerName, uint key, ApplyFlag flags)
    {
        var args   = ApiHelpers.Args("Name", playerName, "Key", key, "Flags", flags);
        var states = _helpers.FindExistingStates(playerName);

        var any         = false;
        var anyUnlocked = false;
        var anyReverted = false;
        foreach (var state in states)
        {
            any = true;
            if (!state.CanUnlock(key))
                continue;

            anyUnlocked =  true;
            anyReverted |= RevertToAutomation(state, key, flags) is GlamourerApiEc.Success;
        }

        if (any)
            ApiHelpers.Return(GlamourerApiEc.NothingDone, args);

        if (!anyReverted)
            ApiHelpers.Return(GlamourerApiEc.ActorNotFound, args);

        if (!anyUnlocked)
            return ApiHelpers.Return(GlamourerApiEc.InvalidKey, args);

        return ApiHelpers.Return(GlamourerApiEc.Success, args);
    }

    public event Action<nint>?                    StateChanged;
    public event Action<IntPtr, StateChangeType>? StateChangedWithType;
    public event Action<bool>?                    GPoseChanged;

    private void ApplyDesign(ActorState state, DesignBase design, uint key, ApplyFlag flags)
    {
        var once = (flags & ApplyFlag.Once) != 0;
        var settings = new ApplySettings(Source: once ? StateSource.IpcManual : StateSource.IpcFixed, Key: key, MergeLinks: true,
            ResetMaterials: !once && key != 0);
        _stateManager.ApplyDesign(state, design, settings);
        ApiHelpers.Lock(state, key, flags);
    }

    private void Revert(ActorState state, uint key, ApplyFlag flags)
    {
        var source = (flags & ApplyFlag.Once) != 0 ? StateSource.IpcManual : StateSource.IpcFixed;
        switch (flags & (ApplyFlag.Equipment | ApplyFlag.Customization))
        {
            case ApplyFlag.Equipment:
                _stateManager.ResetEquip(state, source, key);
                break;
            case ApplyFlag.Customization:
                _stateManager.ResetCustomize(state, source, key);
                break;
            case ApplyFlag.Equipment | ApplyFlag.Customization:
                _stateManager.ResetState(state, source, key);
                break;
        }

        ApiHelpers.Lock(state, key, flags);
    }

    private GlamourerApiEc RevertToAutomation(ActorState state, uint key, ApplyFlag flags)
    {
        _objects.Update();
        if (!_objects.TryGetValue(state.Identifier, out var actors) || !actors.Valid)
            return GlamourerApiEc.ActorNotFound;

        foreach (var actor in actors.Objects)
            RevertToAutomation(actor, state, key, flags);

        return GlamourerApiEc.Success;
    }

    private void RevertToAutomation(Actor actor, ActorState state, uint key, ApplyFlag flags)
    {
        var source = (flags & ApplyFlag.Once) != 0 ? StateSource.IpcManual : StateSource.IpcFixed;
        _autoDesigns.ReapplyAutomation(actor, state.Identifier, state, true, out var forcedRedraw);
        _stateManager.ReapplyState(actor, state, forcedRedraw, source);
        ApiHelpers.Lock(state, key, flags);
    }

    private (GlamourerApiEc, JObject?) Convert(ActorState? state, uint key)
    {
        if (state == null)
            return (GlamourerApiEc.ActorNotFound, null);

        if (!state.CanUnlock(key))
            return (GlamourerApiEc.InvalidKey, null);

        return (GlamourerApiEc.Success, _converter.ShareJObject(state, ApplicationRules.AllWithConfig(_config)));
    }

    private (GlamourerApiEc, string?) ConvertBase64(ActorState? state, uint key)
    {
        var (ec, jObj) = Convert(state, key);
        return (ec, jObj != null ? DesignConverter.ToBase64(jObj) : null);
    }

    private DesignBase? Convert(object? state, ApplyFlag flags, out byte version)
    {
        version = DesignConverter.Version;
        return state switch
        {
            string s  => _converter.FromBase64(s, (flags & ApplyFlag.Equipment) != 0, (flags & ApplyFlag.Customization) != 0, out version),
            JObject j => _converter.FromJObject(j, (flags & ApplyFlag.Equipment) != 0, (flags & ApplyFlag.Customization) != 0),
            _         => null,
        };
    }

    private void OnGPoseChange(bool gPose)
        => GPoseChanged?.Invoke(gPose);

    private void OnStateChange(StateChangeType type, StateSource _2, ActorState _3, ActorData actors, object? _5)
    {
        if (StateChanged != null)
            foreach (var actor in actors.Objects)
                StateChanged.Invoke(actor.Address);

        if (StateChangedWithType != null)
            foreach (var actor in actors.Objects)
                StateChangedWithType.Invoke(actor.Address, type);
    }
}
