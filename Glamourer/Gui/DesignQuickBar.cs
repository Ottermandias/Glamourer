using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Actors;

namespace Glamourer.Gui;

public sealed class DesignQuickBar : Window, IDisposable
{
    private ImGuiWindowFlags GetFlags
        => _config.Ephemeral.LockDesignQuickBar
            ? ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoMove
            : ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoFocusOnAppearing;

    private readonly Configuration     _config;
    private readonly DesignCombo       _designCombo;
    private readonly StateManager      _stateManager;
    private readonly AutoDesignApplier _autoDesignApplier;
    private readonly ObjectManager     _objects;
    private readonly IKeyState         _keyState;
    private readonly ImRaii.Style      _windowPadding  = new();
    private readonly ImRaii.Color      _windowColor    = new();
    private          DateTime          _keyboardToggle = DateTime.UnixEpoch;
    private          int               _numButtons;

    public DesignQuickBar(Configuration config, DesignCombo designCombo, StateManager stateManager, IKeyState keyState,
        ObjectManager objects, AutoDesignApplier autoDesignApplier)
        : base("Glamourer Quick Bar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking)
    {
        _config             = config;
        _designCombo        = designCombo;
        _stateManager       = stateManager;
        _keyState           = keyState;
        _objects            = objects;
        _autoDesignApplier  = autoDesignApplier;
        IsOpen              = _config.Ephemeral.ShowDesignQuickBar;
        DisableWindowSounds = true;
        Size                = Vector2.Zero;
    }

    public void Dispose()
        => _windowPadding.Dispose();

    public override void PreOpenCheck()
    {
        CheckHotkeys();
        IsOpen = _config.Ephemeral.ShowDesignQuickBar;
    }

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
        _objects.Update();
        using var group      = ImRaii.Group();
        var       spacing    = ImGui.GetStyle().ItemInnerSpacing;
        using var style      = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       buttonSize = new Vector2(ImGui.GetFrameHeight());
        var       comboSize  = width - _numButtons * (buttonSize.X + spacing.X);
        _designCombo.Draw(comboSize);
        PrepareButtons();
        ImGui.SameLine();
        DrawApplyButton(buttonSize);
        ImGui.SameLine();
        DrawRevertButton(buttonSize);
        DrawRevertAutomationButton(buttonSize);
        DrawRevertAdvancedCustomization(buttonSize);
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
        _playerState                     = _stateManager.GetValueOrDefault(_playerIdentifier);
        _targetState                     = _stateManager.GetValueOrDefault(_targetIdentifier);
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
                tooltip   =  $"Left-Click: Apply {(_config.Ephemeral.IncognitoMode ? design.Incognito : design.Name)} to yourself.";
            }

            if (_targetIdentifier.IsValid && _targetData.Valid)
            {
                if (available != 0)
                    tooltip += '\n';
                available |= 2;
                tooltip   += $"Right-Click: Apply {(_config.Ephemeral.IncognitoMode ? design.Incognito : design.Name)} to {_targetIdentifier}.";
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

        var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();
        using var _ = design!.TemporarilyRestrictApplication(applyGear, applyCustomize, applyCrest, applyParameters);
        _stateManager.ApplyDesign(state, design, ApplySettings.Manual);
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

        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.UndoAlt, buttonSize, tooltip, available);
        if (clicked)
            _stateManager.ResetState(state!, StateSource.Manual);
    }

    public void DrawRevertAutomationButton(Vector2 buttonSize)
    {
        if (!_config.EnableAutoDesigns)
            return;

        var available = 0;
        var tooltip   = string.Empty;

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

        ImGui.SameLine();
        var (clicked, id, data, state) = ResolveTarget(FontAwesomeIcon.SyncAlt, buttonSize, tooltip, available);
        if (!clicked)
            return;

        foreach (var actor in data.Objects)
        {
            _autoDesignApplier.ReapplyAutomation(actor, id, state!);
            _stateManager.ReapplyState(actor);
        }
    }

    public void DrawRevertAdvancedCustomization(Vector2 buttonSize)
    {
        if (!_config.ShowRevertAdvancedParametersButton || !_config.UseAdvancedParameters)
            return;

        var available = 0;
        var tooltip   = string.Empty;

        if (_playerIdentifier.IsValid && _playerState is { IsLocked: false } && _playerData.Valid)
        {
            available |= 1;
            tooltip   =  "Left-Click: Revert the advanced customizations of the player character to their game state.";
        }

        if (_targetIdentifier.IsValid && _targetState is { IsLocked: false } && _targetData.Valid)
        {
            if (available != 0)
                tooltip += '\n';
            available |= 2;
            tooltip   += $"Right-Click: Revert the advanced customizations of {_targetIdentifier} to their game state.";
        }

        if (available == 0)
            tooltip = "Neither player character nor target are available or their state is locked.";

        ImGui.SameLine();
        var (clicked, _, _, state) = ResolveTarget(FontAwesomeIcon.Palette, buttonSize, tooltip, available);
        if (clicked)
            _stateManager.ResetAdvancedState(state!, StateSource.Manual);
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
        _numButtons = (_config.EnableAutoDesigns, _config is { ShowRevertAdvancedParametersButton: true, UseAdvancedParameters: true }) switch
        {
            (true, true)   => 4,
            (false, true)  => 3,
            (true, false)  => 3,
            (false, false) => 2,
        };
        Size = new Vector2((7 + _numButtons) * ImGui.GetFrameHeight() + _numButtons * ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());
        return Size.Value.X;
    }
}
