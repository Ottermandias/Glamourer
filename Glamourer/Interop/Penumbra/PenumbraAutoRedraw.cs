using Dalamud.Plugin.Services;
using Glamourer.Api.Enums;
using Glamourer.Config;
using Glamourer.Events;
using Glamourer.State;
using Luna;
using Penumbra.Api.Enums;
using Penumbra.Api.Wrappers;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Penumbra;

public class PenumbraAutoRedrawSkip : IService
{
    private bool _skipAutoUpdates;

    public BoolSetter SkipAutoUpdates(bool skip)
        => new(ref _skipAutoUpdates, skip);

    public bool Skip
        => _skipAutoUpdates;
}

public sealed class PenumbraAutoRedraw : IDisposable, IRequiredService
{
    private const    int                    WaitFrames = 5;
    private readonly Configuration          _config;
    private readonly PenumbraSubscriber     _penumbra;
    private readonly StateManager           _state;
    private readonly ActorObjectManager     _objects;
    private readonly IFramework             _framework;
    private readonly StateChanged           _stateChanged;
    private readonly PenumbraAutoRedrawSkip _skip;


    public PenumbraAutoRedraw(PenumbraSubscriber penumbra, Configuration config, StateManager state, ActorObjectManager objects,
        IFramework framework,
        StateChanged stateChanged, PenumbraAutoRedrawSkip skip)
    {
        _penumbra                                =  penumbra;
        _config                                  =  config;
        _state                                   =  state;
        _objects                                 =  objects;
        _framework                               =  framework;
        _stateChanged                            =  stateChanged;
        _skip                                    =  skip;
        _penumbra.Collections.ModSettingsChanged += OnModSettingsChange;
        _penumbra.Collections.CollectionChanged  += OnCollectionChange;
        _framework.Update                        += OnFramework;
        _stateChanged.Subscribe(OnStateChanged, StateChanged.Priority.PenumbraAutoRedraw);
    }

    public void Dispose()
    {
        _penumbra.Collections.ModSettingsChanged -= OnModSettingsChange;
        _penumbra.Collections.CollectionChanged  -= OnCollectionChange;
        _framework.Update                        -= OnFramework;
        _stateChanged.Unsubscribe(OnStateChanged);
    }

    private readonly ConcurrentQueue<(ActorState, Action, int)> _actions = [];
    private readonly ConcurrentSet<ActorState>                  _skips   = [];
    private          DateTime                                   _frame;

    private void OnStateChanged(in StateChanged.Arguments arguments)
    {
        if (arguments.Type is StateChangeType.Design && arguments.Source.IsIpc())
            _skips.TryAdd(arguments.State);
    }

    private void OnFramework(IFramework _)
    {
        var count = _actions.Count;
        while (_actions.TryDequeue(out var tuple) && count-- > 0)
        {
            if (_skips.ContainsKey(tuple.Item1))
            {
                _skips.TryRemove(tuple.Item1);
                continue;
            }

            if (tuple.Item3 > 0)
                _actions.Enqueue((tuple.Item1, tuple.Item2, tuple.Item3 - 1));
            else
                tuple.Item2();
        }
    }

    private void OnModSettingsChange(in ModSettingsChangedArguments arguments)
    {
        // Can only be triggered when subscribed to Penumbra.
        var type         = arguments.Type;
        var collectionId = arguments.Collection;
        if (type is ModSettingChange.TemporaryMod)
        {
            _framework.RunOnFrameworkThread(() =>
            {
                foreach (var (id, state) in _state)
                {
                    if (!_objects.TryGetValue(id, out var actors) || !actors.Valid)
                        continue;

                    var collection = _penumbra.Collections.ObjectCollectionId(actors.Objects[0].Index).Identifier;
                    if (collection != collectionId)
                        continue;

                    _actions.Enqueue((state, () =>
                    {
                        foreach (var actor in actors.Objects)
                            _state.ReapplyState(actor, state, false, StateSource.IpcManual, true);
                        Glamourer.Log.Debug($"Automatically applied mod settings of type {type} to {id.Incognito(null)}.");
                    }, WaitFrames));
                }
            });
        }
        else if (_config.AutoRedrawEquipOnChanges && !_skip.Skip)
        {
            // Only update once per frame.
            var playerName = _penumbra.Collections.PlayerCollectionId.Identifier;
            if (playerName != collectionId)
                return;

            var currentFrame = _framework.LastUpdateUTC;
            if (currentFrame == _frame)
                return;

            _frame = currentFrame;
            _framework.RunOnFrameworkThread(() =>
            {
                _state.ReapplyState(_objects.Player, false, StateSource.IpcManual, true);
                Glamourer.Log.Debug(
                    $"Automatically applied mod settings of type {type} to {_objects.PlayerData.Identifier.Incognito(null)} (Local Player).");
            });
        }
    }

    private void OnCollectionChange(in CollectionChangedArguments arguments)
    {
        if (!_config.AutoRedrawEquipOnChanges || _skip.Skip)
            return;

        if ((byte)arguments.Type is ApiCollectionTypeExtensions.Inactive or ApiCollectionTypeExtensions.Temporary)
            return;

        // Only update once per frame.
        var currentFrame = _framework.LastUpdateUTC;
        if (currentFrame == _frame)
            return;

        _frame = currentFrame;
        _framework.RunOnFrameworkThread(() =>
        {
            _state.ReapplyState(_objects.Player, false, StateSource.IpcManual, true);
            Glamourer.Log.Debug(
                $"Automatically applied collection assignment change to {_objects.PlayerData.Identifier.Incognito(null)} (Local Player).");
        });
    }
}
