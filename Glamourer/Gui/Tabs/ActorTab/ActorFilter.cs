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

    private bool DrawCombo(float popupLeft)
    {
        using var color = ImGuiColor.Button.Push(LunaStyle.AttentionBackground,
            _config.ActorTypeFilter is not ActorTypeFilter.None || !_clanGender.IsEmpty);
        const ComboFlags flags = ComboFlags.NoPreview | ComboFlags.HeightLargest | ComboFlags.PopupAlignLeft;
        Im.Combo.DrawPreview("##combo"u8, StringU8.Empty, out var popupId, out var buttonBounds, flags);

        if (Im.Item.MiddleClicked())
        {
            // Ensure that a right-click clears the text filter if it is currently being edited.
            Im.Id.ClearActive();
            return Clear();
        }

        Im.Tooltip.OnHover("Filter actors for their type, clan and gender.\nMiddle-Click to clear all filters, including the text-filter."u8);

        var popupBounds = GetPopupBounds(popupLeft, buttonBounds);
        using var combo = Im.Combo.DrawPopup(popupId, popupBounds, flags);
        if (!combo)
            return false;

        var changes = false;
        using (var table = Im.Table.Begin("##filters"u8, 2,
                   TableFlags.SizingFixedFit | TableFlags.BordersInnerVertical | TableFlags.NoHostExtendX))
        {
            if (table)
            {
                Im.Table.NextColumn();
                var filter = _config.ActorTypeFilter ^ ActorTypeFilter.All;
                if (Im.Checkbox("Everything"u8, ref filter, ActorTypeFilter.All))
                {
                    _config.ActorTypeFilter = filter ^ ActorTypeFilter.All;
                    changes                 = true;
                }

                Im.Dummy(ImEx.ScaledVectorY(8));
                using (ImStyleDouble.ItemSpacing.PushY(2 * Im.Style.GlobalScale))
                {
                    foreach (var flag in ActorTypeFilter.Values.Skip(1))
                    {
                        if (Im.Checkbox(flag.ToNameU8(), ref filter, flag))
                        {
                            _config.ActorTypeFilter = filter ^ ActorTypeFilter.All;
                            changes                 = true;
                        }

                        Im.Tooltip.OnHover(flag.Tooltip());
                    }
                }

                Im.Table.NextColumn();
                changes |= _clanGender.Draw();
            }
        }

        if (changes)
            InvokeEvent();

        return changes;
    }

    private static unsafe Rectangle GetPopupBounds(float desiredX, Rectangle buttonBounds)
    {
        var popupName = new StringU8($"##Combo_{Im.Context.Pointer->BeginPopupStack.Size:D2}");
        fixed (byte* name = popupName.Span)
        {
            var popup = Im.Native.Methods.Internal.FindWindowByName(name);
            if (popup is null || !popup->WasActive)
                return new Rectangle(desiredX, buttonBounds.Minimum.Y, desiredX + buttonBounds.Width, buttonBounds.Maximum.Y);

            ImVec2 popupSize;
            Im.Native.Methods.Internal.CalcWindowNextAutoFitSize(&popupSize, popup);

            var viewport = Im.Window.Viewport.Pointer;
            var workLeft = viewport->WorkPos.X;
            var workRight = workLeft + viewport->WorkSize.X;
            var margin = Im.Style.DisplaySafeAreaPadding.X;
            var minimumX = workLeft + margin;
            var maximumX = workRight - popupSize.X - margin;
            if (maximumX < minimumX)
            {
                minimumX = workLeft;
                maximumX = Math.Max(workLeft, workRight - popupSize.X);
            }

            var popupLeft = Math.Clamp(desiredX, minimumX, maximumX);
            return new Rectangle(popupLeft, buttonBounds.Minimum.Y, popupLeft + popupSize.X, buttonBounds.Maximum.Y);
        }
    }

    public override bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
    {
        var popupLeft   = Im.Cursor.ScreenX;
        var filterRegion = availableRegion with { X = availableRegion.X - Im.Style.FrameHeight };
        var ret          = base.DrawFilter(label, filterRegion);
        Im.Line.NoSpacing();
        ret |= DrawCombo(popupLeft);

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
