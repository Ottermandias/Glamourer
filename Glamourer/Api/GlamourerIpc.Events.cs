using System;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Glamourer.State;
using Penumbra.Api.Helpers;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelStateChanged = "Glamourer.StateChanged";

    private readonly StateChanged                                         _stateChangedEvent;
    private readonly EventProvider<StateChanged.Type, nint, Lazy<string>> _stateChangedProvider;

    private void OnStateChanged(StateChanged.Type type, StateChanged.Source source, ActorState state, ActorData actors, object? data = null)
    {
        foreach (var actor in actors.Objects)
            _stateChangedProvider.Invoke(type, actor.Address, new Lazy<string>(() => _designConverter.ShareBase64(state)));
    }
}
