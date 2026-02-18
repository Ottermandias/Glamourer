using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ImSharp;

namespace Glamourer.Gui;

public class GenericPopupWindow : Luna.Window
{
    private readonly Configuration.Configuration _config;
    private readonly ICondition                  _condition;
    private readonly IClientState                _state;
    public           bool                        OpenFestivalPopup { get; internal set; }

    public GenericPopupWindow(Configuration.Configuration config, IClientState state, ICondition condition)
        : base("Glamourer Popups",
            WindowFlags.NoBringToFrontOnFocus
          | WindowFlags.NoDecoration
          | WindowFlags.NoInputs
          | WindowFlags.NoSavedSettings
          | WindowFlags.NoBackground
          | WindowFlags.NoMove
          | WindowFlags.NoNav
          | WindowFlags.NoTitleBar, true)
    {
        _config             = config;
        _state              = state;
        _condition          = condition;
        DisableWindowSounds = true;
        IsOpen              = true;
    }

    public override void Draw()
    {
        if (OpenFestivalPopup && CheckFestivalPopupConditions())
        {
            Im.Popup.Open("FestivalPopup"u8);
            OpenFestivalPopup = false;
        }

        DrawFestivalPopup();
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
        var viewportSize = Im.Window.Viewport.Size;
        Im.Window.SetNextSize(new Vector2(Math.Max(viewportSize.X / 5, 400), Math.Max(viewportSize.Y / 7, 150)));
        Im.Window.SetNextPosition(viewportSize / 2, Condition.Always, new Vector2(0.5f));
        using var popup = Im.Popup.Begin("FestivalPopup"u8, WindowFlags.Modal);
        if (!popup)
            return;

        Im.TextWrapped(
            "Glamourer has some festival-specific behaviour that is turned on by default. You can always turn this behaviour on or off in the general settings, and choose your current preference now."u8);

        var buttonWidth = new Vector2(150 * Im.Style.GlobalScale, 0);
        var yPos        = Im.Window.Height - 2 * Im.Style.FrameHeight;
        var xPos        = (Im.Window.Width - Im.Style.ItemSpacing.X) / 2 - buttonWidth.X;
        Im.Cursor.Position = new Vector2(xPos, yPos);
        if (Im.Button("Let's Check It Out!"u8, buttonWidth))
        {
            _config.DisableFestivals = 0;
            _config.Save();
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Not Right Now."u8, buttonWidth))
        {
            _config.DisableFestivals = 2;
            _config.Save();
            Im.Popup.CloseCurrent();
        }
    }
}
