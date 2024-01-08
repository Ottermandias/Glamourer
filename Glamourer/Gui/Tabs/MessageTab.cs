using OtterGui.Classes;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs;

public class MessagesTab : ITab
{
    private readonly MessageService _messages;

    public MessagesTab(MessageService messages)
        => _messages = messages;

    public ReadOnlySpan<byte> Label
        => "Messages"u8;

    public bool IsVisible
        => _messages.Count > 0;

    public void DrawContent()
        => _messages.Draw();
}
