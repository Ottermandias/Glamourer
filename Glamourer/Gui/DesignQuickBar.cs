using System;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Actors;

namespace Glamourer.Gui;

public class DesignQuickBar : Window, IDisposable
{
    private ImGuiWindowFlags GetFlags
        => _config.LockDesignQuickBar
            ? ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground
            : ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing;

    private readonly Configuration     _config;
    private readonly DesignCombo       _designCombo;
    private readonly StateManager      _stateManager;
    private readonly AutoDesignApplier _autoDesignApplier;
    private readonly ObjectManager     _objects;
    private readonly IKeyState         _keyState;
    private readonly ImRaii.Style      _windowPadding  = new();
    private          DateTime          _keyboardToggle = DateTime.UnixEpoch;

    public DesignQuickBar(Configuration config, DesignCombo designCombo, StateManager stateManager, IKeyState keyState,
        ObjectManager objects, AutoDesignApplier autoDesignApplier)
        : base("Glamourer Quick Bar", ImGuiWindowFlags.NoDecoration)
    {
        _config             = config;
        _designCombo        = designCombo;
        _stateManager       = stateManager;
        _keyState           = keyState;
        _objects            = objects;
        _autoDesignApplier  = autoDesignApplier;
        IsOpen              = _config.ShowDesignQuickBar;
        DisableWindowSounds = true;
        Size                = Vector2.Zero;
    }

    public void Dispose()
        => _windowPadding.Dispose();

    public override void PreOpenCheck()
    {
        CheckHotkeys();
        IsOpen = _config.ShowDesignQuickBar;
    }

    public override void PreDraw()
    {
        Flags = GetFlags;
        Size  = new Vector2(12 * ImGui.GetFrameHeight(), ImGui.GetFrameHeight());

        _windowPadding.Push(ImGuiStyleVar.WindowPadding, new Vector2(ImGuiHelpers.GlobalScale * 4));
    }

    public override void PostDraw()
        => _windowPadding.Dispose();


    public override void Draw()
        => Draw(ImGui.GetContentRegionAvail().X);

    private void Draw(float width)
    {
        _objects.Update();
        using var group         = ImRaii.Group();
        var       spacing       = ImGui.GetStyle().ItemInnerSpacing;
        using var style         = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       contentRegion = width;
        var       buttonSize    = new Vector2(ImGui.GetFrameHeight());
        var       comboSize     = contentRegion - 3 * buttonSize.X - 3 * spacing.X;
        _designCombo.Draw(comboSize);
        PrepareButtons();
        ImGui.SameLine();
        DrawApplyButton(buttonSize);
        ImGui.SameLine();
        DrawRevertButton(buttonSize);
        ImGui.SameLine();
        DrawRevertAutomationButton(buttonSize);
    }

    private ActorIdentifier _playerIdentifier;
    private ActorData       _playerData;
    private ActorState?     _playerState;

    private ActorData       _targetData;
    private ActorIdentifier _targetIdentifier;
    private ActorState?     _targetState;

    private void PrepareButtons()
    {
        _objects.Update();
        (_playerIdentifier, _playerData) = _objects.PlayerData;
        (_targetIdentifier, _targetData) = _objects.TargetData;
        if (!_stateManager.TryGetValue(_playerIdentifier, out _playerState))
            _playerState = null;
        if (!_stateManager.TryGetValue(_targetIdentifier, out _targetState))
            _targetState = null;
    }

    private void DrawApplyButton(Vector2 size)
    {
        var design    = _designCombo.Design;
        var available = 0;
        var tooltip   = string.Empty;
        if (design == null)
        {
            tooltip = "No design selected.";
        }
        else
        {
            if (_playerIdentifier.IsValid && _playerData.Valid)
            {
                available |= 1;
                tooltip   =  $"Left-Click: Apply {(_config.IncognitoMode ? design.Incognito : design.Name)} to yourself.";
            }

            if (_targetIdentifier.IsValid && _targetData.Valid)
            {
                if (available != 0)
                    tooltip += '\n';
                available |= 2;
                tooltip   += $"Right-Click: Apply {(_config.IncognitoMode ? design.Incognito : design.Name)} to {_targetIdentifier}.";
            }

            if (available == 0)
                tooltip = "Neither player character nor target available.";
        }


        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.PlayCircle, size, tooltip, available);
        if (!clicked)
            return;

        if (state == null && !_stateManager.GetOrCreate(id, data.Objects[0], out state))
        {
            Glamourer.Messager.NotificationMessage($"Could not apply {design!.Incognito} to {id.Incognito(null)}: Failed to create state.");
            return;
        }

        var (applyGear, applyCustomize) = UiHelpers.ConvertKeysToFlags();
        using var _ = design!.TemporarilyRestrictApplication(applyGear, applyCustomize);
        _stateManager.ApplyDesign(design, state, StateChanged.Source.Manual);
    }

    public void DrawRevertButton(Vector2 buttonSize)
    {
        var available = 0;
        var tooltip   = string.Empty;
        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false })
        {
            available |= 1;
            tooltip   =  "Left-Click: Revert the player character to their game state.";
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false })
        {
            if (available != 0)
                tooltip += '\n';
            available |= 2;
            tooltip   += $"Right-Click: Revert {_targetIdentifier} to their game state.";
        }

        if (available == 0)
            tooltip = "Neither player character nor target are available, have state modified by Glamourer, or their state is locked.";

        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.RedoAlt, buttonSize, tooltip, available);
        if (clicked)
            _stateManager.ResetState(state!, StateChanged.Source.Manual);
    }

    public void DrawRevertAutomationButton(Vector2 buttonSize)
    {
        var available = 0;
        var tooltip   = string.Empty;
        if (!_config.EnableAutoDesigns)
        {
            tooltip = "Automation is not enabled, you can not reset to automation state.";
        }
        else
        {
            if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
            {
                available |= 1;
                tooltip   =  "Left-Click: Revert the player character to their automation state.";
            }

            if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
            {
                if (available != 0)
                    tooltip += '\n';
                available |= 2;
                tooltip   += $"Right-Click: Revert {_targetIdentifier} to their automation state.";
            }

            if (available == 0)
                tooltip = "Neither player character nor target are available, have state modified by Glamourer, or their state is locked.";
        }

        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.SyncAlt, buttonSize, tooltip, available);
        if (!clicked)
        { }
        else
        {
            foreach (var actor in data.Objects)
            {
                _autoDesignApplier.ReapplyAutomation(actor, id, state!);
                _stateManager.ReapplyState(actor);
            }
        }
    }

    private (bool, ActorIdentifier, ActorData, ActorState?) ResolveTarget(FontAwesomeIcon icon, Vector2 buttonSize, string tooltip,
        int available)
    {
        ImGuiUtil.DrawDisabledButton(icon.ToIconString(), buttonSize, tooltip, available == 0, true);
        if ((available & 1) == 1 && ImGui.IsItemClicked(ImGuiMouseButton.Left))
            return (true, _playerIdentifier, _playerData, _playerState);
        if ((available & 2) == 2 && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            return (true, _targetIdentifier, _targetData, _targetState);

        return (false, ActorIdentifier.Invalid, ActorData.Invalid, null);
    }

    private void CheckHotkeys()
    {
        if (_keyboardToggle > DateTime.UtcNow || !CheckKeyState(_config.ToggleQuickDesignBar, false))
            return;

        _keyboardToggle            = DateTime.UtcNow.AddMilliseconds(500);
        _config.ShowDesignQuickBar = !_config.ShowDesignQuickBar;
        _config.Save();
    }

    public bool CheckKeyState(ModifiableHotkey key, bool noKey)
    {
        if (key.Hotkey == VirtualKey.NO_KEY)
            return noKey;

        return _keyState[key.Hotkey] && key.Modifier1.IsActive() && key.Modifier2.IsActive();
    }
}
