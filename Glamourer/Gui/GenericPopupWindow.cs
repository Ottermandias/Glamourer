using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Glamourer.Gui.Materials;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

public class GenericPopupWindow : Window
{
    private readonly Configuration    _config;
    private readonly AdvancedDyePopup _advancedDye;
    private readonly ICondition       _condition;
    private readonly IClientState     _state;
    public           bool             OpenFestivalPopup { get; internal set; } = false;

    public GenericPopupWindow(Configuration config, IClientState state, ICondition condition, AdvancedDyePopup advancedDye)
        : base("Glamourer Popups",
            ImGuiWindowFlags.NoBringToFrontOnFocus
          | ImGuiWindowFlags.NoDecoration
          | ImGuiWindowFlags.NoInputs
          | ImGuiWindowFlags.NoSavedSettings
          | ImGuiWindowFlags.NoBackground
          | ImGuiWindowFlags.NoMove
          | ImGuiWindowFlags.NoNav
          | ImGuiWindowFlags.NoTitleBar, true)
    {
        _config             = config;
        _state              = state;
        _condition          = condition;
        _advancedDye        = advancedDye;
        DisableWindowSounds = true;
        IsOpen              = true;
    }

    public override void Draw()
    {
        if (OpenFestivalPopup && CheckFestivalPopupConditions())
        {
            ImGui.OpenPopup("FestivalPopup");
            OpenFestivalPopup = false;
        }

        DrawFestivalPopup();
        //_advancedDye.Draw();
    }

    private bool CheckFestivalPopupConditions()
        => !_state.IsPvPExcludingDen
         && !_condition[ConditionFlag.InCombat]
         && !_condition[ConditionFlag.BoundByDuty]
         && !_condition[ConditionFlag.WatchingCutscene]
         && !_condition[ConditionFlag.WatchingCutscene78]
         && !_condition[ConditionFlag.BoundByDuty95]
         && !_condition[ConditionFlag.BoundByDuty56]
         && !_condition[ConditionFlag.InDeepDungeon]
         && !_condition[ConditionFlag.PlayingLordOfVerminion]
         && !_condition[ConditionFlag.ChocoboRacing];


    private void DrawFestivalPopup()
    {
        var viewportSize = ImGui.GetWindowViewport().Size;
        ImGui.SetNextWindowSize(new Vector2(Math.Max(viewportSize.X / 5, 400), Math.Max(viewportSize.Y / 7, 150)));
        ImGui.SetNextWindowPos(viewportSize / 2, ImGuiCond.Always, new Vector2(0.5f));
        using var popup = ImRaii.Popup("FestivalPopup", ImGuiWindowFlags.Modal);
        if (!popup)
            return;

        ImGuiUtil.TextWrapped(
            "Glamourer has some festival-specific behaviour that is turned on by default. You can always turn this behaviour on or off in the general settings, and choose your current preference now.");

        var buttonWidth = new Vector2(150 * ImGuiHelpers.GlobalScale, 0);
        var yPos        = ImGui.GetWindowHeight() - 2 * ImGui.GetFrameHeight();
        var xPos        = (ImGui.GetWindowWidth() - ImGui.GetStyle().ItemSpacing.X) / 2 - buttonWidth.X;
        ImGui.SetCursorPos(new Vector2(xPos, yPos));
        if (ImGui.Button("Let's Check It Out!", buttonWidth))
        {
            _config.DisableFestivals = 0;
            _config.Save();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Not Right Now.", buttonWidth))
        {
            _config.DisableFestivals = 2;
            _config.Save();
            ImGui.CloseCurrentPopup();
        }
    }
}
