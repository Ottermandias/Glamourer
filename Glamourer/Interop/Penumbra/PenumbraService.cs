using System;
using Dalamud.Logging;
using Dalamud.Plugin;
using Glamourer.Interop.Structs;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Glamourer.Interop.Penumbra;

public unsafe class PenumbraService : IDisposable
{
    public const int RequiredPenumbraBreakingVersion = 4;
    public const int RequiredPenumbraFeatureVersion  = 15;

    private readonly DalamudPluginInterface                              _pluginInterface;
    private readonly EventSubscriber<ChangedItemType, uint>              _tooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint> _clickSubscriber;
    private readonly EventSubscriber<nint, string, nint, nint, nint>     _creatingCharacterBase;
    private readonly EventSubscriber<nint, string, nint>                 _createdCharacterBase;
    private          ActionSubscriber<int, RedrawType>                   _redrawSubscriber;
    private          FuncSubscriber<nint, (nint, string)>                _drawObjectInfo;
    private          FuncSubscriber<int, int>                            _cutsceneParent;

    private readonly EventSubscriber _initializedEvent;
    private readonly EventSubscriber _disposedEvent;
    public           bool            Available { get; private set; }

    public PenumbraService(DalamudPluginInterface pi)
    {
        _pluginInterface       = pi;
        _initializedEvent      = Ipc.Initialized.Subscriber(pi, Reattach);
        _disposedEvent         = Ipc.Disposed.Subscriber(pi, Unattach);
        _tooltipSubscriber     = Ipc.ChangedItemTooltip.Subscriber(pi);
        _clickSubscriber       = Ipc.ChangedItemClick.Subscriber(pi);
        _createdCharacterBase  = Ipc.CreatedCharacterBase.Subscriber(pi);
        _creatingCharacterBase = Ipc.CreatingCharacterBase.Subscriber(pi);
        Reattach();
    }

    public event Action<MouseButton, ChangedItemType, uint> Click
    {
        add => _clickSubscriber.Event += value;
        remove => _clickSubscriber.Event -= value;
    }

    public event Action<ChangedItemType, uint> Tooltip
    {
        add => _tooltipSubscriber.Event += value;
        remove => _tooltipSubscriber.Event -= value;
    }


    public event Action<nint, string, nint, nint, nint> CreatingCharacterBase
    {
        add => _creatingCharacterBase.Event += value;
        remove => _creatingCharacterBase.Event -= value;
    }

    public event Action<nint, string, nint> CreatedCharacterBase
    {
        add => _createdCharacterBase.Event += value;
        remove => _createdCharacterBase.Event -= value;
    }

    /// <summary> Obtain the game object corresponding to a draw object. </summary>
    public Actor GameObjectFromDrawObject(Model drawObject)
        => Available ? _drawObjectInfo.Invoke(drawObject.Address).Item1 : Actor.Null;

    /// <summary> Obtain the parent of a cutscene actor if it is known. </summary>
    public int CutsceneParent(int idx)
        => Available ? _cutsceneParent.Invoke(idx) : -1;

    /// <summary> Try to redraw the given actor. </summary>
    public void RedrawObject(Actor actor, RedrawType settings)
    {
        if (!actor || !Available)
            return;

        try
        {
            _redrawSubscriber.Invoke(actor.AsObject->ObjectIndex, settings);
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Failure redrawing object:\n{e}");
        }
    }

    /// <summary> Reattach to the currently running Penumbra IPC provider. Unattaches before if necessary. </summary>
    public void Reattach()
    {
        try
        {
            Unattach();

            var (breaking, feature) = Ipc.ApiVersions.Subscriber(_pluginInterface).Invoke();
            if (breaking != RequiredPenumbraBreakingVersion || feature < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {breaking}.{feature:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            _tooltipSubscriber.Enable();
            _clickSubscriber.Enable();
            _creatingCharacterBase.Enable();
            _createdCharacterBase.Enable();
            _drawObjectInfo   = Ipc.GetDrawObjectInfo.Subscriber(_pluginInterface);
            _cutsceneParent   = Ipc.GetCutsceneParentIndex.Subscriber(_pluginInterface);
            _redrawSubscriber = Ipc.RedrawObjectByIndex.Subscriber(_pluginInterface);
            Available         = true;
            Item.Log.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
            Item.Log.Debug($"Could not attach to Penumbra:\n{e}");
        }
    }

    /// <summary> Unattach from the currently running Penumbra IPC provider. </summary>
    public void Unattach()
    {
        _tooltipSubscriber.Disable();
        _clickSubscriber.Disable();
        _creatingCharacterBase.Disable();
        _createdCharacterBase.Disable();
        if (Available)
        {
            Available = false;
            Item.Log.Debug("Glamourer detached from Penumbra.");
        }
    }

    public void Dispose()
    {
        Unattach();
        _tooltipSubscriber.Dispose();
        _clickSubscriber.Dispose();
        _creatingCharacterBase.Dispose();
        _createdCharacterBase.Dispose();
        _initializedEvent.Dispose();
        _disposedEvent.Dispose();
    }
}
