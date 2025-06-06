﻿using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui;

[Flags]
public enum QdbButtons
{
    ApplyDesign                 = 0x01,
    RevertAll                   = 0x02,
    RevertAutomation            = 0x04,
    RevertAdvancedDyes          = 0x08,
    RevertEquip                 = 0x10,
    RevertCustomize             = 0x20,
    ReapplyAutomation           = 0x40,
    ResetSettings               = 0x80,
    RevertAdvancedCustomization = 0x100,
}

public sealed class DesignQuickBar : Window, IDisposable
{
    private ImGuiWindowFlags GetFlags
        => _config.Ephemeral.LockDesignQuickBar
            ? ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoMove
            : ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoFocusOnAppearing;

    private readonly Configuration      _config;
    private readonly QuickDesignCombo   _designCombo;
    private readonly StateManager       _stateManager;
    private readonly AutoDesignApplier  _autoDesignApplier;
    private readonly ActorObjectManager _objects;
    private readonly PenumbraService    _penumbra;
    private readonly IKeyState          _keyState;
    private readonly ImRaii.Style       _windowPadding  = new();
    private readonly ImRaii.Color       _windowColor    = new();
    private          DateTime           _keyboardToggle = DateTime.UnixEpoch;
    private          int                _numButtons;
    private readonly StringBuilder      _tooltipBuilder = new(512);

    public DesignQuickBar(Configuration config, QuickDesignCombo designCombo, StateManager stateManager, IKeyState keyState,
        ActorObjectManager objects, AutoDesignApplier autoDesignApplier, PenumbraService penumbra)
        : base("Glamourer Quick Bar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking)
    {
        _config             = config;
        _designCombo        = designCombo;
        _stateManager       = stateManager;
        _keyState           = keyState;
        _objects            = objects;
        _autoDesignApplier  = autoDesignApplier;
        _penumbra           = penumbra;
        IsOpen              = _config.Ephemeral.ShowDesignQuickBar;
        DisableWindowSounds = true;
        Size                = Vector2.Zero;
    }

    public void Dispose()
        => _windowPadding.Dispose();

    public override void PreOpenCheck()
    {
        CheckHotkeys();
        IsOpen = _config.Ephemeral.ShowDesignQuickBar && _config.QdbButtons != 0;
    }

    public override bool DrawConditions()
        => _objects.Player.Valid;

    public override void PreDraw()
    {
        Flags = GetFlags;
        UpdateWidth();

        _windowPadding.Push(ImGuiStyleVar.WindowPadding, new Vector2(ImGuiHelpers.GlobalScale * 4))
            .Push(ImGuiStyleVar.WindowBorderSize, 0);
        _windowColor.Push(ImGuiCol.WindowBg, ColorId.QuickDesignBg.Value())
            .Push(ImGuiCol.Button,  ColorId.QuickDesignButton.Value())
            .Push(ImGuiCol.FrameBg, ColorId.QuickDesignFrame.Value());
    }

    public override void PostDraw()
    {
        _windowPadding.Dispose();
        _windowColor.Dispose();
    }

    public void DrawAtEnd(float yPos)
    {
        var width = UpdateWidth();
        ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - width, yPos - ImGuiHelpers.GlobalScale));
        Draw();
    }

    public override void Draw()
        => Draw(ImGui.GetContentRegionAvail().X);

    private void Draw(float width)
    {
        using var group      = ImUtf8.Group();
        var       spacing    = ImGui.GetStyle().ItemInnerSpacing;
        using var style      = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       buttonSize = new Vector2(ImGui.GetFrameHeight());
        PrepareButtons();
        if (_config.QdbButtons.HasFlag(QdbButtons.ApplyDesign))
        {
            var comboSize = width - _numButtons * (buttonSize.X + spacing.X);
            _designCombo.Draw(comboSize);
            ImGui.SameLine();
            DrawApplyButton(buttonSize);
        }

        DrawRevertButton(buttonSize);
        DrawRevertEquipButton(buttonSize);
        DrawRevertCustomizeButton(buttonSize);
        DrawRevertAdvancedCustomization(buttonSize);
        DrawRevertAdvancedDyes(buttonSize);
        DrawRevertAutomationButton(buttonSize);
        DrawReapplyAutomationButton(buttonSize);
        DrawResetSettingsButton(buttonSize);
    }

    private ActorIdentifier _playerIdentifier;
    private ActorData       _playerData;
    private ActorState?     _playerState;

    private ActorData       _targetData;
    private ActorIdentifier _targetIdentifier;
    private ActorState?     _targetState;

    private void PrepareButtons()
    {
        (_playerIdentifier, _playerData) = _objects.PlayerData;
        (_targetIdentifier, _targetData) = _objects.TargetData;
        _playerState                     = _stateManager.GetValueOrDefault(_playerIdentifier);
        _targetState                     = _stateManager.GetValueOrDefault(_targetIdentifier);
    }

    private void DrawApplyButton(Vector2 size)
    {
        var design    = _designCombo.Design as Design;
        var available = 0;
        _tooltipBuilder.Clear();

        if (design == null)
        {
            _tooltipBuilder.Append("No design selected.");
        }
        else
        {
            if (_playerIdentifier.IsValid && _playerData.Valid)
            {
                available |= 1;
                _tooltipBuilder.Append("Left-Click: Apply ")
                    .Append(design.ResolveName(_config.Ephemeral.IncognitoMode))
                    .Append(" to yourself.");
            }

            if (_targetIdentifier.IsValid && _targetData.Valid)
            {
                if (available != 0)
                    _tooltipBuilder.Append('\n');
                available |= 2;
                _tooltipBuilder.Append("Right-Click: Apply ")
                    .Append(design.ResolveName(_config.Ephemeral.IncognitoMode))
                    .Append(" to ").Append(_config.Ephemeral.IncognitoMode ? _targetIdentifier.Incognito(null) : _targetIdentifier.ToName());
            }

            if (available == 0)
                _tooltipBuilder.Append("Neither player character nor target available.");
        }


        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.PlayCircle, size, available);
        ImGui.SameLine();
        if (!clicked)
            return;

        if (state == null && !_stateManager.GetOrCreate(id, data.Objects[0], out state))
        {
            Glamourer.Messager.NotificationMessage(
                $"Could not apply {design!.ResolveName(true)} to {id.Incognito(null)}: Failed to create state.");
            return;
        }

        using var _ = design!.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
        _stateManager.ApplyDesign(state, design, ApplySettings.ManualWithLinks with { IsFinal = true });
    }

    private void DrawRevertButton(Vector2 buttonSize)
    {
        if (!_config.QdbButtons.HasFlag(QdbButtons.RevertAll))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false })
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Revert the player character to their game state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false })
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Revert ")
                .Append(_targetIdentifier)
                .Append(" to their game state.");
        }

        if (available == 0)
            _tooltipBuilder.Append(
                "Neither player character nor target are available, have state modified by Glamourer, or their state is locked.");

        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.UndoAlt, buttonSize, available);
        ImGui.SameLine();
        if (clicked)
            _stateManager.ResetState(state!, StateSource.Manual, isFinal: true);
    }

    private void DrawRevertAutomationButton(Vector2 buttonSize)
    {
        if (!_config.EnableAutoDesigns)
            return;

        if (!_config.QdbButtons.HasFlag(QdbButtons.RevertAutomation))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Revert the player character to their automation state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Revert ")
                .Append(_targetIdentifier)
                .Append(" to their automation state.");
        }

        if (available == 0)
            _tooltipBuilder.Append(
                "Neither player character nor target are available, have state modified by Glamourer, or their state is locked.");

        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.SyncAlt, buttonSize, available);
        ImGui.SameLine();
        if (!clicked)
            return;

        foreach (var actor in data.Objects)
        {
            _autoDesignApplier.ReapplyAutomation(actor, id, state!, true, false, out var forcedRedraw);
            _stateManager.ReapplyAutomationState(actor, forcedRedraw, true, StateSource.Manual);
        }
    }

    private void DrawReapplyAutomationButton(Vector2 buttonSize)
    {
        if (!_config.EnableAutoDesigns)
            return;

        if (!_config.QdbButtons.HasFlag(QdbButtons.ReapplyAutomation))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Reapply the player character's current automation on top of their current state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Reapply ")
                .Append(_targetIdentifier)
                .Append("'s current automation on top of their current state.");
        }

        if (available == 0)
            _tooltipBuilder.Append(
                "Neither player character nor target are available, have state modified by Glamourer, or their state is locked.");

        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.Repeat, buttonSize, available);
        ImGui.SameLine();
        if (!clicked)
            return;

        foreach (var actor in data.Objects)
        {
            _autoDesignApplier.ReapplyAutomation(actor, id, state!, false, false, out var forcedRedraw);
            _stateManager.ReapplyAutomationState(actor, forcedRedraw, false, StateSource.Manual);
        }
    }

    private void DrawRevertAdvancedCustomization(Vector2 buttonSize)
    {
        if (!_config.QdbButtons.HasFlag(QdbButtons.RevertAdvancedCustomization))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Revert the advanced customizations of the player character to their game state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Revert the advanced customizations of ")
                .Append(_targetIdentifier)
                .Append(" to their game state.");
        }

        if (available == 0)
            _tooltipBuilder.Append("Neither player character nor target are available or their state is locked.");

        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.PaintBrush, buttonSize, available);
        ImGui.SameLine();
        if (clicked)
            _stateManager.ResetAdvancedCustomizations(state!, StateSource.Manual);
    }

    private void DrawRevertAdvancedDyes(Vector2 buttonSize)
    {
        if (!_config.QdbButtons.HasFlag(QdbButtons.RevertAdvancedDyes))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Revert the advanced dyes of the player character to their game state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Revert the advanced dyes of ")
                .Append(_targetIdentifier)
                .Append(" to their game state.");
        }

        if (available == 0)
            _tooltipBuilder.Append("Neither player character nor target are available or their state is locked.");

        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.Palette, buttonSize, available);
        ImGui.SameLine();
        if (clicked)
            _stateManager.ResetAdvancedDyes(state!, StateSource.Manual);
    }

    private void DrawRevertCustomizeButton(Vector2 buttonSize)
    {
        if (!_config.QdbButtons.HasFlag(QdbButtons.RevertCustomize))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Revert the customizations of the player character to their game state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Revert the customizations of ")
                .Append(_targetIdentifier)
                .Append(" to their game state.");
        }

        if (available == 0)
            _tooltipBuilder.Append("Neither player character nor target are available or their state is locked.");

        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.User, buttonSize, available);
        ImGui.SameLine();
        if (clicked)
            _stateManager.ResetCustomize(state!, StateSource.Manual);
    }

    private void DrawRevertEquipButton(Vector2 buttonSize)
    {
        if (!_config.QdbButtons.HasFlag(QdbButtons.RevertEquip))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder.Append("Left-Click: Revert the equipment of the player character to its game state.");
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder.Append("Right-Click: Revert the equipment of ")
                .Append(_targetIdentifier)
                .Append(" to its game state.");
        }

        if (available == 0)
            _tooltipBuilder.Append("Neither player character nor target are available or their state is locked.");

        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.Vest, buttonSize, available);
        ImGui.SameLine();
        if (clicked)
            _stateManager.ResetEquip(state!, StateSource.Manual);
    }

    private void DrawResetSettingsButton(Vector2 buttonSize)
    {
        if (!_config.QdbButtons.HasFlag(QdbButtons.ResetSettings))
            return;

        var available = 0;
        _tooltipBuilder.Clear();

        if (_playerIdentifier.IsValid && _playerData.Valid)
        {
            available |= 1;
            _tooltipBuilder
                .Append(
                    "Left-Click: Reset all temporary settings applied by Glamourer (manually or through automation) to the collection affecting ")
                .Append(_playerIdentifier)
                .Append('.');
        }

        if (_targetIdentifier.IsValid && _targetData.Valid)
        {
            if (available != 0)
                _tooltipBuilder.Append('\n');
            available |= 2;
            _tooltipBuilder
                .Append(
                    "Right-Click: Reset all temporary settings applied by Glamourer (manually or through automation) to the collection affecting ")
                .Append(_targetIdentifier)
                .Append('.');
        }

        if (available == 0)
            _tooltipBuilder.Append("Neither player character nor target are available to identify their collections.");

        var (clicked, _, data, _) = ResolveTarget(FontAwesomeIcon.Cog, buttonSize, available);
        ImGui.SameLine();
        if (clicked)
        {
            _penumbra.RemoveAllTemporarySettings(data.Objects[0].Index, StateSource.Manual);
            _penumbra.RemoveAllTemporarySettings(data.Objects[0].Index, StateSource.Fixed);
        }
    }

    private (bool, ActorIdentifier, ActorData, ActorState?) ResolveTarget(FontAwesomeIcon icon, Vector2 buttonSize, int available)
    {
        var enumerator = _tooltipBuilder.GetChunks();
        var span       = enumerator.MoveNext() ? enumerator.Current.Span : [];
        ImUtf8.IconButton(icon, span, buttonSize, available == 0);
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

        _keyboardToggle                      = DateTime.UtcNow.AddMilliseconds(500);
        _config.Ephemeral.ShowDesignQuickBar = !_config.Ephemeral.ShowDesignQuickBar;
        _config.Ephemeral.Save();
    }

    private bool CheckKeyState(ModifiableHotkey key, bool noKey)
    {
        if (key.Hotkey == VirtualKey.NO_KEY)
            return noKey;

        return _keyState[key.Hotkey] && key.Modifier1.IsActive() && key.Modifier2.IsActive();
    }

    private float UpdateWidth()
    {
        _numButtons = 0;
        if (_config.QdbButtons.HasFlag(QdbButtons.RevertAll))
            ++_numButtons;
        if (_config.EnableAutoDesigns)
        {
            if (_config.QdbButtons.HasFlag(QdbButtons.RevertAutomation))
                ++_numButtons;
            if (_config.QdbButtons.HasFlag(QdbButtons.ReapplyAutomation))
                ++_numButtons;
        }

        if (_config.QdbButtons.HasFlag(QdbButtons.RevertAdvancedCustomization))
            ++_numButtons;
        if (_config.QdbButtons.HasFlag(QdbButtons.RevertAdvancedDyes))
            ++_numButtons;
        if (_config.QdbButtons.HasFlag(QdbButtons.RevertCustomize))
            ++_numButtons;
        if (_config.QdbButtons.HasFlag(QdbButtons.RevertEquip))
            ++_numButtons;
        if (_config.UseTemporarySettings && _config.QdbButtons.HasFlag(QdbButtons.ResetSettings))
            ++_numButtons;
        if (_config.QdbButtons.HasFlag(QdbButtons.ApplyDesign))
        {
            ++_numButtons;
            Size = new Vector2((7 + _numButtons) * ImGui.GetFrameHeight() + _numButtons * ImGui.GetStyle().ItemInnerSpacing.X,
                ImGui.GetFrameHeight());
        }
        else
        {
            Size = new Vector2(
                _numButtons * ImGui.GetFrameHeight()
              + (_numButtons - 1) * ImGui.GetStyle().ItemInnerSpacing.X
              + ImGui.GetStyle().WindowPadding.X * 2,
                ImGui.GetFrameHeight());
        }

        return Size.Value.X;
    }
}
