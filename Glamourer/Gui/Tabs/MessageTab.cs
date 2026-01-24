using Luna;
using ITab = OtterGui.Widgets.ITab;

namespace Glamourer.Gui.Tabs;

public class MessagesTab(MessageService messages) : ITab
{
    public ReadOnlySpan<byte> Label
        => "Messages"u8;

    public bool IsVisible
        => messages.Count > 0;

    public void DrawContent()
        => messages.DrawNotificationLog();
}
