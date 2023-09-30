using System.Numerics;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

public class ConvenienceRevertButtons
{
    private readonly StateManager      _stateManager;
    private readonly AutoDesignApplier _autoDesignApplier;
    private readonly ObjectManager     _objects;
    private readonly Configuration     _config;


    public ConvenienceRevertButtons(StateManager stateManager, AutoDesignApplier autoDesignApplier, ObjectManager objects,
        Configuration config)
    {
        _stateManager      = stateManager;
        _autoDesignApplier = autoDesignApplier;
        _objects           = objects;
        _config            = config;
    }

    public void DrawButtons(float yPos)
    {
        _objects.Update();
        var (playerIdentifier, playerData) = _objects.PlayerData;

        string? error = null;
        if (!playerIdentifier.IsValid || !playerData.Valid)
            error = "No player character available.";

        if (!_stateManager.TryGetValue(playerIdentifier, out var state))
            error = "The player character was not modified by Glamourer yet.";
        else if (state.IsLocked)
            error = "The state of the player character is currently locked.";

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        var spacing    = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - 2 * buttonSize.X - spacing.X, yPos - 1));

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.RedoAlt.ToIconString(), buttonSize,
                error ?? "Revert the player character to its game state.", error != null, true))
            _stateManager.ResetState(state, StateChanged.Source.Manual);

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.SyncAlt.ToIconString(), buttonSize,
                error ?? "Revert the player character to its automation state.", error != null && _config.EnableAutoDesigns, true))
            foreach (var actor in playerData.Objects)
            {
                _autoDesignApplier.ReapplyAutomation(actor, playerIdentifier, state);
                _stateManager.ReapplyState(actor);
            }
    }
}
