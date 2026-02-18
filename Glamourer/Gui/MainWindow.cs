using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Glamourer.Interop.Penumbra;
using ImSharp;
using Luna;

namespace Glamourer.Gui;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Configuration.Configuration _config;
    private readonly PenumbraService             _penumbra;
    private readonly DesignQuickBar              _quickBar;
    private readonly MainTabBar                  _mainTabBar;
    private          bool                        _ignorePenumbra;

    public MainWindow(IDalamudPluginInterface pi, Configuration.Configuration config, PenumbraService penumbra,
        MainTabBar mainTabBar, DesignQuickBar quickBar)
        : base("GlamourerMainWindow")
    {
        pi.UiBuilder.DisableGposeUiHide = true;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 675),
            MaximumSize = new Vector2(3840, 2160),
        };
        _mainTabBar = mainTabBar;
        _quickBar   = quickBar;
        _config     = config;
        _penumbra   = penumbra;
        _mainTabBar = mainTabBar;
        IsOpen      = _config.OpenWindowAtStart;

        _penumbra.DrawSettingsSection += _mainTabBar.Settings.DrawPenumbraIntegrationSettings;
    }

    public void OpenSettings()
    {
        IsOpen              = true;
        _mainTabBar.NextTab = MainTabType.Settings;
    }

    public override void PreDraw()
    {
        Flags = _config.Ephemeral.LockMainWindow
            ? Flags | WindowFlags.NoMove | WindowFlags.NoResize
            : Flags & ~(WindowFlags.NoMove | WindowFlags.NoResize);
        WindowName = GetLabel();
    }

    public void Dispose()
        => _penumbra.DrawSettingsSection -= _mainTabBar.Settings.DrawPenumbraIntegrationSettings;

    public override void Draw()
    {
        var yPos = Im.Cursor.Y;
        if (!_penumbra.Available && !_ignorePenumbra)
        {
            if (_penumbra.CurrentMajor is 0)
                DrawProblemWindow(
                    "Could not attach to Penumbra. Please make sure Penumbra is installed and running.\n\nPenumbra is required for Glamourer to work properly."u8);
            else if (_penumbra is
                     {
                         CurrentMajor: PenumbraService.RequiredPenumbraBreakingVersion,
                         CurrentMinor: >= PenumbraService.RequiredPenumbraFeatureVersion,
                     })
                DrawProblemWindow(
                    $"You are currently not attached to Penumbra, seemingly by manually detaching from it.\n\nPenumbra's last API Version was {_penumbra.CurrentMajor}.{_penumbra.CurrentMinor}.\n\nPenumbra is required for Glamourer to work properly.");
            else
                DrawProblemWindow(
                    $"Attaching to Penumbra failed.\n\nPenumbra's API Version was {_penumbra.CurrentMajor}.{_penumbra.CurrentMinor}, but Glamourer requires a version of {PenumbraService.RequiredPenumbraBreakingVersion}.{PenumbraService.RequiredPenumbraFeatureVersion}, where the major version has to match exactly, and the minor version has to be greater or equal.\nYou may need to update Penumbra or enable Testing Builds for it for this version of Glamourer.\n\nPenumbra is required for Glamourer to work properly.");
        }
        else
        {
            _mainTabBar.Draw();
            if (_config.ShowQuickBarInTabs)
                _quickBar.DrawAtEnd(yPos);
        }
    }

    /// <summary> The longest support button text. </summary>
    public static ReadOnlySpan<byte> SupportInfoButtonText
        => "Copy Support Info to Clipboard"u8;

    /// <summary> Draw the support button group on the right-hand side of the window. </summary>
    public static void DrawSupportButtons(Glamourer glamourer, Changelog changelog)
    {
        var width = new Vector2(Im.Font.CalculateSize(SupportInfoButtonText).X + Im.Style.FramePadding.X * 2, 0);
        var xPos  = Im.Window.Width - width.X;
        Im.Cursor.Position = new Vector2(xPos, 0);
        SupportButton.Discord(Glamourer.Messager, width.X);

        Im.Cursor.Position = new Vector2(xPos, Im.Style.FrameHeightWithSpacing);
        DrawSupportButton(glamourer);

        Im.Cursor.Position = new Vector2(xPos, 2 * Im.Style.FrameHeightWithSpacing);
        SupportButton.ReniGuide(Glamourer.Messager, width.X);

        Im.Cursor.Position = new Vector2(xPos, 3 * Im.Style.FrameHeightWithSpacing);
        if (Im.Button("Show Changelogs"u8, new Vector2(width.X, 0)))
            changelog.ForceOpen = true;

        Im.Cursor.Position = new Vector2(xPos, 4 * Im.Style.FrameHeightWithSpacing);
        SupportButton.KoFiPatreon(Glamourer.Messager, width);
    }

    /// <summary>
    /// Draw a button that copies the support info to clipboards.
    /// </summary>
    private static void DrawSupportButton(Glamourer glamourer)
    {
        if (!Im.Button(SupportInfoButtonText))
            return;

        var text = glamourer.GatherSupportInformation();
        Im.Clipboard.Set(text);
        Glamourer.Messager.NotificationMessage("Copied Support Info to Clipboard.", NotificationType.Success, false);
    }

    private string GetLabel()
        => (Glamourer.Version.Length is 0, _config.Ephemeral.IncognitoMode) switch
        {
            (true, true)   => "Glamourer (Incognito Mode)###GlamourerMainWindow",
            (true, false)  => "Glamourer###GlamourerMainWindow",
            (false, false) => $"Glamourer v{Glamourer.Version}###GlamourerMainWindow",
            (false, true)  => $"Glamourer v{Glamourer.Version} (Incognito Mode)###GlamourerMainWindow",
        };

    private void DrawProblemWindow(Utf8StringHandler<TextStringHandlerBuffer> text)
    {
        using var color = ImGuiColor.Text.Push(Colors.SelectedRed);
        Im.Line.New();
        Im.Line.New();
        Im.TextWrapped(text);
        color.Pop();

        Im.Line.New();
        if (ImEx.Button("Try Attaching Again"u8))
            _penumbra.Reattach();

        var ignoreAllowed = _config.DeleteDesignModifier.IsActive();
        Im.Line.Same();
        if (ImEx.Button("Ignore Penumbra This Time"u8, default,
                $"Some functionality, like automation or retaining state, will not work correctly without Penumbra.\n\nIgnore this at your own risk!{(ignoreAllowed ? string.Empty : $"\n\nHold {_config.DeleteDesignModifier} while clicking to enable this button.")}",
                !ignoreAllowed))
            _ignorePenumbra = true;

        Im.Line.New();
        Im.Line.New();
        SupportButton.Discord(Glamourer.Messager, 0);
        Im.Line.Same();
        Im.Line.New();
        Im.Line.New();
    }
}

public enum MainTabType
{
    None       = -1,
    Settings   = 0,
    Debug      = 1,
    Actors     = 2,
    Designs    = 3,
    Automation = 4,
    Unlocks    = 5,
    Messages   = 6,
    Npcs       = 7,
}
