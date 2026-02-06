using Dalamud.Plugin.Services;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorFilter : TextFilterBase<ActorCacheItem>, IUiService
{
    private readonly IPlayerState _playerState;

    private enum FilterMethod
    {
        Player,
        Owned,
        Npc,
        Retainer,
        Special,
        Homeworld,
        Text,
        Empty,
    };

    private FilterMethod _method = FilterMethod.Empty;

    public ActorFilter(IPlayerState playerState)
    {
        _playerState = playerState;
        FilterChanged += () =>
        {
            _method = Text switch
            {
                ""             => FilterMethod.Empty,
                "<p>" or "<P>" => FilterMethod.Player,
                "<o>" or "<O>" => FilterMethod.Owned,
                "<n>" or "<N>" => FilterMethod.Npc,
                "<r>" or "<R>" => FilterMethod.Retainer,
                "<s>" or "<S>" => FilterMethod.Special,
                "<w>" or "<W>" => FilterMethod.Homeworld,
                _              => FilterMethod.Text,
            };
        };
    }

    public override bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
    {
        var ret = base.DrawFilter(label, availableRegion);

        if (!Im.Item.Hovered())
            return ret;

        using var tt = Im.Tooltip.Begin();
        Im.Text("Filter for names containing the input."u8);
        Im.Dummy(new Vector2(0, Im.Style.TextHeight / 2));
        Im.Text("Special filters are:"u8);
        var color = ColorId.HeaderButtons.Value();
        Im.Text("<p>"u8, color);
        Im.Line.NoSpacing();
        Im.Text(": show only player characters."u8);


        Im.Text("<o>"u8, color);
        Im.Line.NoSpacing();
        Im.Text(": show only owned game objects."u8);

        Im.Text("<n>"u8, color);
        Im.Line.NoSpacing();
        Im.Text(": show only NPCs."u8);

        Im.Text("<r>"u8, color);
        Im.Line.NoSpacing();
        Im.Text(": show only retainers."u8);

        Im.Text("<s>"u8, color);
        Im.Line.NoSpacing();
        Im.Text(": show only special screen characters."u8);

        Im.Text("<w>"u8, color);
        Im.Line.NoSpacing();
        Im.Text(": show only players from your world."u8);
        return ret;
    }

    protected override string ToFilterString(in ActorCacheItem item, int globalIndex)
        => item.DisplayText.Utf16;

    public override bool WouldBeVisible(in ActorCacheItem item, int globalIndex)
        => _method switch
        {
            FilterMethod.Player   => item.Identifier.Type is IdentifierType.Player,
            FilterMethod.Owned    => item.Identifier.Type is IdentifierType.Owned,
            FilterMethod.Npc      => item.Identifier.Type is IdentifierType.Npc,
            FilterMethod.Retainer => item.Identifier.Type is IdentifierType.Retainer,
            FilterMethod.Special  => item.Identifier.Type is IdentifierType.Special,
            FilterMethod.Homeworld => item.Identifier.Type is IdentifierType.Player
             && item.Identifier.HomeWorld == _playerState.HomeWorld.RowId,
            FilterMethod.Text => base.WouldBeVisible(item, globalIndex),
            _                 => true,
        };
}
