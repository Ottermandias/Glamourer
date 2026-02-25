using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcHeader : SplitButtonHeader
{
    private readonly NpcSelection _selection;

    public NpcHeader(NpcSelection selection, LocalNpcAppearanceData favorites, DesignManager designs)
    {
        _selection = selection;
        LeftButtons.AddButton(new ExportToClipboardButton(selection),     100);
        LeftButtons.AddButton(new SaveAsDesignButton(selection, designs), 50);

        RightButtons.AddButton(new FavoriteButton(selection, favorites), 0);
    }

    public override ReadOnlySpan<byte> Text
        => _selection.HasSelection ? _selection.Name : "No Selection"u8;

    public override ColorParameter TextColor
        => ColorId.NormalDesign.Value();

    public override void Draw(Vector2 size)
    {
        var       color = ColorId.HeaderButtons.Value();
        using var _     = ImGuiColor.Text.Push(color).Push(ImGuiColor.Border, color);
        base.Draw(size with { Y = Im.Style.FrameHeight });
    }

    private sealed class FavoriteButton(NpcSelection selection, LocalNpcAppearanceData favorites) : BaseIconButton<AwesomeIcon>
    {
        private readonly Im.ColorDisposable _color = new();

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(
                selection.Favorite ? "Remove this NPC appearance from your favorites."u8 : "Add this NPC Appearance to your favorites."u8);

        public override AwesomeIcon Icon
            => LunaStyle.FavoriteIcon;

        public override bool IsVisible
            => selection.HasSelection;

        public override void OnClick()
            => favorites.ToggleFavorite(selection.Data);

        protected override void PreDraw()
            => _color.Push(ImGuiColor.Text, selection.Favorite ? ColorId.FavoriteStarOn.Value() : 0x80000000);

        protected override void PostDraw()
            => _color.Dispose();
    }

    private sealed class ExportToClipboardButton(NpcSelection selection) : BaseIconButton<AwesomeIcon>
    {
        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(
                "Copy the current NPCs appearance to your clipboard.\nHold Control to disable applying of customizations for the copied design.\nHold Shift to disable applying of gear for the copied design."u8);

        public override AwesomeIcon Icon
            => LunaStyle.ToClipboardIcon;

        public override bool IsVisible
            => selection.HasSelection;

        public override void OnClick()
        {
            try
            {
                var text = selection.ToBase64();
                Im.Clipboard.Set(text);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not copy {selection.Data.Name}'s data to clipboard.",
                    $"Could not copy data from NPC appearance {selection.Data.Kind} {selection.Data.Id.Id} to clipboard",
                    NotificationType.Error);
            }
        }
    }

    private sealed class SaveAsDesignButton(NpcSelection selection, DesignManager designs) : BaseIconButton<AwesomeIcon>
    {
        private StringU8    _newName = StringU8.Empty;
        private DesignBase? _newDesign;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(
                "Save this NPCs appearance as a design.\nHold Control to disable applying of customizations for the saved design.\nHold Shift to disable applying of gear for the saved design."u8);

        public override AwesomeIcon Icon
            => LunaStyle.SaveIcon;

        public override bool IsVisible
            => selection.HasSelection;

        public override void OnClick()
        {
            Im.Popup.Open("Save as Design"u8);
            _newName   = new StringU8(selection.Data.Name);
            _newDesign = selection.ToDesignBase();
        }

        protected override void PostDraw()
        {
            if (!InputPopup.OpenName("Save as Design"u8, _newName, out var name))
                return;

            if (_newDesign is not null && name.Length > 0)
                designs.CreateClone(_newDesign, name, true);
            _newDesign = null;
            _newName   = StringU8.Empty;
        }
    }
}
