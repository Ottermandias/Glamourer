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
using Penumbra.GameData.Interop;
using Penumbra.String;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class StateIpcTester : IUiService, IDisposable
{
    private readonly DalamudPluginInterface _pluginInterface;

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
    private         nint                  _lastStateChangeActor;
    private         ByteString            _lastStateChangeName = ByteString.Empty;
    private         DateTime              _lastStateChangeTime;

    public readonly EventSubscriber<bool> GPoseChanged;
    private         bool                  _lastGPoseChangeValue;
    private         DateTime              _lastGPoseChangeTime;

    private int _numUnlocked;

    public StateIpcTester(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        StateChanged     = Api.IpcSubscribers.StateChangedWithType.Subscriber(_pluginInterface, OnStateChanged);
        GPoseChanged     = Api.IpcSubscribers.GPoseChanged.Subscriber(_pluginInterface, OnGPoseChange);
        StateChanged.Disable();
        GPoseChanged.Disable();
    }

    public void Dispose()
    {
        StateChanged.Dispose();
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
        PrintName();
        IpcTesterHelpers.DrawIntro("Last GPose Change");
        ImGui.TextUnformatted($"{_lastGPoseChangeValue} at {_lastGPoseChangeTime.ToLocalTime().TimeOfDay}");


        IpcTesterHelpers.DrawIntro(GetState.Label);
        DrawStatePopup();
        if (ImGui.Button("Get##Idx"))
        {
            (_lastError, _state) = new GetState(_pluginInterface).Invoke(_gameObjectIndex, _key);
            _stateString         = _state?.ToString(Formatting.Indented) ?? "No State Available";
            ImGui.OpenPopup("State");
        }

        IpcTesterHelpers.DrawIntro(GetStateName.Label);
        if (ImGui.Button("Get##Name"))
        {
            (_lastError, _state) = new GetStateName(_pluginInterface).Invoke(_gameObjectName, _key);
            _stateString         = _state?.ToString(Formatting.Indented) ?? "No State Available";
            ImGui.OpenPopup("State");
        }

        IpcTesterHelpers.DrawIntro(GetStateBase64.Label);
        if (ImGui.Button("Get##Base64Idx"))
        {
            (_lastError, _getStateString) = new GetStateBase64(_pluginInterface).Invoke(_gameObjectIndex, _key);
            _stateString                  = _getStateString ?? "No State Available";
            ImGui.OpenPopup("State");
        }

        IpcTesterHelpers.DrawIntro(GetStateBase64Name.Label);
        if (ImGui.Button("Get##Base64Idx"))
        {
            (_lastError, _getStateString) = new GetStateBase64Name(_pluginInterface).Invoke(_gameObjectName, _key);
            _stateString                  = _getStateString ?? "No State Available";
            ImGui.OpenPopup("State");
        }

        IpcTesterHelpers.DrawIntro(ApplyState.Label);
        if (ImGuiUtil.DrawDisabledButton("Apply Last##Idx", Vector2.Zero, string.Empty, _state == null))
            _lastError = new ApplyState(_pluginInterface).Invoke(_state!, _gameObjectIndex, _key, _flags);
        ImGui.SameLine();
        if (ImGui.Button("Apply Base64##Idx"))
            _lastError = new ApplyState(_pluginInterface).Invoke(_base64State, _gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(ApplyStateName.Label);
        if (ImGuiUtil.DrawDisabledButton("Apply Last##Name", Vector2.Zero, string.Empty, _state == null))
            _lastError = new ApplyStateName(_pluginInterface).Invoke(_state!, _gameObjectName, _key, _flags);
        ImGui.SameLine();
        if (ImGui.Button("Apply Base64##Name"))
            _lastError = new ApplyStateName(_pluginInterface).Invoke(_base64State, _gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertState.Label);
        if (ImGui.Button("Revert##Idx"))
            _lastError = new RevertState(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertStateName.Label);
        if (ImGui.Button("Revert##Name"))
            _lastError = new RevertStateName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);

        IpcTesterHelpers.DrawIntro(UnlockState.Label);
        if (ImGui.Button("Unlock##Idx"))
            _lastError = new UnlockState(_pluginInterface).Invoke(_gameObjectIndex, _key);

        IpcTesterHelpers.DrawIntro(UnlockStateName.Label);
        if (ImGui.Button("Unlock##Name"))
            _lastError = new UnlockStateName(_pluginInterface).Invoke(_gameObjectName, _key);

        IpcTesterHelpers.DrawIntro(UnlockAll.Label);
        if (ImGui.Button("Unlock##All"))
            _numUnlocked = new UnlockAll(_pluginInterface).Invoke(_key);
        ImGui.SameLine();
        ImGui.TextUnformatted($"Unlocked {_numUnlocked}");

        IpcTesterHelpers.DrawIntro(RevertToAutomation.Label);
        if (ImGui.Button("Revert##AutomationIdx"))
            _lastError = new RevertToAutomation(_pluginInterface).Invoke(_gameObjectIndex, _key, _flags);

        IpcTesterHelpers.DrawIntro(RevertToAutomationName.Label);
        if (ImGui.Button("Revert##AutomationName"))
            _lastError = new RevertToAutomationName(_pluginInterface).Invoke(_gameObjectName, _key, _flags);
    }

    private void DrawStatePopup()
    {
        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(500, 500));
        if (_stateString == null)
            return;

        using var p = ImRaii.Popup("State");
        if (!p)
            return;

        if (ImGui.Button("Copy to Clipboard"))
            ImGui.SetClipboardText(_stateString);
        if (_stateString[0] is '{')
        {
            ImGui.SameLine();
            if (ImGui.Button("Copy as Base64") && _state != null)
                ImGui.SetClipboardText(DesignConverter.ToBase64(_state));
        }

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        ImGuiUtil.TextWrapped(_stateString ?? string.Empty);

        if (ImGui.Button("Close", -Vector2.UnitX) || !ImGui.IsWindowFocused())
            ImGui.CloseCurrentPopup();
    }

    private unsafe void PrintName()
    {
        ImGuiNative.igTextUnformatted(_lastStateChangeName.Path, _lastStateChangeName.Path + _lastStateChangeName.Length);
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImGuiUtil.CopyOnClickSelectable($"0x{_lastStateChangeActor:X}");
        }

        ImGui.SameLine();
        ImGui.TextUnformatted($"at {_lastStateChangeTime.ToLocalTime().TimeOfDay}");
    }

    private void OnStateChanged(nint actor, StateChangeType _)
    {
        _lastStateChangeActor = actor;
        _lastStateChangeTime  = DateTime.UtcNow;
        _lastStateChangeName  = actor != nint.Zero ? ((Actor)actor).Utf8Name.Clone() : ByteString.Empty;
    }

    private void OnGPoseChange(bool value)
    {
        _lastGPoseChangeValue = value;
        _lastGPoseChangeTime  = DateTime.UtcNow;
    }
}
