using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs;

public sealed class IncognitoButton(Configuration config) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => config.Ephemeral.IncognitoMode
            ? LunaStyle.IncognitoOn
            : LunaStyle.IncognitoOff;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
    {
        var hold = config.IncognitoModifier.IsActive();
        Im.Text(config.Ephemeral.IncognitoMode ? "Toggle incognito mode off."u8 : "Toggle incognito mode on."u8);
        if (!hold)
            Im.Text($"\nHold {config.IncognitoModifier} while clicking to toggle.");
    }

    public override void OnClick()
    {
        if (config.IncognitoModifier.IsActive())
            config.Ephemeral.IncognitoMode = !config.Ephemeral.IncognitoMode;
    }
}
