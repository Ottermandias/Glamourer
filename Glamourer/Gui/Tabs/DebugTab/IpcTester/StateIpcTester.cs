using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using Glamourer.Designs;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using OtterGui.Text;
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
        _pluginInterface = pluginInterface;
        StateChanged     = StateChangedWithType.Subscriber(_pluginInterface, OnStateChanged);
        StateFinalized   = Api.IpcSubscribers.StateFinalized.Subscriber(_pluginInterface, OnStateFinalized);
        GPoseChanged     = Api.IpcSubscribers.GPoseChanged.Subscriber(_pluginInterface, OnGPoseChange);
        StateChanged.Disable();
        StateFinalized.Disable();
        GPoseChanged.Disable();
    }

    public void Dispose()
    {
        StateChanged.Dispose();
        StateFinalized.Dispose();
        GPoseChanged.Dispose();
    }

    public void Draw()
    {
        using var tree = ImRaii.TreeNode("State");
        if (!tree)
            return;

        IpcTesterHelpers.IndexInput(ref _gameObjectIndex);
        IpcTesterHelpers.KeyInput(ref _key);
        IpcTesterHelpers.NameInput(ref _gameObjectName);
        IpcTesterHelpers.DrawFlagInput(ref _flags);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##base64", "Base 64 State...", ref _base64State, 2000);
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.SizingFixedFit);

        IpcTesterHelpers.DrawIntro("Last Error");
        ImGui.TextUnformatted(_lastError.ToString());
        IpcTesterHelpers.DrawIntro("Last State Change");
        PrintChangeName();
        IpcTesterHelpers.DrawIntro("Last State Finalization");
        PrintFinalizeName();
        IpcTesterHelpers.DrawIntro("Last GPose Change");
        ImGui.TextUnformatted($"{_lastGPoseChangeValue} at {_lastGPoseChangeTime.ToLocalTime().TimeOfDay}");


        IpcTesterHelpers.DrawIntro(GetState.Label);
        DrawStatePopup();
        if (ImUtf8.Button("Get##Idx"u8))
        {
            (_lastError, _state) = new GetState(_pluginInterface).Invoke(_gameObjectIndex, _key);
            _stateString         = _state?.ToString(Formatting.Indented) ?? "No State Available";
            ImUtf8.OpenPopup("State"u8);
        }

        IpcTesterHelpers.DrawIntro(GetStateName.Label);
        if (ImUtf8.Button("Get##Name"u8))
        {
            (_lastError, _state) = new GetStateName(_pluginInterface).Invoke(_gameObjectName, _key);
            _stateString         = _state?.ToString(Formatting.Indented) ?? "No State Available";
            ImUtf8.OpenPopup("State"u8);
        }

        IpcTesterHelpers.DrawIntro(GetStateBase64.Label);
        if (ImUtf8.Button("Get##Base64Idx"u8))
        {
            (_lastError, _getStateString) = new GetStateBase64(_pluginInterface).Invoke(_gameObjectIndex, _key);
            _stateString                  = _getStateString ?? "No State Available";
            ImUtf8.OpenPopup("State"u8);
        }

        IpcTesterHelpers.DrawIntro(GetStateBase64Name.Label);
        if (ImUtf8.Button("Get##Base64Idx"u8))
        {
            (_lastError, _getStateString) = new GetStateBase64Name(_pluginInterface).Invoke(_gameObjectName, _key);
            _stateString                  = _getStateString ?? "No State Available";
            ImUtf8.OpenPopup("State"u8);
        }

        IpcTesterHelpers.DrawIntro(ApplyState.Label);
        if (ImGuiUtil.DrawDisabledButton("Apply Last##Idx", Vector2.Zero, string.Empty, _state == null))
            _lastError = new ApplyState(_pluginInterface).Invoke(_state!, _gameObjectIndex, _key, _flags);
        ImGui.SameLine();
        if (ImUtf8.Button("Apply Base64##Idx"u8))
            _lastError = new ApplyState(_pluginInterface).Invoke(_base64State, _gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(ApplyStateName.Label);
        if (ImGuiUtil.DrawDisabledButton("Apply Last##Name", Vector2.Zero, string.Empty, _state == null))
            _lastError = new ApplyStateName(_pluginInterface).Invoke(_state!, _gameObjectName, _key, _flags);
        ImGui.SameLine();
        if (ImUtf8.Button("Apply Base64##Name"u8))
            _lastError = new ApplyStateName(_pluginInterface).Invoke(_base64State, _gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertState.Label);
        if (ImUtf8.Button("Revert##Idx"u8))
            _lastError = new RevertState(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertStateName.Label);
        if (ImUtf8.Button("Revert##Name"u8))
            _lastError = new RevertStateName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(UnlockState.Label);
        if (ImUtf8.Button("Unlock##Idx"u8))
            _lastError = new UnlockState(_pluginInterface).Invoke(_gameObjectIndex, _key);

        IpcTesterHelpers.DrawIntro(UnlockStateName.Label);
        if (ImUtf8.Button("Unlock##Name"u8))
            _lastError = new UnlockStateName(_pluginInterface).Invoke(_gameObjectName, _key);

        IpcTesterHelpers.DrawIntro(UnlockAll.Label);
        if (ImUtf8.Button("Unlock##All"u8))
            _numUnlocked = new UnlockAll(_pluginInterface).Invoke(_key);
        ImGui.SameLine();
        ImGui.TextUnformatted($"Unlocked {_numUnlocked}");

        IpcTesterHelpers.DrawIntro(RevertToAutomation.Label);
        if (ImUtf8.Button("Revert##AutomationIdx"u8))
            _lastError = new RevertToAutomation(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertToAutomationName.Label);
        if (ImUtf8.Button("Revert##AutomationName"u8))
            _lastError = new RevertToAutomationName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);
    }

    private void DrawStatePopup()
    {
        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(500, 500));
        if (_stateString == null)
            return;

        using var p = ImUtf8.Popup("State"u8);
        if (!p)
            return;

        if (ImUtf8.Button("Copy to Clipboard"u8))
            ImUtf8.SetClipboardText(_stateString);
        if (_stateString[0] is '{')
        {
            ImGui.SameLine();
            if (ImUtf8.Button("Copy as Base64"u8) && _state != null)
                ImUtf8.SetClipboardText(DesignConverter.ToBase64(_state));
        }

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        ImUtf8.TextWrapped(_stateString ?? string.Empty);

        if (ImUtf8.Button("Close"u8, -Vector2.UnitX) || !ImGui.IsWindowFocused())
            ImGui.CloseCurrentPopup();
    }

    private unsafe void PrintChangeName()
    {
        ImUtf8.Text(_lastStateChangeName.Span);
        ImGui.SameLine(0, 0);
        ImUtf8.Text($" ({_lastStateChangeType})");
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImUtf8.CopyOnClickSelectable($"0x{_lastStateChangeActor:X}");
        }

        ImGui.SameLine();
        ImUtf8.Text($"at {_lastStateChangeTime.ToLocalTime().TimeOfDay}");
    }

    private unsafe void PrintFinalizeName()
    {
        ImUtf8.Text(_lastStateFinalizeName.Span);
        ImGui.SameLine(0, 0);
        ImUtf8.Text($" ({_lastStateFinalizeType})");
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImUtf8.CopyOnClickSelectable($"0x{_lastStateFinalizeActor:X}");
        }

        ImGui.SameLine();
        ImUtf8.Text($"at {_lastStateFinalizeTime.ToLocalTime().TimeOfDay}");
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
