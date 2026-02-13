using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public sealed class UnlocksTab : Window, ITab<MainTabType>
{
    private readonly EphemeralConfig _config;
    private readonly UnlockOverview  _overview;
    private readonly UnlockTable     _table;

    public UnlocksTab(EphemeralConfig config, UnlockOverview overview, UnlockTable table)
        : base("Unlocked Equipment")
    {
        _config   = config;
        _overview = overview;
        _table    = table;

        Flags  |= WindowFlags.NoDocking;
        IsOpen =  false;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700,  675),
            MaximumSize = new Vector2(3840, 2160),
        };
    }

    private bool DetailMode
    {
        get => _config.UnlockDetailMode;
        set
        {
            _config.UnlockDetailMode = value;
            _config.Save();
        }
    }

    public ReadOnlySpan<byte> Label
        => "Unlocks"u8;

    public MainTabType Identifier
        => MainTabType.Unlocks;

    public void DrawContent()
    {
        DrawTypeSelection();
        if (DetailMode)
            _table.Draw();
        else
            _overview.Draw();
        _table.Flags |= TableFlags.Resizable;
    }

    public override void Draw()
    {
        DrawContent();
    }

    private void DrawTypeSelection()
    {
        using var style = ImStyleDouble.ItemSpacing.Push(Vector2.Zero)
            .Push(ImStyleSingle.FrameRounding, 0);
        var buttonSize = new Vector2(Im.ContentRegion.Available.X / 2, Im.Style.FrameHeight);
        if (!IsOpen)
            buttonSize.X -= Im.Style.FrameHeight / 2;
        if (DetailMode)
            buttonSize.X -= Im.Style.FrameHeight / 2;

        if (ImEx.Button("Overview Mode"u8, buttonSize, "Show tinted icons of sets of unlocks."u8, !DetailMode))
            DetailMode = false;

        Im.Line.Same();
        if (ImEx.Button("Detailed Mode"u8, buttonSize, "Show all unlockable data as a combined filterable and sortable table."u8,
                DetailMode))
            DetailMode = true;

        if (DetailMode)
        {
            Im.Line.Same();
            if (ImEx.Icon.Button(LunaStyle.AutoResizeIcon, "Restore all columns to their original size."u8))
                _table.Flags &= ~TableFlags.Resizable;
        }

        if (!IsOpen)
        {
            Im.Line.Same();
            if (ImEx.Icon.Button(LunaStyle.PopOutIcon, "Pop the unlocks tab out into its own window."u8))
                IsOpen = true;
        }
    }
}
