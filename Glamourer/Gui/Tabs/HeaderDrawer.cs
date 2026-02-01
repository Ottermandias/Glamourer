using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs;

public static class HeaderDrawer
{
    public abstract class Button
    {
        protected abstract void OnClick();

        protected virtual string Description
            => string.Empty;

        protected virtual Rgba32 BorderColor
            => ColorId.HeaderButtons.Value();

        protected virtual Rgba32 TextColor
            => ColorId.HeaderButtons.Value();

        protected virtual FontAwesomeIcon Icon
            => FontAwesomeIcon.None;

        protected virtual bool Disabled
            => false;

        public virtual bool Visible
            => true;

        public void Draw(float width)
        {
            if (!Visible)
                return;

            using var color = ImGuiColor.Border.Push(BorderColor)
                .Push(ImGuiColor.Text, TextColor, TextColor.IsVisible);
            if (ImGuiUtil.DrawDisabledButton(Icon.ToIconString(), new Vector2(width, Im.Style.FrameHeight), string.Empty, Disabled, true))
                OnClick();
            color.Pop();
            ImGuiUtil.HoverTooltip(Description);
        }
    }

    public sealed class IncognitoButton(Configuration config) : Button
    {
        protected override string Description
        {
            get
            {
                var hold = config.IncognitoModifier.IsActive();
                return (config.Ephemeral.IncognitoMode, hold)
                    switch
                    {
                        (true, true)   => "Toggle incognito mode off.",
                        (false, true)  => "Toggle incognito mode on.",
                        (true, false)  => $"Toggle incognito mode off.\n\nHold {config.IncognitoModifier} while clicking to toggle.",
                        (false, false) => $"Toggle incognito mode on.\n\nHold {config.IncognitoModifier} while clicking to toggle.",
                    };
            }
        }

        protected override FontAwesomeIcon Icon
            => config.Ephemeral.IncognitoMode
                ? FontAwesomeIcon.EyeSlash
                : FontAwesomeIcon.Eye;

        protected override void OnClick()
        {
            if (!config.IncognitoModifier.IsActive())
                return;

            config.Ephemeral.IncognitoMode = !config.Ephemeral.IncognitoMode;
            config.Ephemeral.Save();
        }
    }

    public static void Draw(string text, uint textColor, uint frameColor, Button[] leftButtons, Button[] rightButtons)
    {
        var width = Im.Style.FrameHeightWithSpacing;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding,   0)
            .Push(ImGuiStyleVar.FrameBorderSize, Im.Style.GlobalScale);

        var leftButtonSize = 0f;
        foreach (var button in leftButtons.Where(b => b.Visible))
        {
            button.Draw(width);
            Im.Line.Same();
            leftButtonSize += width;
        }

        var rightButtonSize = rightButtons.Count(b => b.Visible) * width;
        var midSize         = Im.ContentRegion.Available.X - rightButtonSize - Im.Style.GlobalScale;

        style.Pop();
        style.Push(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f + (rightButtonSize - leftButtonSize) / midSize, 0.5f));
        if (textColor != 0)
            ImGuiUtil.DrawTextButton(text, new Vector2(midSize, Im.Style.FrameHeight), frameColor, textColor);
        else
            ImGuiUtil.DrawTextButton(text, new Vector2(midSize, Im.Style.FrameHeight), frameColor);
        style.Pop();
        style.Push(ImGuiStyleVar.FrameBorderSize, Im.Style.GlobalScale);

        foreach (var button in rightButtons.Where(b => b.Visible))
        {
            Im.Line.Same();
            button.Draw(width);
        }
    }
}
