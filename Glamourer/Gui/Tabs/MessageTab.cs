using Luna;

namespace Glamourer.Gui.Tabs;

public sealed class MessagesTab(MessageService messages) : ITab<MainTabType>
{
    public bool IsVisible
        => messages.Count > 0;

    public ReadOnlySpan<byte> Label
        => "Messages"u8;

    public MainTabType Identifier
        => MainTabType.Messages;

    public void DrawContent()
        => messages.DrawNotificationLog();
}
