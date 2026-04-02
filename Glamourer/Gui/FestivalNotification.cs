using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;
using Dalamud.Plugin;
using Glamourer.Config;
using ImSharp;
using Luna;

namespace Glamourer.Gui;

public enum FestivalSetting
{
    Undefined,
    NeverAskNo,
    NeverAskYes,
    AskYes,
    AskNo,
}

public sealed class FestivalNotification(Configuration config, IDalamudPluginInterface pi) : INotificationAwareMessage, IUiService
{
    private bool                 _doNotAskAgain;
    private IActiveNotification? _active;

    public NotificationType NotificationType
        => NotificationType.Info;

    public string NotificationMessage
        => config.FestivalMode switch
        {
            FestivalSetting.Undefined =>
                "Glamourer provides some festival-specific behaviour turned off by default.\n\nYou can always turn this behaviour on or off in the general settings, and choose your current preference now.",
            FestivalSetting.AskYes =>
                "A new Glamourer seasonal easter egg has started.\n\nDo you still want to keep festival-specific behavior on?",
            _ => "A new Glamourer seasonal easter egg has started.\n\nAre you still not interested?",
        };

    public string NotificationTitle
        => "Seasonal Easter Egg";

    public TimeSpan NotificationDuration
        => TimeSpan.MaxValue;

    public string LogMessage
        => string.Empty;

    public SeString ChatMessage
        => SeString.Empty;

    public StringU8 StoredMessage
        => StringU8.Empty;

    public StringU8 StoredTooltip
        => StringU8.Empty;

    public void OnNotificationActions(INotificationDrawArgs args)
    {
        Im.Separator();
        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
        var region = Im.ContentRegion.Available;
        var width  = Im.Font.CalculateSize("Do not ask again"u8).X + Im.Style.ItemInnerSpacing.X + Im.Style.FrameHeight;
        Im.Cursor.X += (region.X - width) / 2;
        using (ImStyleBorder.Frame.Push(ColorParameter.Default, 1))
        {
            Im.Checkbox("Do not ask again"u8, ref _doNotAskAgain);
        }

        var buttonSize = new Vector2((region.X - Im.Style.ItemSpacing.X) / 2, 0);
        var (yesText, noText) = config.FestivalMode switch
        {
            FestivalSetting.Undefined => RefTuple.Create("Let's Check It Out!"u8, "I Don't Like Fun."u8),
            FestivalSetting.AskYes    => RefTuple.Create("Keep It!"u8,            "Turn It Off."u8),
            _                         => RefTuple.Create("Try It This Time!"u8,   "Still No."u8),
        };
        if (ImEx.Button(yesText, buttonSize, !pi.AllowSeasonalEvents && !config.DeleteDesignModifier.IsActive()))
        {
            config.FestivalMode      = _doNotAskAgain ? FestivalSetting.NeverAskYes : FestivalSetting.AskYes;
            config.LastFestivalPopup = DateOnly.FromDateTime(DateTime.Now);
            args.Notification.DismissNow();
        }

        if (!pi.AllowSeasonalEvents && Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(
                "You have seasonal events disabled globally in Dalamud.\n\nGlamourer will respect this setting, so choosing yes here will not work until you enable the global setting."u8,
                Colors.SelectedRed);

            if (!config.DeleteDesignModifier.IsActive())
                Im.Text($"\nHold {config.DeleteDesignModifier} to click anyway.");
        }

        Im.Line.Same();
        if (Im.Button(noText, buttonSize))
        {
            config.FestivalMode      = _doNotAskAgain ? FestivalSetting.NeverAskNo : FestivalSetting.AskNo;
            config.LastFestivalPopup = DateOnly.FromDateTime(DateTime.Now);
            args.Notification.DismissNow();
        }
    }

    public void Update()
    {
        if (_active is null)
            Glamourer.Messager.AddMessage(this, false);
        else
            _active.Content = NotificationMessage;
    }

    public void OnNotificationCreated(IActiveNotification notification)
    {
        _active                             = notification;
        _active.MinimizedText               = _active.Title;
        _active.UserDismissable             = false;
        _active.RespectUiHidden             = true;
        _active.ShowIndeterminateIfNoExpiry = false;
        _active.Icon                        = INotificationIcon.From(FontAwesomeIcon.TheaterMasks);
        _active.Dismiss += args =>
        {
            if (args.Notification == _active)
                _active = null;
        };
    }
}
