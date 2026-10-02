using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Glamourer.Config;
using Glamourer.Services;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorFilter : TextFilterBase<ActorCacheItem>, IUiService
{
    private readonly IPlayerState _playerState;
    private readonly FilterConfig _config;
    private readonly ClanGenderFilter _clanGender;

    public ActorFilter(IPlayerState playerState, Configuration config, CustomizeService customize)
    {
        _playerState  =  playerState;
        _config       =  config.Filters;
        _clanGender  = new ClanGenderFilter(customize);
        FilterChanged += () => { _config.ActorFilter = Text; };
        if (config.RememberActorFilter)
            Set(_config.ActorFilter);
    }

    public override bool Clear()
    {
        var changes = _clanGender.Clear();
        changes |= _config.ActorTypeFilter is not ActorTypeFilter.None;
        _config.ActorTypeFilter = ActorTypeFilter.None;
        if (!SetInternal(string.Empty) && !changes)
            return false;

        InvokeEvent();
        return true;
    }

    public bool DrawSelectors(Vector2 availableRegion)
    {
        var changes = _clanGender.DrawSelectors(availableRegion);

        if (changes)
            InvokeEvent();

        return changes;
    }

    private bool DrawCombo()
    {
        using var color = ImGuiColor.Button.Push(LunaStyle.AttentionBackground, _config.ActorTypeFilter is not ActorTypeFilter.None);
        using var combo = Im.Combo.Begin("##combo"u8, StringU8.Empty,
            ComboFlags.NoPreview | ComboFlags.HeightLargest | ComboFlags.PopupAlignLeft);

        var changes = false;
        if (Im.Item.MiddleClicked())
        {
            // Ensure that a right-click clears the text filter if it is currently being edited.
            Im.Id.ClearActive();
            changes = Clear();
        }

        Im.Tooltip.OnHover("Filter actors for their type.\nMiddle-Click to clear all filters, including the text-filter."u8);

        if (!combo)
            return changes;

        var filter = _config.ActorTypeFilter ^ ActorTypeFilter.All;
        if (Im.Checkbox("Everything"u8, ref filter, ActorTypeFilter.All))
        {
            _config.ActorTypeFilter = filter ^ ActorTypeFilter.All;
            changes                 = true;
        }

        Im.Dummy(ImEx.ScaledVectorY(8));
        using var style = ImStyleDouble.ItemSpacing.PushY(2 * Im.Style.GlobalScale);
        foreach (var flag in ActorTypeFilter.Values.Skip(1))
        {
            if (Im.Checkbox(flag.ToNameU8(), ref filter, flag))
            {
                _config.ActorTypeFilter = filter ^ ActorTypeFilter.All;
                changes                 = true;
            }

            Im.Tooltip.OnHover(flag.Tooltip());
        }

        if (changes)
            InvokeEvent();

        return changes;
    }

    public override bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
    {
        var filterRegion = availableRegion with { X = availableRegion.X - Im.Style.FrameHeight };
        var ret          = base.DrawFilter(label, filterRegion);
        Im.Line.NoSpacing();
        ret |= DrawCombo();

        return ret;
    }

    protected override string ToFilterString(in ActorCacheItem item, int globalIndex)
        => item.DisplayText.Utf16;

    public override bool WouldBeVisible(in ActorCacheItem item, int globalIndex)
    {
        if (!base.WouldBeVisible(item, globalIndex))
            return false;
        if (!_clanGender.Matches(item.Clan, item.Gender))
            return false;

        switch (item.Identifier.Type)
        {
            case IdentifierType.Player:
                if (_config.ActorTypeFilter.HasFlag(ActorTypeFilter.Player))
                    return false;
                if (item.Identifier.HomeWorld != _playerState.HomeWorld.RowId && _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Homeworld))
                    return false;

                return true;
            case IdentifierType.Owned:
                if (_config.ActorTypeFilter.HasFlag(ActorTypeFilter.Owned))
                    return false;
                if (item.Identifier.HomeWorld != _playerState.HomeWorld.RowId && _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Homeworld))
                    return false;

                return item.Identifier.Kind switch
                {
                    ObjectKind.Companion when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Minion)   => false,
                    ObjectKind.Mount when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Mount)        => false,
                    ObjectKind.Ornament when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Accessory) => false,
                    _                                                                                   => true,
                };
            case IdentifierType.Npc:
                return item.Identifier.Kind switch
                {
                    ObjectKind.BattleNpc when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.BattleNpc) => false,
                    ObjectKind.EventNpc when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.EventNpc)   => false,
                    ObjectKind.Companion when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Minion)    => false,
                    ObjectKind.Mount when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Mount)         => false,
                    ObjectKind.Ornament when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Accessory)  => false,
                    _                                                                                    => true,
                };
            case IdentifierType.Retainer when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Retainer):
            case IdentifierType.Special when _config.ActorTypeFilter.HasFlag(ActorTypeFilter.Special):
                return false;
            default: return true;
        }
    }
}
