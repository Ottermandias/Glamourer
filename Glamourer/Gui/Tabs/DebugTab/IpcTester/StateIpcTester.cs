using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using Glamourer.Designs;
using ImSharp;
using Luna;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Interop;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class StateIpcTester : IUiService, IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;

    private int            _gameObjectIndex;
    private string         _gameObjectName = string.Empty;
    private uint           _key;
    private ApplyFlag      _flags = ApplyFlagEx.DesignDefault;
    private GlamourerApiEc _lastError;
    private JObject?       _state;
    private string?        _stateString;
    private string         _base64State = string.Empty;
    private string?        _getStateString;

    public readonly EventSubscriber<bool> AutoRedrawChanged;
    private         bool                  _lastAutoRedrawChangeValue;
    private         DateTime              _lastAutoRedrawChangeTime;

    public readonly EventSubscriber<nint, StateChangeType> StateChanged;
    private         nint                                   _lastStateChangeActor;
    private         ByteString                             _lastStateChangeName = ByteString.Empty;
    private         DateTime                               _lastStateChangeTime;
    private         StateChangeType                        _lastStateChangeType;

    public readonly EventSubscriber<nint, StateFinalizationType> StateFinalized;
    private         nint                                         _lastStateFinalizeActor;
    private         ByteString                                   _lastStateFinalizeName = ByteString.Empty;
    private         DateTime                                     _lastStateFinalizeTime;
    private         StateFinalizationType                        _lastStateFinalizeType;

    public readonly EventSubscriber<bool> GPoseChanged;
    private         bool                  _lastGPoseChangeValue;
    private         DateTime              _lastGPoseChangeTime;

    private int _numUnlocked;

    public StateIpcTester(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface  = pluginInterface;
        AutoRedrawChanged = AutoReloadGearChanged.Subscriber(_pluginInterface, OnAutoRedrawChanged);
        StateChanged      = StateChangedWithType.Subscriber(_pluginInterface, OnStateChanged);
        StateFinalized    = Api.IpcSubscribers.StateFinalized.Subscriber(_pluginInterface, OnStateFinalized);
        GPoseChanged      = Api.IpcSubscribers.GPoseChanged.Subscriber(_pluginInterface, OnGPoseChange);
        AutoRedrawChanged.Disable();
        StateChanged.Disable();
        StateFinalized.Disable();
        GPoseChanged.Disable();
    }

    public void Dispose()
    {
        AutoRedrawChanged.Dispose();
        StateChanged.Dispose();
        StateFinalized.Dispose();
        GPoseChanged.Dispose();
    }

    public void Draw()
    {
        using var tree = Im.Tree.Node("State"u8);
        if (!tree)
            return;

        IpcTesterHelpers.IndexInput(ref _gameObjectIndex);
        IpcTesterHelpers.KeyInput(ref _key);
        IpcTesterHelpers.NameInput(ref _gameObjectName);
        IpcTesterHelpers.DrawFlagInput(ref _flags);
        Im.Item.SetNextWidthFull();
        Im.Input.Text("##base64"u8, ref _base64State, "Base 64 State..."u8);
        using var table = Im.Table.Begin("##table"u8, 2, TableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error"u8);
        Im.Text($"{_lastError}");
        IpcTesterHelpers.DrawIntro("Last Auto Redraw Change"u8);
        Im.Text($"{_lastAutoRedrawChangeValue} at {_lastAutoRedrawChangeTime.ToLocalTime().TimeOfDay}");
        IpcTesterHelpers.DrawIntro("Last State Change"u8);
        PrintChangeName();
        IpcTesterHelpers.DrawIntro("Last State Finalization"u8);
        PrintFinalizeName();
        IpcTesterHelpers.DrawIntro("Last GPose Change"u8);
        Im.Text($"{_lastGPoseChangeValue} at {_lastGPoseChangeTime.ToLocalTime().TimeOfDay}");


        IpcTesterHelpers.DrawIntro(GetState.LabelU8);
        DrawStatePopup();
        if (Im.Button("Get##Idx"u8))
        {
            (_lastError, _state) = new GetState(_pluginInterface).Invoke(_gameObjectIndex, _key);
            _stateString         = _state?.ToString(Formatting.Indented) ?? "No State Available";
            Im.Popup.Open("State"u8);
        }

        IpcTesterHelpers.DrawIntro(GetStateName.LabelU8);
        if (Im.Button("Get##Name"u8))
        {
            (_lastError, _state) = new GetStateName(_pluginInterface).Invoke(_gameObjectName, _key);
            _stateString         = _state?.ToString(Formatting.Indented) ?? "No State Available";
            Im.Popup.Open("State"u8);
        }

        IpcTesterHelpers.DrawIntro(GetStateBase64.LabelU8);
        if (Im.Button("Get##Base64Idx"u8))
        {
            (_lastError, _getStateString) = new GetStateBase64(_pluginInterface).Invoke(_gameObjectIndex, _key);
            _stateString                  = _getStateString ?? "No State Available";
            Im.Popup.Open("State"u8);
        }

        IpcTesterHelpers.DrawIntro(GetStateBase64Name.LabelU8);
        if (Im.Button("Get##Base64Idx"u8))
        {
            (_lastError, _getStateString) = new GetStateBase64Name(_pluginInterface).Invoke(_gameObjectName, _key);
            _stateString                  = _getStateString ?? "No State Available";
            Im.Popup.Open("State"u8);
        }

        IpcTesterHelpers.DrawIntro(ApplyState.LabelU8);
        if (ImEx.Button("Apply Last##Idx"u8, Vector2.Zero, StringU8.Empty, _state is null))
            _lastError = new ApplyState(_pluginInterface).Invoke(_state!, _gameObjectIndex, _key, _flags);
        Im.Line.Same();
        if (Im.Button("Apply Base64##Idx"u8))
            _lastError = new ApplyState(_pluginInterface).Invoke(_base64State, _gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(ApplyStateName.LabelU8);
        if (ImEx.Button("Apply Last##Name"u8, Vector2.Zero, StringU8.Empty, _state is null))
            _lastError = new ApplyStateName(_pluginInterface).Invoke(_state!, _gameObjectName, _key, _flags);
        Im.Line.Same();
        if (Im.Button("Apply Base64##Name"u8))
            _lastError = new ApplyStateName(_pluginInterface).Invoke(_base64State, _gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(ReapplyState.LabelU8);
        if (Im.Button("Reapply##Idx"u8))
            _lastError = new ReapplyState(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(ReapplyStateName.LabelU8);
        if (Im.Button("Reapply##Name"u8))
            _lastError = new ReapplyStateName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertState.LabelU8);
        if (Im.Button("Revert##Idx"u8))
            _lastError = new RevertState(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertStateName.LabelU8);
        if (Im.Button("Revert##Name"u8))
            _lastError = new RevertStateName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(UnlockState.LabelU8);
        if (Im.Button("Unlock##Idx"u8))
            _lastError = new UnlockState(_pluginInterface).Invoke(_gameObjectIndex, _key);

        IpcTesterHelpers.DrawIntro(UnlockStateName.LabelU8);
        if (Im.Button("Unlock##Name"u8))
            _lastError = new UnlockStateName(_pluginInterface).Invoke(_gameObjectName, _key);

        IpcTesterHelpers.DrawIntro(UnlockAll.LabelU8);
        if (Im.Button("Unlock##All"u8))
            _numUnlocked = new UnlockAll(_pluginInterface).Invoke(_key);
        Im.Line.Same();
        Im.Text($"Unlocked {_numUnlocked}");

        IpcTesterHelpers.DrawIntro(RevertToAutomation.LabelU8);
        if (Im.Button("Revert##AutomationIdx"u8))
            _lastError = new RevertToAutomation(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertToAutomationName.LabelU8);
        if (Im.Button("Revert##AutomationName"u8))
            _lastError = new RevertToAutomationName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);
    }

    private void DrawStatePopup()
    {
        if (_stateString is null)
            return;

        Im.Window.SetNextSize(ImEx.ScaledVector(500, 500));
        using var p = Im.Popup.Begin("State"u8);
        if (!p)
            return;

        if (Im.Button("Copy to Clipboard"u8))
            Im.Clipboard.Set(_stateString);
        if (_stateString[0] is '{')
        {
            Im.Line.Same();
            if (Im.Button("Copy as Base64"u8) && _state is not null)
                Im.Clipboard.Set(DesignConverter.ToBase64(_state));
        }

        using var font = Im.Font.PushMono();
        Im.TextWrapped(_stateString ?? string.Empty);

        if (Im.Button("Close"u8, Im.ContentRegion.Available with { Y = 0 }) || !Im.Window.Focused())
            Im.Popup.CloseCurrent();
    }

    private unsafe void PrintChangeName()
    {
        Im.Text(_lastStateChangeName.Span);
        Im.Line.NoSpacing();
        Im.Text($" ({_lastStateChangeType})");
        Im.Line.Same();
        Glamourer.Dynamis.DrawPointer(_lastStateChangeActor);

        Im.Line.Same();
        Im.Text($"at {_lastStateChangeTime.ToLocalTime().TimeOfDay}");
    }

    private unsafe void PrintFinalizeName()
    {
        Im.Text(_lastStateFinalizeName.Span);
        Im.Line.NoSpacing();
        Im.Text($" ({_lastStateFinalizeType})");
        Im.Line.Same();
        Glamourer.Dynamis.DrawPointer(_lastStateFinalizeActor);

        Im.Line.Same();
        Im.Text($"at {_lastStateFinalizeTime.ToLocalTime().TimeOfDay}");
    }

    private void OnAutoRedrawChanged(bool value)
    {
        _lastAutoRedrawChangeValue = value;
        _lastAutoRedrawChangeTime  = DateTime.UtcNow;
    }

    private void OnStateChanged(nint actor, StateChangeType type)
    {
        _lastStateChangeActor = actor;
        _lastStateChangeTime  = DateTime.UtcNow;
        _lastStateChangeName  = actor != nint.Zero ? ((Actor)actor).Utf8Name.Clone() : ByteString.Empty;
        _lastStateChangeType  = type;
    }

    private void OnStateFinalized(nint actor, StateFinalizationType type)
    {
        _lastStateFinalizeActor = actor;
        _lastStateFinalizeTime  = DateTime.UtcNow;
        _lastStateFinalizeName  = actor != nint.Zero ? ((Actor)actor).Utf8Name.Clone() : ByteString.Empty;
        _lastStateFinalizeType  = type;
    }

    private void OnGPoseChange(bool value)
    {
        _lastGPoseChangeValue = value;
        _lastGPoseChangeTime  = DateTime.UtcNow;
    }
}
