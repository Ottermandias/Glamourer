using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

public class GenericPopupWindow : Window
{
    private Configuration _config;
    public  bool          OpenFestivalPopup { get; internal set; } = false;

    public GenericPopupWindow(Configuration config)
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
        _config = config;
        IsOpen  = true;
    }

    public override void Draw()
    {
        if (OpenFestivalPopup)
        {
            ImGui.OpenPopup("FestivalPopup");
            OpenFestivalPopup = false;
        }

        DrawFestivalPopup();
    }


    private void DrawFestivalPopup()
    {
        var viewportSize = ImGui.GetWindowViewport().Size;
        ImGui.SetNextWindowSize(new Vector2(viewportSize.X / 5, viewportSize.Y / 7));
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
